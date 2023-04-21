using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace LGSTrayHID.HidApi
{
    internal static partial class HidApiWinApi
    {
        [LibraryImport("hidapi", EntryPoint = "hid_winapi_get_container_id")]
        internal static unsafe partial int HidWinApiGetContainerId(nint dev, Guid* container_id);
    }
}
