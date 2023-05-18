using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace LGSTrayUI
{
    public class AppSettings
    {
        public class HttpServerSettings
        {
            public bool ServerEnable { get; set; }
            public int TcpPort { get; set; }
            public string ServerAddr { get; set; } = null!;

            private HttpServerSettings() { }

            public IPEndPoint IPEndPoint
            {
                get
                {
                    IPAddress ipAddress;
                    if (ServerAddr == "localhost")
                    {
                        ipAddress = IPAddress.Loopback;
                    }
                    else if (!IPAddress.TryParse(ServerAddr, out ipAddress!))
                    {
                        try
                        {
                            IPHostEntry host = Dns.GetHostEntry(ServerAddr);
                            ipAddress = host.AddressList[0];
                        }
                        catch (SocketException)
                        {
                            Debug.WriteLine("Invalid hostname, defaulting to loopback");
                            ipAddress = IPAddress.Loopback;
                        }
                    }

                    return new IPEndPoint(ipAddress, TcpPort);
                }
            }
        }
        public HttpServerSettings HTTPServer { get; set; } = null!;

        public class DeviceManagerSettings
        {
            public bool GHUB { get; set; }
            public bool Native { get; set; }

            private DeviceManagerSettings() { }
        }
        public DeviceManagerSettings DeviceManager { get; set; } = null!;
    }
}
