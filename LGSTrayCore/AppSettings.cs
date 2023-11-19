namespace LGSTrayCore
{
    public class AppSettings
    {
        public HttpServerSettings HTTPServer { get; set; } = null!;

        public IDeviceManagerSettings GHub { get; set; } = null!;

        public NativeDeviceManagerSettings Native { get; set; } = null!;
    }

    public class HttpServerSettings
    {
        public bool ServerEnable { get; set; }
        public int Port { get; set; }
        public string ServerAddr { get; set; } = null!;

        public string UrlPrefix => $"http://{ServerAddr}:{Port}/";
    }

    public class IDeviceManagerSettings
    {
        public bool Enabled { get; set; }
    }

    public class NativeDeviceManagerSettings : IDeviceManagerSettings
    {

    }
}
