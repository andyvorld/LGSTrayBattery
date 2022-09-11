using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace LGSTrayNative
{
    internal static class LGSTrayNative_bridge
    {
        [DllImport("LGSTrayNative_lib")]
        public static extern void Register_battery_update_cb(IntPtr cb);

        [DllImport("LGSTrayNative_lib")]
        public static extern void Register_device_ready_cb(IntPtr cb);

        [DllImport("LGSTrayNative_lib")]
        public static extern void Load_devices();

        [DllImport("LGSTrayNative_lib")]
        public static extern void Update_device_battery(string dev_id);
    }
}
