using System.Runtime.InteropServices;

namespace LGSTrayHID.HidApi
{
    internal enum HidBusType : int
    {
        HID_API_BUS_UNKNOWN = 0x00,
        HID_API_BUS_USB = 0x01,
        HID_API_BUS_BLUETOOTH = 0x02,
        HID_API_BUS_I2C = 0x03,
        HID_API_BUS_SPI = 0x04,
    }

    [StructLayout(LayoutKind.Sequential)]
    internal readonly unsafe struct HidDeviceInfo
    {
        internal readonly byte* Path;
        internal readonly ushort VendorId;
        internal readonly ushort ProductId;
        internal readonly byte* SerialNumber;
        internal readonly ushort ReleaseNumber;
        internal readonly byte* ManufacturerString;
        internal readonly byte* ProductString;
        internal readonly ushort UsagePage;
        internal readonly ushort Usage;
        internal readonly int InterfaceNumber;
        internal readonly HidDeviceInfo* Next;
        internal readonly HidBusType BusType;
    }
}
