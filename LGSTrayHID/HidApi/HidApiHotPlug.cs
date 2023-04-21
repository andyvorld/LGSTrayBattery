global using HidHotPlugCallbackHandle = System.Int32;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace LGSTrayHID.HidApi
{
    [Flags]
    internal enum HidApiHotPlugEvent
    {
        /** A device has been plugged in and is ready to use */
        HID_API_HOTPLUG_EVENT_DEVICE_ARRIVED = (1 << 0),

        /** A device has left and is no longer available.
            It is the user's responsibility to call hid_close with a disconnected device.
        */
        HID_API_HOTPLUG_EVENT_DEVICE_LEFT = (1 << 1)
    }

    [Flags]
    internal enum HidApiHotPlugFlag
    {
        NONE = 0,
        /** Arm the callback and fire it for all matching currently attached devices. */
        HID_API_HOTPLUG_ENUMERATE = (1 << 0)
    }

    internal unsafe delegate int HidApiHotPlugEventCallbackFn(HidHotPlugCallbackHandle callbackHandle, HidDeviceInfo* device, HidApiHotPlugEvent hidApiHotPlugEvent, nint userData);

    internal static partial class HidApiHotPlug
    {
        [LibraryImport("hidapi", EntryPoint = "hid_hotplug_register_callback")]
        internal static unsafe partial int HidHotplugRegisterCallback(ushort vendor_id,
                                                                      ushort product_id,
                                                                      HidApiHotPlugEvent events,
                                                                      HidApiHotPlugFlag flags,
                                                                      HidApiHotPlugEventCallbackFn callback,
                                                                      nint user_data,
                                                                      HidHotPlugCallbackHandle* callback_handle);

        [LibraryImport("hidapi", EntryPoint = "hid_hotplug_deregister_callback")]
        internal static unsafe partial int HidHotplugDeregisterCallback(HidHotPlugCallbackHandle callback_handle);

    }
}
