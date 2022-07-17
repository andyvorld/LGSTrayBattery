using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LGSTrayCore
{
    public abstract class LogiDeviceManager
    {
        protected ICollection<LogiDevice> _LogiDevices;
        public LogiDeviceManager(ICollection<LogiDevice> logiDevices)
        {
            this._LogiDevices = logiDevices;
        }

        public abstract Task LoadDevicesAsync();
        public abstract Task UpdateDevicesAsync();
    }
}
