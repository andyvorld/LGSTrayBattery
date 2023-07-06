using LGSTrayCore.MessageStructs;
using MessagePipe;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Net.WebSockets;
using Websocket.Client;

namespace LGSTrayCore.Managers
{
    file struct GHUBMsg
    {
        public string MsgId { get; set; }
        public string Verb { get; set; }
        public string Path { get; set; }
        public string Origin { get; set; }
        public JObject Result { get; set; }
        public JObject Payload { get; set; }

        public static GHUBMsg DeserializeJson(string json)
        {
            return JsonConvert.DeserializeObject<GHUBMsg>(json);
        }
    }

    public class GHubManager : IHostedService, IDisposable
    {
        #region IDisposable
        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _ws.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~GHubManager()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion

        private const string WEBSOCKET_SERVER = "ws://localhost:9010";
        private const string DEVICE_REGEX = @"dev[0-9a-zA-Z]+";

        private readonly IDistributedSubscriber<IPCMessageType, IPCMessage> _subscriber;
        private readonly ILogiDeviceCollection _logiDeviceCollection;
        private readonly AppSettings _appSettings;

        protected WebsocketClient _ws = null!;

        public GHubManager(
            IDistributedSubscriber<IPCMessageType, IPCMessage> subscriber,
            ILogiDeviceCollection logiDeviceCollection,
            IOptions<AppSettings> appSettings
        )
        {
            _subscriber = subscriber;
            _logiDeviceCollection = logiDeviceCollection;
            _appSettings = appSettings.Value;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (!_appSettings.DeviceManager.GHUB)
            {
                return;
            }

            var url = new Uri(WEBSOCKET_SERVER);

            var factory = new Func<ClientWebSocket>(() =>
            {
                var client = new ClientWebSocket();
                client.Options.UseDefaultCredentials = false;
                client.Options.SetRequestHeader("Origin", "file://");
                client.Options.SetRequestHeader("Pragma", "no-cache");
                client.Options.SetRequestHeader("Cache-Control", "no-cache");
                client.Options.SetRequestHeader("Sec-WebSocket-Extensions", "permessage-deflate; client_max_window_bits");
                client.Options.SetRequestHeader("Sec-WebSocket-Protocol", "json");
                client.Options.AddSubProtocol("json");
                return client;
            });

            _ws = new WebsocketClient(url, factory);
            _ws.MessageReceived.Subscribe(ParseSocketMsg);
            _ws.ErrorReconnectTimeout = TimeSpan.FromMilliseconds(500);
            _ws.ReconnectTimeout = null;

            Debug.WriteLine($"Trying to connect to LGHUB_agent, at {url}");

            try
            {
                await _ws.Start();
            }
            catch (Websocket.Client.Exceptions.WebsocketException)
            {
                Debug.WriteLine("Failed to connect to LGHUB_agent");
                this.Dispose();
                return;
            }

            Debug.WriteLine($"Connected to LGHUB_agent");

            _ws.Send(JsonConvert.SerializeObject(new
            {
                msgId = "",
                verb = "SUBSCRIBE",
                path = "/devices/state/changed"
            }));

            _ws.Send(JsonConvert.SerializeObject(new
            {
                msgId = "",
                verb = "SUBSCRIBE",
                path = "/battery/state/changed"
            }));

            LoadDevices();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public void LoadDevices()
        {
            _ws.Send(JsonConvert.SerializeObject(new
            {
                msgId = "",
                verb = "GET",
                path = "/devices/list"
            }));
        }

        protected void ParseSocketMsg(ResponseMessage msg)
        {
            GHUBMsg ghubmsg = GHUBMsg.DeserializeJson(msg.Text);

            if (ghubmsg.Path == "/devices/state/changed")
            {

            }
            else if (ghubmsg.Path == "/devices/list")
            {
                _loadDevices(ghubmsg.Payload);
            }
        }

        protected void _loadDevices(JObject payload)
        {
            try
            {
                foreach (var deviceToken in payload["deviceInfos"]!)
                {
                    if (!Enum.TryParse(deviceToken["deviceType"]!.ToString(), true, out DeviceType deviceType))
                    {
                        deviceType = DeviceType.Mouse;
                    }

                    _logiDeviceCollection.OnInitMessage(new(
                        deviceToken["id"]!.ToString(),
                        deviceToken["extendedDisplayName"]!.ToString(),
                        (bool) deviceToken["capabilities"]!["hasBatteryStatus"]!,
                        deviceType
                    ));
                }
            }
            catch (Exception e)
            {
                if (e is NullReferenceException || e is JsonReaderException)
                {
                    Debug.WriteLine("Failed to parse device list, LGHUB_agent is probably starting up");
                }
            }
        }
    }
}
