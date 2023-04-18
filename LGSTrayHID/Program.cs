using LGSTrayHID.HidApi;
using System.Runtime.InteropServices;
using System.Text;

using static LGSTrayHID.HidApi.HidApi;
using static LGSTrayHID.HidApi.HidApiWinApi;
using static LGSTrayHID.HidApi.HidApiHotPlug;

namespace LGSTrayHID
{
    internal class Program
    {
        static unsafe int PrintDevice(HidHotPlugCallbackHandle callbackHandle, HidDeviceInfo* device, HidApiHotPlugEvent hidApiHotPlugEvent, nint userData)
        {
            Console.WriteLine((*device).GetPath());

            var dev = HidOpenPath((*device).Path);

            Guid containerId = new();
            _ = HidWinApiGetContainerId(dev, &containerId);
            Console.WriteLine(containerId.ToString());
            Console.WriteLine();


            return 0;
        }

        static void Main(string[] args)
        {
            Console.WriteLine("Hello, World!");

            unsafe
            {
                int ret = HidInit();

                HidHotPlugCallbackHandle callback_handle;
                HidHotplugRegisterCallback(0x00, 0x00, HidApiHotPlugEvent.HID_API_HOTPLUG_EVENT_DEVICE_ARRIVED, HidApiHotPlugFlag.HID_API_HOTPLUG_ENUMERATE, PrintDevice, IntPtr.Zero, &callback_handle);

                _ = HidExit();
            }

            while (true)
            {
                Thread.Sleep(100);
            }
        }
    }
}