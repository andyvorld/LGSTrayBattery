using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LGSTrayCore
{
    public sealed class LogiDeviceCollection
    {
        #region Singleton defs
        private static readonly LogiDeviceCollection _instance = new();
        public static LogiDeviceCollection Instance => _instance;

        static LogiDeviceCollection() { }
        #endregion

        public ObservableCollection<LogiDevice> Devices { get; } = new();

        private LogiDeviceCollection()
        {
        }

        public bool TryGetDevice(string deviceId, [NotNullWhen(true)] out LogiDevice? device)
        {
            device = Devices.SingleOrDefault(x => x.DeviceId == deviceId);

            return device != null;
        }
    }
}
