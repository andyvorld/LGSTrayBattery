using LGSTrayPrimitives;
using LGSTrayPrimitives.MessageStructs;
using LGSTrayHID.Features;
using System.Text;

using static LGSTrayHID.HidppDevices;

#if DEBUG
using Log = System.Console;
#else
using Log = System.Diagnostics.Debug;
#endif

namespace LGSTrayHID
{
    public class HidppDevice
    {
        private readonly SemaphoreSlim _initSemaphore = new(1, 1);
        private Func<HidppDevice, Task<BatteryUpdateReturn?>>? _getBatteryAsync;

        public string DeviceName { get; private set; } = string.Empty;
        public int DeviceType { get; private set; } = 3;
        public string Identifier { get; private set; } = string.Empty;

        private BatteryUpdateReturn lastBatteryReturn;
        private DateTimeOffset lastUpdate = DateTimeOffset.MinValue;

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

        private readonly CancellationTokenSource _cts = new();
        private bool _initialised = false;

        public bool IsCancelled => _cts.IsCancellationRequested;

        // Called when this object is superseded (new link notification for the same index) or the parent goes away.
        public void Cancel()
        {
            _cts.Cancel();
        }

        // Init retry schedule: 2s, 4s, 6s ... capped at 30s between attempts. Retries indefinitely: a sleeping
        // device does not answer pings at all, and the link may not drop/re-establish (no 0x41) when it wakes.
        private const int PING_TIMEOUT_MS = 300;
        private const int INIT_REQUEST_TIMEOUT_MS = 500;

