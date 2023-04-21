using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LGSTrayHID
{
    public class HidppManagerContext
    {
        public Dictionary<Guid, HidppDevices> DeviceMap { get; init; }

        public HidppManagerContext()
        {
            DeviceMap = new();
        }
    }
}
