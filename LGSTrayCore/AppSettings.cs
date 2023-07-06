namespace LGSTrayCore
{
    public class AppSettings
    {
        public class HttpServerSettings
        {
            public bool ServerEnable { get; set; }
            public int Port { get; set; }
            public string ServerAddr { get; set; } = null!;

            public string UrlPrefix => $"http://{ServerAddr}:{Port}/";

            public HttpServerSettings() { }

            //public IPEndPoint IPEndPoint
            //{
            //    get
            //    {
            //        IPAddress ipAddress;
            //        if (ServerAddr == "localhost")
            //        {
            //            ipAddress = IPAddress.Loopback;
            //        }
            //        else if (!IPAddress.TryParse(ServerAddr, out ipAddress!))
            //        {
            //            try
            //            {
            //                IPHostEntry host = Dns.GetHostEntry(ServerAddr);
            //                ipAddress = host.AddressList[0];
            //            }
            //            catch (SocketException)
            //            {
            //                Debug.WriteLine("Invalid hostname, defaulting to loopback");
            //                ipAddress = IPAddress.Loopback;
            //            }
            //        }

            //        return new IPEndPoint(ipAddress, TcpPort);
            //    }
            //}
        }
        public HttpServerSettings HTTPServer { get; set; } = null!;

        public class DeviceManagerSettings
        {
            public bool GHUB { get; set; }
            public bool Native { get; set; }

            public DeviceManagerSettings() { }
        }
        public DeviceManagerSettings DeviceManager { get; set; } = null!;
    }
}
