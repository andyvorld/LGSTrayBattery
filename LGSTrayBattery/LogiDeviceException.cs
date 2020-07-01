using System;

namespace LGSTrayBattery
{
    public class LogiDeviceException : Exception
    {
        public LogiDeviceException()
        {
        }

        public LogiDeviceException(string message) : base(message)
        {

        }
    }
}