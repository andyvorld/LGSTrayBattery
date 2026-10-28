using LGSTrayPrimitives;
using LGSTrayPrimitives.MessageStructs;
using LGSTrayHID.Features;
using System.Text;

namespace LGSTrayHID
{
    public class HidppDevice
    {
        private readonly SemaphoreSlim _initSemaphore = new(1, 1);
        private Func<HidppDevice, Task<BatteryUpdateReturn?>>? _getBatteryAsync;
        private bool _pollLoopStarted;

        public string DeviceName { get; private set; } = string.Empty;
        public int DeviceType { get; private set; } = 3;
        public string Identifier { get; private set; } = string.Empty;

        private BatteryUpdateReturn lastBatteryReturn;
        private DateTimeOffset lastUpdate = DateTimeOffset.MinValue;
        public DateTimeOffset LastUpdate => lastUpdate;
        public bool HasBattery => _getBatteryAsync != null;

        private readonly HidppDevices _parent;
        public HidppDevices Parent => _parent;

        private readonly byte _deviceIdx;
        public byte DeviceIdx => _deviceIdx;

        private readonly Dictionary<ushort, byte> _featureMap = [];
        public Dictionary<ushort, byte> FeatureMap => _featureMap;

        public HidppDevice(HidppDevices parent, byte deviceIdx)
        {
            _parent = parent;
            _deviceIdx = deviceIdx;
        }

