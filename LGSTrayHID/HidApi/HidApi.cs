using System.Runtime.InteropServices;

namespace LGSTrayHID.HidApi
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct HidApiVersion
    {
        readonly int Major;
        readonly int Minor;
        readonly int Patch;

        public override readonly string ToString()
        {
            return $"{Major}.{Minor}.{Patch}";
        }
    }

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
        private static unsafe partial nint _HidOpenPath(byte* path);

        internal static unsafe nint HidOpenPath(ref HidDeviceInfo dev)
        {
            return _HidOpenPath(dev.Path);
        }

        [LibraryImport("hidapi", EntryPoint = "hid_close")]
        internal static unsafe partial void HidClose(nint dev);

        [LibraryImport("hidapi", EntryPoint = "hid_write")]
        internal static unsafe partial int HidWrite(nint dev, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] byte[] data, nuint length);

        [LibraryImport("hidapi", EntryPoint = "hid_read_timeout")]
        internal static unsafe partial int HidReadTimeOut(nint dev, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] byte[] data, nuint length, int milliseconds);

        [LibraryImport("hidapi", EntryPoint = "hid_version")]
        private static unsafe partial HidApiVersion* _HidVersion();

        internal unsafe static HidApiVersion HidVersion()
        {
            return *_HidVersion();
        }
    }
}
