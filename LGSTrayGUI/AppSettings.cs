using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace LGSTrayGUI
{
    public static class AppSettings
    {
        public static AppSettingsInstace Setting { get; set; }
        public class AppSettingsInstace
        {
            public HTTPServerSettings HTTPServer { get; set; }
            public class HTTPServerSettings
            {
                public bool serverEnable { get; set; }
                public int tcpPort { get; set; }
                public string tcpAddr { get; set; }

                public IPEndPoint IPEndPoint
                {
                    get
                    {
                        IPAddress ipAddress;
                        if (tcpAddr == "localhost")
                        {
                            ipAddress = IPAddress.Loopback;
                        }
                        else if (!IPAddress.TryParse(tcpAddr, out ipAddress))
                        {
                            try
                            {
                                IPHostEntry host = Dns.GetHostEntry(tcpAddr);
                                ipAddress = host.AddressList[0];
                            }
                            catch (SocketException)
                            {
                                Debug.WriteLine("Invalid hostname, defaulting to loopback");
                                ipAddress = IPAddress.Loopback;
                            }
                        }

                        return new IPEndPoint(ipAddress, tcpPort);
                    }
                }
            }

            public DeviceManagerSettings DeviceManager { get; set; }
            public class DeviceManagerSettings
            {
                public bool GHUB { get; set; }
                public bool HID_NET { get; set; }
                public bool Native { get; set; }
            }
        }
    }
}