        public async Task InitAsync()
        {
            await _initSemaphore.WaitAsync();
            try
            {
                Hidpp20 ret;

                // Sync Ping
                int successCount = 0;
                int successThresh = 3;
                for (int i = 0; i < 10; i++)
                {
                    var ping = await _parent.Ping20(_deviceIdx, 100);
                    if (ping)
                    {
                        successCount++;
                    }
                    else
                    {
                        successCount = 0;
                    }

                    if (successCount >= successThresh) { break; }
                }

                if (successCount < successThresh)
                {
                    Log.Write($"device {_deviceIdx}: no answer to ping");
                    return;
                }

                _featureMap.Clear();

                // Find 0x0001 IFeatureSet
                ret = await _parent.WriteRead20(_parent.RequestTarget, new byte[7] { 0x10, _deviceIdx, 0x00, 0x00 | HidppDevices.SW_ID, 0x00, 0x01, 0x00 });
                _featureMap[0x0001] = ret.GetParam(0);

                // Get Feature Count
                ret = await _parent.WriteRead20(_parent.RequestTarget, new byte[7] { 0x10, _deviceIdx, _featureMap[0x0001], 0x00 | HidppDevices.SW_ID, 0x00, 0x00, 0x00 });
                int featureCount = ret.GetParam(0);

                // Enumerate Features
                for (byte i = 0; i <= featureCount; i++)
                {
                    ret = await _parent.WriteRead20(_parent.RequestTarget, new byte[7] { 0x10, _deviceIdx, _featureMap[0x0001], 0x10 | HidppDevices.SW_ID, i, 0x00, 0x00 });
                    ushort featureId = (ushort)((ret.GetParam(0) << 8) + ret.GetParam(1));

                    _featureMap[featureId] = i;
                }

                await InitPopulateAsync();
            }
            catch (Exception ex)
            {
                Log.Write($"device {_deviceIdx}: init failed: {ex.GetType().Name}: {ex.Message}");
            }
            finally
            {
                _initSemaphore.Release();
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0018:Inline variable declaration")]
        private async Task InitPopulateAsync()
        {
            Hidpp20 ret;
            byte featureId;

            // Device name
            if (_featureMap.TryGetValue(0x0005, out featureId))
            {
                ret = await _parent.WriteRead20(_parent.RequestTarget, new byte[7] { 0x10, _deviceIdx, featureId, 0x00 | HidppDevices.SW_ID, 0x00, 0x00, 0x00 });
                int nameLength = ret.GetParam(0);

                string name = "";

                while (name.Length < nameLength)
                {
                    ret = await _parent.WriteRead20(_parent.RequestTarget, new byte[7] { 0x10, _deviceIdx, featureId, 0x10 | HidppDevices.SW_ID, (byte)name.Length, 0x00, 0x00 });
                    name += Encoding.UTF8.GetString(ret.GetParams());
                }

                DeviceName = name.TrimEnd('\0');

                foreach (var tag in GlobalSettings.settings.DisabledDevices)
                {
                    if (DeviceName.Contains(tag))
                    {
                        Log.Write($"{DeviceName} is marked as disabled");
                        return;
                    }
                };

                ret = await _parent.WriteRead20(_parent.RequestTarget, new byte[7] { 0x10, _deviceIdx, featureId, 0x20 | HidppDevices.SW_ID, 0x00, 0x00, 0x00 });
                DeviceType = ret.GetParam(0);
            }
            else
            {
                // Device does not have a name/Hidpp error ignore it
                return;
            }

            if (_featureMap.TryGetValue(0x0003, out featureId))
            {
                ret = await _parent.WriteRead20(_parent.RequestTarget, new byte[7] { 0x10, _deviceIdx, featureId, 0x00 | HidppDevices.SW_ID, 0x00, 0x00, 0x00 });

                string unitId = BitConverter.ToString(ret.GetParams().ToArray(), 1, 4).Replace("-", string.Empty);
                string modelId = BitConverter.ToString(ret.GetParams().ToArray(), 7, 5).Replace("-", string.Empty);

                bool serialNumberSupported = (ret.GetParam(14) & 0x1) == 0x1;
                string? serialNumber = null;
                if (serialNumberSupported)
                {
                    ret = await _parent.WriteRead20(_parent.RequestTarget, new byte[7] { 0x10, _deviceIdx, featureId, 0x20 | HidppDevices.SW_ID, 0x00, 0x00, 0x00 });
                    serialNumber = BitConverter.ToString(ret.GetParams().ToArray(), 0, 11).Replace("-", string.Empty);
                }

                Identifier = serialNumber ?? $"{unitId}-{modelId}";
            }
            else
            {
                // Device does not have a serial identifier the device name as a hash identifier
                Identifier = $"{DeviceName.GetHashCode():X04}";
            }

            _getBatteryAsync = FeatureMap switch
            {
                { } when FeatureMap.ContainsKey(0x1000) => Battery1000.GetBatteryAsync,
                { } when FeatureMap.ContainsKey(0x1001) => Battery1001.GetBatteryAsync,
                { } when FeatureMap.ContainsKey(0x1004) => Battery1004.GetBatteryAsync,
                _ => null
            };

            Log.Write($"device {_deviceIdx} ready: '{DeviceName}' id={Identifier} transport={_parent.RequestModeName} batteryFeature={(_getBatteryAsync == null ? "none" : "yes")}");

            HidppManagerContext.Instance.SignalDeviceEvent(
                IPCMessageType.INIT,
                new InitMessage(Identifier, DeviceName, _getBatteryAsync != null, (DeviceType)DeviceType)
            );

            await Task.Delay(200);

            if (_getBatteryAsync == null || _pollLoopStarted) { return; }

            _pollLoopStarted = true;
            _ = Task.Run(PollLoopAsync);
        }

        private async Task PollLoopAsync()
        {
            int consecutiveFailures = 0;
            bool quietLogged = false;

            while (!_parent.Disposed)
            {
                try
                {
                    bool ok = await UpdateBattery();

                    if (ok)
                    {
                        consecutiveFailures = 0;
                        quietLogged = false;
                        await Task.Delay(Math.Max(1, GlobalSettings.settings.PollPeriod) * 1000);
                        continue;
                    }

                    consecutiveFailures++;
                    await Task.Delay(Math.Max(1, GlobalSettings.settings.RetryTime) * 1000);

                    if (consecutiveFailures < 3) { continue; }

                    consecutiveFailures = 0;

                    if (_parent.RequestMode == 1)
                    {
                        // Wireless slot that went quiet: the device is asleep or plugged in for
                        // charging. Keep retrying (the receiver answers again as soon as the device
                        // is back), just don't spam the log about it.
                        if (!quietLogged)
                        {
                            Log.Write($"device {_deviceIdx}: wireless slot quiet, keeping retry");
                            quietLogged = true;
                        }
                    }
                    else
                    {
                        // Device stopped answering: re-resolve the feature table instead of
                        // silently sitting on a stale value forever.
                        Log.Write($"device {_deviceIdx}: 3 failed reads, re-initialising");
                        await InitAsync();
                    }
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Log.Write($"device {_deviceIdx}: poll error: {ex.GetType().Name}: {ex.Message}");
                    await Task.Delay(1000);
                }
            }
        }

        public async Task<bool> UpdateBattery(bool forceIpcUpdate = false)
        {
            if (Parent.Disposed) { return false; }
            if (_getBatteryAsync == null) { return false; }

            var ret = await _getBatteryAsync.Invoke(this);

            if (ret == null)
            {
                // One immediate retry: the first request after an idle period can be lost
                await Task.Delay(150);
                ret = await _getBatteryAsync.Invoke(this);
            }

            if (ret == null) { return false; }

            var batStatus = ret.Value;
            lastUpdate = DateTimeOffset.Now;

            if (forceIpcUpdate || (batStatus == lastBatteryReturn))
            {
                // Don't report if no change
                return true;
            }

            lastBatteryReturn = batStatus;
            Log.Write($"device {_deviceIdx} update: {batStatus.batteryPercentage}% status={batStatus.status} mV={batStatus.batteryMVolt}");
            HidppManagerContext.Instance.SignalDeviceEvent(
                IPCMessageType.UPDATE,
                new UpdateMessage(Identifier, batStatus.batteryPercentage, batStatus.status, batStatus.batteryMVolt, lastUpdate)
            );

            return true;
        }
    }
}
