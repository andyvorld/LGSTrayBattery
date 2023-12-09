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
        private static unsafe partial int _HidWinApiGetContainerId(nint dev, Guid* container_id);

        internal static unsafe int HidWinApiGetContainerId(nint dev, out Guid container_id)
        {
            Guid _container_id = new();
            int ret = _HidWinApiGetContainerId(dev, &_container_id);
            container_id = _container_id;

            return ret;
        }
    }
}
