using static LGSTrayHID.HidApi.HidApi;
using static LGSTrayHID.HidApi.HidApiWinApi;
using static LGSTrayHID.HidApi.HidApiHotPlug;
using LGSTrayHID.HidApi;
using System.Collections.Concurrent;
using LGSTrayPrimitives.MessageStructs;

namespace LGSTrayHID
{
    public sealed class HidppManagerContext
    {
        public static readonly HidppManagerContext _instance = new();
        public static HidppManagerContext Instance => _instance;

        private readonly Dictionary<string, Guid> _containerMap = [];
        private readonly Dictionary<Guid, HidppDevices> _deviceMap = [];
        private readonly BlockingCollection<HidDeviceInfo> _deviceQueue = [];

        public delegate void HidppDeviceEventHandler(IPCMessageType messageType, IPCMessage message);

        public event HidppDeviceEventHandler? HidppDeviceEvent;

        private HidppManagerContext()
        {

        }

        static HidppManagerContext()
        {
            _ = HidInit();
        }

        public void SignalDeviceEvent(IPCMessageType messageType, IPCMessage message)
        {
            try
            {
                HidppDeviceEvent?.Invoke(messageType, message);
            }
            catch (Exception ex)
            {
                Log.Write($"signal {messageType} failed: {ex.GetType().Name}: {ex.Message}");
            }
        }

        private unsafe int EnqueueDevice(HidHotPlugCallbackHandle _, HidDeviceInfo* device, HidApiHotPlugEvent hidApiHotPlugEvent, nint __)
        {
            if (hidApiHotPlugEvent == HidApiHotPlugEvent.HID_API_HOTPLUG_EVENT_DEVICE_ARRIVED)
            {
                _deviceQueue.Add(*device);
            }

            return 0;
        }

        private async Task<int> InitDevice(HidDeviceInfo deviceInfo)
        {
            var messageType = (deviceInfo).GetHidppMessageType();
            switch (messageType)
            {
                case HidppMessageType.NONE:
                case HidppMessageType.VERY_LONG:
                    return 0;
            }

            string devPath = (deviceInfo).GetPath();

            Log.Write($"arrived pid=0x{deviceInfo.ProductId:X4} usage=0x{deviceInfo.Usage:X4} {messageType} {devPath}");

            HidDevicePtr dev = HidOpenPath(ref deviceInfo);
            if (dev == IntPtr.Zero)
            {
                Log.Write($"open failed: {devPath}");
                return 0;
            }

            _ = HidWinApiGetContainerId(dev, out Guid containerId);

            if (!_deviceMap.TryGetValue(containerId, out HidppDevices? value))
            {
                value = new();
                _deviceMap[containerId] = value;
                _containerMap[devPath] = containerId;
            }

            switch (messageType)
            {
                case HidppMessageType.SHORT:
                    await value.SetDevShort(dev);
                    break;
                case HidppMessageType.LONG:
                    await value.SetDevLong(dev);
                    break;
            }

            return 0;
        }

        private unsafe int DeviceLeft(HidHotPlugCallbackHandle callbackHandle, HidDeviceInfo* deviceInfo, HidApiHotPlugEvent hidApiHotPlugEvent, nint userData)
        {
            try
            {
                string devPath = (*deviceInfo).GetPath();
                Log.Write($"left {devPath}");

                if (_containerMap.TryGetValue(devPath, out var containerId))
                {
                    if (_deviceMap.TryGetValue(containerId, out HidppDevices? devices))
                    {
                        devices.Dispose();
                        _deviceMap.Remove(containerId);
                    }

                    _containerMap.Remove(devPath);
                }
            }
            catch (Exception ex)
            {
                Log.Write($"device-left handler failed: {ex.GetType().Name}: {ex.Message}");
            }

            return 0;
        }

        public void Start(CancellationToken cancellationToken)
        {
            new Thread(async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    HidDeviceInfo dev;
                    try
                    {
                        dev = _deviceQueue.Take(cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }

                    try
                    {
                        await InitDevice(dev);
                    }
                    catch (Exception ex)
                    {
                        // Never let one bad device kill the queue processor
                        Log.Write($"device init failed: {ex.GetType().Name}: {ex.Message}");
                    }
                }
            })
            { IsBackground = true }.Start();

            _ = Task.Run(() => WatchdogAsync(cancellationToken));

