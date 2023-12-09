using LGSTrayPrimitives;
using LGSTrayPrimitives.MessageStructs;
using MessagePipe;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Swan;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Text.RegularExpressions;
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

    public partial class GHubManager : IDeviceManager, IHostedService, IDisposable
    {
        #region IDisposable
        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _ws?.Dispose();
                    _ws = null;
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

        [GeneratedRegex(@"\/battery\/dev[0-9a-zA-Z]+\/state")]
        private static partial Regex BatteryDeviceStateRegex();

        private readonly IPublisher<IPCMessage> _deviceEventBus;

        protected WebsocketClient? _ws;

        public GHubManager(IPublisher<IPCMessage> deviceEventBus)
        {
            _deviceEventBus = deviceEventBus;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
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
            _ws?.Dispose();

            return Task.CompletedTask;
        }

        public void LoadDevices()
        {
            _ws?.Send(JsonConvert.SerializeObject(new
            {
                msgId = "",
                verb = "GET",
                path = "/devices/list"
            }));
        }

        protected void ParseSocketMsg(ResponseMessage msg)
        {
            GHUBMsg ghubmsg = GHUBMsg.DeserializeJson(msg.Text!);

            switch (ghubmsg.Path)
            {
                case "/devices/list":
                    {
                        LoadDevices(ghubmsg.Payload);
                        break;
                    }
                case "/battery/state/changed":
                case { } when BatteryDeviceStateRegex().Match(ghubmsg.Path).Success:
                    {
                        Console.WriteLine(ghubmsg.Path);
                        ParseBatteryUpdate(ghubmsg.Payload);
                        break;
                    }
                default: break;
            }
        }

        protected void LoadDevices(JObject payload)
        {
            try
            {
                foreach (var deviceToken in payload["deviceInfos"]!)
                {
                    if (!Enum.TryParse(deviceToken["deviceType"]!.ToString(), true, out DeviceType deviceType))
                    {
                        deviceType = DeviceType.Mouse;
                    }

                    string deviceId = deviceToken["id"]!.ToString();
                    _deviceEventBus.Publish(new InitMessage(
                        deviceId,
                        deviceToken["extendedDisplayName"]!.ToString(),
                        (bool) deviceToken["capabilities"]!["hasBatteryStatus"]!,
                        deviceType
                    ));

                    _ws?.Send(JsonConvert.SerializeObject(new
                    {
                        msgId = "",
                        verb = "GET",
                        path = $"/battery/{deviceId}/state"
                    }));
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

        protected void ParseBatteryUpdate(JObject payload)
        {
            try
            {
                _deviceEventBus.Publish(new UpdateMessage(
                    payload["deviceId"]!.ToString(),
                    payload["percentage"]!.ToObject<int>(),
                    payload["charging"]!.ToBoolean() ? PowerSupplyStatus.POWER_SUPPLY_STATUS_CHARGING : PowerSupplyStatus.POWER_SUPPLY_STATUS_NOT_CHARGING,
                    0,
                    DateTime.Now,
                    payload["mileage"]!.ToObject<double>()
                ));
            }
            catch { }
        }

        public async void RediscoverDevices()
        {
            using var cts = new CancellationTokenSource();
            await StopAsync(cts.Token);
            await StartAsync(cts.Token);
        }
    }
}
