namespace LGSTrayPrimitives
{
    public class AppSettings
    {
        public HttpServerSettings HTTPServer { get; set; } = null!;

        public IDeviceManagerSettings GHub { get; set; } = null!;

        public NativeDeviceManagerSettings Native { get; set; } = null!;
    }

    public class HttpServerSettings
    {
        public bool Enabled { get; set; }
        public int Port { get; set; }
        public string Addr { get; set; } = null!;

        public string UrlPrefix => $"http://{Addr}:{Port}/";
    }

    public class IDeviceManagerSettings
    {
        public bool Enabled { get; set; }
    }

    public class NativeDeviceManagerSettings : IDeviceManagerSettings
    {
        public int RetryTime { get; set; } = 10;
        public int PollPeriod { get; set; } = 600;

        public IEnumerable<string> DisabledDevices { get; set; } = [];
    }
}