            unsafe
            {
                HidHotplugRegisterCallback(0x046D, 0x00, HidApiHotPlugEvent.HID_API_HOTPLUG_EVENT_DEVICE_ARRIVED, HidApiHotPlugFlag.HID_API_HOTPLUG_ENUMERATE, EnqueueDevice, IntPtr.Zero, (int*)IntPtr.Zero);
                HidHotplugRegisterCallback(0x046D, 0x00, HidApiHotPlugEvent.HID_API_HOTPLUG_EVENT_DEVICE_LEFT, HidApiHotPlugFlag.NONE, DeviceLeft, IntPtr.Zero, (int*)IntPtr.Zero);
            }
        }

        /// <summary>
        /// Watches for devices that stopped reporting. If the HID link went stale (mouse switched
        /// between wired and wireless, USB power saving, ...) the whole HID layer is rebuilt,
        /// which is what restarting the application manually used to do.
        /// </summary>
        private async Task WatchdogAsync(CancellationToken cancellationToken)
        {
            int staleSeconds = 90;

            using PeriodicTimer timer = new(TimeSpan.FromSeconds(20));
            try
            {
                while (await timer.WaitForNextTickAsync(cancellationToken))
                {
                    try
                    {
                        DateTimeOffset now = DateTimeOffset.Now;

                        bool anyStale = false;
                        bool anyFresh = false;

                        foreach (var (_, devices) in _deviceMap)
                        {
                            foreach (var (_, device) in devices.DeviceCollection)
                            {
                                if (!device.HasBattery) { continue; }

                                double age = (now - device.LastUpdate).TotalSeconds;
                                if (age > staleSeconds) { anyStale = true; }
                                if (age < 60) { anyFresh = true; }
                            }
                        }

                        if (!anyStale)
                        {
                            staleSeconds = 90;
                            continue;
                        }

                        if (anyFresh)
                        {
                            // Another device is still delivering data (e.g. the mouse is plugged in
                            // and its wireless slot went quiet) - nothing to repair
                            staleSeconds = 90;
                            continue;
                        }

                        Log.Write($"watchdog: no fresh battery data for >{staleSeconds}s, rebuilding HID layer");
                        await ReenumerateAsync(cancellationToken);

                        staleSeconds = Math.Min(staleSeconds * 2, 600);
                    }
                    catch (Exception ex)
                    {
                        Log.Write($"watchdog error: {ex.GetType().Name}: {ex.Message}");
                    }
                }
            }
            catch (OperationCanceledException) { }
        }

        private async Task ReenumerateAsync(CancellationToken cancellationToken)
        {
            foreach (var (_, devices) in _deviceMap)
            {
                devices.Dispose();
            }

            _deviceMap.Clear();
            _containerMap.Clear();

            // hidapi owns the enumeration memory, copy what is needed before awaiting
            var entries = new List<(string Path, ushort Usage)>();

            unsafe
            {
                HidDeviceInfo* list = HidEnumerate(0x046D, 0x0000);
                if (list == null) { return; }

                try
                {
                    for (HidDeviceInfo* cur = list; cur != null; cur = cur->Next)
                    {
                        var type = (*cur).GetHidppMessageType();
                        if (type != HidppMessageType.SHORT && type != HidppMessageType.LONG) { continue; }

                        entries.Add(((*cur).GetPath(), (*cur).Usage));
                    }
                }
                finally
                {
                    HidFreeEnumeration(list);
                }
            }

            foreach (var (path, usage) in entries)
            {
                if (cancellationToken.IsCancellationRequested) { break; }

                try
                {
                    await OpenAndAttachAsync(path, usage);
                }
                catch (Exception ex)
                {
                    Log.Write($"re-enumerate failed for {path}: {ex.GetType().Name}: {ex.Message}");
                }
            }

            await Task.Delay(200, cancellationToken);
        }

        private async Task OpenAndAttachAsync(string path, ushort usage)
        {
            nint dev = HidOpenPathAnsi(path);
            if (dev == IntPtr.Zero)
            {
                Log.Write($"re-open failed: {path}");
                return;
            }

            _ = HidWinApiGetContainerId(dev, out Guid containerId);

            if (!_deviceMap.TryGetValue(containerId, out HidppDevices? hidppDevices))
            {
                hidppDevices = new();
                _deviceMap[containerId] = hidppDevices;
                _containerMap[path] = containerId;
            }

            if (usage == 0x0001)
            {
                await hidppDevices.SetDevShort(dev);
            }
            else
            {
                await hidppDevices.SetDevLong(dev);
            }
        }


        public async Task ForceBatteryUpdates()
        {
            foreach (var (_, hidppDevice) in _deviceMap)
            {
                var tasks = hidppDevice.DeviceCollection
                    .Select(x => x.Value)
                    .Select(x => x.UpdateBattery(true));

                await Task.WhenAll(tasks);
            }
        }
    }
}
