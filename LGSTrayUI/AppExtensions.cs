using System;
using System.Diagnostics;
using Winmdroot = Windows.Win32;

namespace LGSTrayUI;

public static class AppExtensions
{
    public static unsafe void EnableEfficiencyMode()
    {
        if (OperatingSystem.IsWindowsVersionAtLeast(8))
        {
            var handle = Process.GetCurrentProcess().SafeHandle;
            Winmdroot.PInvoke.SetPriorityClass(handle, Winmdroot.System.Threading.PROCESS_CREATION_FLAGS.IDLE_PRIORITY_CLASS);

            Winmdroot.System.Threading.PROCESS_POWER_THROTTLING_STATE state = new()
            {
                Version = Winmdroot.PInvoke.PROCESS_POWER_THROTTLING_CURRENT_VERSION,
                ControlMask = Winmdroot.PInvoke.PROCESS_POWER_THROTTLING_EXECUTION_SPEED,
                StateMask = Winmdroot.PInvoke.PROCESS_POWER_THROTTLING_EXECUTION_SPEED,
            };

            Winmdroot.PInvoke.SetProcessInformation(
                handle,
                Winmdroot.System.Threading.PROCESS_INFORMATION_CLASS.ProcessPowerThrottling,
                &state,
                (uint)sizeof(Winmdroot.System.Threading.PROCESS_POWER_THROTTLING_STATE)
            );
        }
    }

}
