using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LGSTrayCore
{
    public abstract class LogiDevice
    {
        public abstract string DeviceID { get; }
        public abstract string DeviceName { get; }
        public abstract double BatteryPercentage { get; }

        public abstract string GetXmlData();
    }
}
