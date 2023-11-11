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

        private readonly Dictionary<string, Guid> _containerMap = new();
        private readonly Dictionary<Guid, HidppDevices> _deviceMap = new();
        private readonly BlockingCollection<HidDeviceInfo> _deviceQueue = new();

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
            HidppDeviceEvent?.Invoke(messageType, message);
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
            Console.WriteLine(devPath);

            Guid containerId = new();
            HidDevicePtr dev;
            unsafe
            {
                dev = HidOpenPath(deviceInfo.Path);
#pragma warning disable CS9123 // The '&' operator should not be used on parameters or local variables in async methods.
                _ = HidWinApiGetContainerId(dev, &containerId);
#pragma warning restore CS9123 // The '&' operator should not be used on parameters or local variables in async methods.

                Console.WriteLine(containerId.ToString());
                Console.WriteLine("x{0:X04}", (deviceInfo).Usage);
                Console.WriteLine("x{0:X04}", (deviceInfo).UsagePage);
                Console.WriteLine();
            }


            if (!_deviceMap.ContainsKey(containerId))
            {
                _deviceMap[containerId] = new();
                _containerMap[devPath] = containerId;
            }

            switch (messageType)
            {
                case HidppMessageType.SHORT:
                    await _deviceMap[containerId].SetDevShort(dev);
                    break;
                case HidppMessageType.LONG:
                    await _deviceMap[containerId].SetDevLong(dev);
                    break;
            }

            return 0;
        }

        private unsafe int DeviceLeft(HidHotPlugCallbackHandle callbackHandle, HidDeviceInfo* deviceInfo, HidApiHotPlugEvent hidApiHotPlugEvent, nint userData)
        {
            string devPath = (*deviceInfo).GetPath();

            if (_containerMap.TryGetValue(devPath, out var containerId))
            {
                _deviceMap[containerId].Dispose();
                _deviceMap.Remove(containerId);
                _containerMap.Remove(devPath);
            }

            return 0;
        }

        public void Start(CancellationToken cancellationToken)
        {
            new Thread(async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var dev = _deviceQueue.Take();
                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    await InitDevice(dev);
                }
            }).Start();

            unsafe
            {
                HidHotplugRegisterCallback(0x046D, 0x00, HidApiHotPlugEvent.HID_API_HOTPLUG_EVENT_DEVICE_ARRIVED, HidApiHotPlugFlag.HID_API_HOTPLUG_ENUMERATE, EnqueueDevice, IntPtr.Zero, (int*)IntPtr.Zero);
                HidHotplugRegisterCallback(0x046D, 0x00, HidApiHotPlugEvent.HID_API_HOTPLUG_EVENT_DEVICE_LEFT, HidApiHotPlugFlag.NONE, DeviceLeft, IntPtr.Zero, (int*)IntPtr.Zero);
            }
        }
    }
}
