global using HidDevicePtr = System.IntPtr;
using System.Runtime.InteropServices;

namespace LGSTrayHID.HidApi
{
    internal static partial class HidApi
    {
        [LibraryImport("hidapi", EntryPoint = "hid_init")]
        internal static partial int HidInit();

        [LibraryImport("hidapi", EntryPoint = "hid_exit")]
        internal static partial int HidExit();

        [LibraryImport("hidapi", EntryPoint = "hid_enumerate")]
        internal static unsafe partial HidDeviceInfo* HidEnumerate(ushort vendor_id, ushort product_id);

        [LibraryImport("hidapi", EntryPoint = "hid_free_enumeration")]
        internal static unsafe partial void HidFreeEnumeration(HidDeviceInfo* devs);

        [LibraryImport("hidapi", EntryPoint = "hid_open_path")]
        internal static unsafe partial HidDevicePtr HidOpenPath(byte* path);

        [LibraryImport("hidapi", EntryPoint = "hid_close")]
        internal static unsafe partial void HidClose(HidDevicePtr dev);
    }
}