        public async Task InitAsync()
        {
            await _initSemaphore.WaitAsync();
            try
            {
                if (_initialised) { return; }

                for (int attempt = 1; !IsCancelled && !_parent.Disposed; attempt++)
                {
                    bool ok = false;
                    try
                    {
                        ok = await TryInitOnceAsync();
                    }
                    catch (Exception ex)
                    {
                        Log.WriteLine($"Init attempt {attempt} for device index {_deviceIdx} failed: {ex.Message}");
                    }

                    if (ok)
                    {
                        _initialised = true;
                        return;
                    }

                    int delayMs = Math.Min(30_000, 2_000 * attempt);
                    try
                    {
                        await Task.Delay(delayMs, _cts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        return;
                    }
                }
            }
            finally
            {
                _initSemaphore.Release();
            }
        }

        private async Task<bool> TryInitOnceAsync()
        {
            Hidpp20 ret;
            _featureMap.Clear();

            // Sync Ping. The first request after the link has been idle can take ~0.7s to be answered,
            // which is why the original fixed 100ms timeout with no retry failed at start-up.
            int successCount = 0;
            int successThresh = 3;
            for (int i = 0; i < 10; i++)
            {
                if (IsCancelled) { return false; }

                var ping = await _parent.Ping20(_deviceIdx, PING_TIMEOUT_MS);
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

            if (successCount < successThresh) { return false; }

            // Find 0x0001 IFeatureSet
            ret = await _parent.WriteRead20(_parent.DevShort, new byte[7] { 0x10, _deviceIdx, 0x00, 0x00 | SW_ID, 0x00, 0x01, 0x00 }, INIT_REQUEST_TIMEOUT_MS);
            if (ret.Length == 0) { return false; }
            _featureMap[0x0001] = ret.GetParam(0);

            // Get Feature Count
            ret = await _parent.WriteRead20(_parent.DevShort, new byte[7] { 0x10, _deviceIdx, _featureMap[0x0001], 0x00 | SW_ID, 0x00, 0x00, 0x00 }, INIT_REQUEST_TIMEOUT_MS);
            if (ret.Length == 0) { return false; }
            int featureCount = ret.GetParam(0);

            // Enumerate Features
            for (byte i = 0; i <= featureCount; i++)
            {
                ret = await _parent.WriteRead20(_parent.DevShort, new byte[7] { 0x10, _deviceIdx, _featureMap[0x0001], 0x10 | SW_ID, i, 0x00, 0x00 }, INIT_REQUEST_TIMEOUT_MS);
                if (ret.Length == 0) { return false; }
                ushort featureId = (ushort)((ret.GetParam(0) << 8) + ret.GetParam(1));

                _featureMap[featureId] = i;
            }

            return await InitPopulateAsync();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0018:Inline variable declaration")]
        private async Task<bool> InitPopulateAsync()
        {
            Hidpp20 ret;
            byte featureId;

            // Device name
            if (_featureMap.TryGetValue(0x0005, out featureId))
            {
                ret = await _parent.WriteRead20(_parent.DevShort, new byte[7] { 0x10, _deviceIdx, featureId, 0x00 | SW_ID, 0x00, 0x00, 0x00 }, INIT_REQUEST_TIMEOUT_MS);
                if (ret.Length == 0) { return false; }
                int nameLength = ret.GetParam(0);

                string name = "";

                while (name.Length < nameLength)
                {
                    ret = await _parent.WriteRead20(_parent.DevShort, new byte[7] { 0x10, _deviceIdx, featureId, 0x10 | SW_ID, (byte)name.Length, 0x00, 0x00 }, INIT_REQUEST_TIMEOUT_MS);
                    if (ret.Length == 0) { return false; }
                    name += Encoding.UTF8.GetString(ret.GetParams());
                }

                DeviceName = name.TrimEnd('\0');

                foreach (var tag in GlobalSettings.settings.DisabledDevices)
                {
                    if (DeviceName.Contains(tag))
                    {
                        Log.WriteLine($"{DeviceName} is marked as disabled");
                        return true;
                    }
                };

                ret = await _parent.WriteRead20(_parent.DevShort, new byte[7] { 0x10, _deviceIdx, featureId, 0x20 | SW_ID, 0x00, 0x00, 0x00 }, INIT_REQUEST_TIMEOUT_MS);
                if (ret.Length == 0) { return false; }
                DeviceType = ret.GetParam(0);
            }
            else
            {
                // Device does not have a name/Hidpp error ignore it
                return true;
            }

            if (_featureMap.TryGetValue(0x0003, out featureId))
            {
                ret = await _parent.WriteRead20(_parent.DevShort, new byte[7] { 0x10, _deviceIdx, featureId, 0x00 | SW_ID, 0x00, 0x00, 0x00 }, INIT_REQUEST_TIMEOUT_MS);
                if (ret.Length == 0) { return false; }

                string unitId = BitConverter.ToString(ret.GetParams().ToArray(), 1, 4).Replace("-", string.Empty);
                string modelId = BitConverter.ToString(ret.GetParams().ToArray(), 7, 5).Replace("-", string.Empty);

                bool serialNumberSupported = (ret.GetParam(14) & 0x1) == 0x1;
                string? serialNumber = null;
                if (serialNumberSupported)
                {
                    ret = await _parent.WriteRead20(_parent.DevShort, new byte[7] { 0x10, _deviceIdx, featureId, 0x20 | SW_ID, 0x00, 0x00, 0x00 }, INIT_REQUEST_TIMEOUT_MS);
                    if (ret.Length == 0) { return false; }
                    serialNumber = BitConverter.ToString(ret.GetParams().ToArray(), 0, 11).Replace("-", string.Empty);
                }

                Identifier = serialNumber ?? $"{unitId}-{modelId}";
            }
            else
            {
                // Device does not have a serial identifier the device name as a hash identifier
                Identifier = $"{DeviceName.GetHashCode():X04}";
            }

#if DEBUG
            Log.WriteLine("---");
            Log.WriteLine(DeviceName + " Ready");
            Log.WriteLine(Identifier);
            foreach ((ushort featureIdItr, string featureDesc) in new (ushort, string)[]
            {
                (0x1000, "Battery Unified Level"),
                (0x1001, "Battery Voltage"),
                (0x1004, "Unified Battery"),
            })
            {
                if (_featureMap.ContainsKey(featureIdItr))
                {
                    Log.WriteLine($"0x{featureIdItr:X} - {featureDesc} Found");
                }
            }
            Log.WriteLine("---");
#endif

            _getBatteryAsync = FeatureMap switch
            {
                { } when FeatureMap.ContainsKey(0x1000) => Battery1000.GetBatteryAsync,
                { } when FeatureMap.ContainsKey(0x1001) => Battery1001.GetBatteryAsync,
                { } when FeatureMap.ContainsKey(0x1004) => Battery1004.GetBatteryAsync,
                _ => null
            };

            HidppManagerContext.Instance.SignalDeviceEvent(
                IPCMessageType.INIT,
                new InitMessage(Identifier, DeviceName, _getBatteryAsync != null, (DeviceType)DeviceType)
            );

            await Task.Delay(1000);

            _ = Task.Run(async () =>
            {
                if (_getBatteryAsync == null) { return; }

                while (!IsCancelled && !_parent.Disposed)
                {
                    var now = DateTimeOffset.Now;
#if DEBUG
                    var expectedUpdateTime = lastUpdate.AddSeconds(1);
#else
                    var expectedUpdateTime = lastUpdate.AddSeconds(GlobalSettings.settings.PollPeriod);
#endif
                    if (now < expectedUpdateTime)
                    {
                        await Task.Delay((int)(expectedUpdateTime - now).TotalMilliseconds);
                    }

                    await UpdateBattery();
                    await Task.Delay(GlobalSettings.settings.RetryTime * 1000);
                }
            });

            return true;
        }

        public async Task UpdateBattery(bool forceIpcUpdate = false)
        {
            if (Parent.Disposed) { return; }
            if (_getBatteryAsync == null) { return; }

            var ret = await _getBatteryAsync.Invoke(this);

            if (ret == null) { return; }

            var batStatus = ret.Value;
            lastUpdate = DateTimeOffset.Now;

            if (forceIpcUpdate || (batStatus == lastBatteryReturn))
            {
                // Don't report if no change
                return;
            }

            lastBatteryReturn = batStatus;
            HidppManagerContext.Instance.SignalDeviceEvent(
                IPCMessageType.UPDATE,
                new UpdateMessage(Identifier, batStatus.batteryPercentage, batStatus.status, batStatus.batteryMVolt, lastUpdate)
            );
        }
    }
}
