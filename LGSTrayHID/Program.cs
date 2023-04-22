using LGSTrayHID.HidApi;
using System.Runtime.InteropServices;
using System.Text;

using static LGSTrayHID.HidApi.HidApi;
using static LGSTrayHID.HidApi.HidApiWinApi;
using static LGSTrayHID.HidApi.HidApiHotPlug;
using System.Collections.Concurrent;
using System.ComponentModel;

namespace LGSTrayHID
{
    internal class Program
    {
        static readonly HidppManagerContext context = new();

        //static fixed HidHotPlugCallbackHandle callback_handle;

        static async Task<int> _PrintDevice(HidDeviceInfo deviceInfo)
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
                dev = HidOpenPath((deviceInfo).Path);

                _ = HidWinApiGetContainerId(dev, &containerId);
                //Console.WriteLine(containerId.ToString());
                //Console.WriteLine("x{0:X04}", (deviceInfo).Usage);
                //Console.WriteLine("x{0:X04}", (deviceInfo).UsagePage);
                Console.WriteLine();
            }


            if (!context.DeviceMap.ContainsKey(containerId))
            {
                context.DeviceMap[containerId] = new();
                containerMap[devPath] = containerId;
            }

            Guid _containerId = new (containerId.ToByteArray());
            switch (messageType)
            {
                case HidppMessageType.SHORT:
                    await context.DeviceMap[_containerId].SetDevShort(dev);
                    break;
                case HidppMessageType.LONG:
                    await context.DeviceMap[_containerId].SetDevLong(dev);
                    break;
            }

            return 0;
        }

        static unsafe int PrintDevice(HidHotPlugCallbackHandle callbackHandle, HidDeviceInfo* deviceInfo, HidApiHotPlugEvent hidApiHotPlugEvent, nint userData)
        {
            return 0;
            //return _PrintDevice(*deviceInfo);
            //var messageType = (*deviceInfo).GetHidppMessageType();
            //switch (messageType)
            //{
            //    case HidppMessageType.NONE:
            //    case HidppMessageType.VERY_LONG:
            //        return 0;
            //}

            //Console.WriteLine((*deviceInfo).GetPath());

            //var dev = HidOpenPath((*deviceInfo).Path);

            //Guid containerId = new();
            //_ = HidWinApiGetContainerId(dev, &containerId);
            //Console.WriteLine(containerId.ToString());
            //Console.WriteLine("x{0:X04}", (*deviceInfo).Usage);
            //Console.WriteLine("x{0:X04}", (*deviceInfo).UsagePage);
            //Console.WriteLine();

            //if (!context.DeviceMap.ContainsKey(containerId))
            //{
            //    context.DeviceMap[containerId] = new();
            //}

            //switch (messageType)
            //{
            //    case HidppMessageType.SHORT:
            //        context.DeviceMap[containerId].DevShort = dev;
            //        break;
            //    case HidppMessageType.LONG:
            //        context.DeviceMap[containerId].DevLong = dev;
            //        break;
            //}

            //return 0;
        }

        static unsafe int DeviceLeft(HidHotPlugCallbackHandle callbackHandle, HidDeviceInfo* deviceInfo, HidApiHotPlugEvent hidApiHotPlugEvent, nint userData)
        {
            string devPath = (*deviceInfo).GetPath();

            if (containerMap.TryGetValue(devPath, out var containerId))
            {
                context.DeviceMap.Remove(containerId);
                containerMap.Remove(devPath);
            }


            return 0;
        }

        private static readonly Dictionary<string, Guid> containerMap = new();
        private static readonly BlockingCollection<HidDeviceInfo> queue = new();

        private static HidApiHotPlugEventCallbackFn asdf;

        static async Task Main(string[] args)
        {
            Console.WriteLine("Hello, World!");

            unsafe
            {
                asdf = (_, dev, eventType, _) =>
                {
                    if (eventType == HidApiHotPlugEvent.HID_API_HOTPLUG_EVENT_DEVICE_ARRIVED)
                    {
                        queue.Add(*dev);
                    }
                    else
                    {
                        string path = (*dev).GetPath();

                    }

                    return 0;
                };

                int ret = HidInit();

                HidHotplugRegisterCallback(0x046D, 0x00, HidApiHotPlugEvent.HID_API_HOTPLUG_EVENT_DEVICE_ARRIVED, HidApiHotPlugFlag.HID_API_HOTPLUG_ENUMERATE, asdf, IntPtr.Zero, (int*) IntPtr.Zero);
                HidHotplugRegisterCallback(0x046D, 0x00, HidApiHotPlugEvent.HID_API_HOTPLUG_EVENT_DEVICE_LEFT, HidApiHotPlugFlag.NONE, DeviceLeft, IntPtr.Zero, (int*)IntPtr.Zero);
            }

            while (true)
            {
                var dev = queue.Take();

                await _PrintDevice(dev);
            }
        }
    }
}