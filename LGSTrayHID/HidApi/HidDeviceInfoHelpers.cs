using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace LGSTrayHID.HidApi
{
    internal static class HidDeviceInfoHelpers
    {
        internal static string GetPath(this HidDeviceInfo deviceInfo)
        {
            unsafe
            {
                return Marshal.PtrToStringAnsi((nint)deviceInfo.Path)!;
            }
        }
    }
}
