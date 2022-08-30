using LGSTrayCore;
using LGSTrayGHUB;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using Websocket.Client;

namespace LGSTrayGHUB
{
    public class NativeDeviceManager : GHUBDeviceManager
    {
        private const string WEBSOCKET_SERVER = "ws://localhost:9020";

        public NativeDeviceManager(ICollection<LogiDevice> logiDevices) : base(logiDevices)
        {
        }

        protected override async Task InitialiseWS()
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

            _ws?.Dispose();
            _ws = new WebsocketClient(url, factory);
            _ws.MessageReceived.Subscribe(ParseSocketMsg);
            _ws.ErrorReconnectTimeout = TimeSpan.FromMilliseconds(500);
            _ws.ReconnectTimeout = null;

            Debug.WriteLine($"Trying to connect to Native_agent, at {url}");

            try
            {
                await _ws.Start();
            }
            catch (Websocket.Client.Exceptions.WebsocketException)
            {
                Debug.WriteLine("Failed to connect to Native_agent");
                _ws?.Dispose();
                _ws = null;
                return;
            }

            Debug.WriteLine($"Connected to Native_agent");

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
        }

        protected override void _loadDevices(JObject payload)
        {
            _LogiDevices.Clear();

#pragma warning disable CS8602 // Dereference of a possibly null reference.
            foreach (var deviceToken in payload["deviceInfos"])
            {
                if (!Enum.TryParse(deviceToken["deviceType"].ToString(), true, out DeviceType deviceType))
                {
                    deviceType = DeviceType.Mouse;
                }

                LogiDeviceNative device = new()
                {
                    DeviceID = deviceToken["id"].ToString(),
                    DeviceName = deviceToken["extendedDisplayName"].ToString(),
                    DeviceType = deviceType
                };

                _LogiDevices.Add(device);
            }
#pragma warning restore CS8602 // Dereference of a possibly null reference.

            UpdateDevicesAsync().Wait();
        }

        protected override void _UpdateDevice(JObject payload)
        {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            LogiDeviceNative? device = _LogiDevices.FirstOrDefault(x => x.DeviceID == payload["deviceId"].ToString()) as LogiDeviceNative;

            if (device == null)
            {
                return;
            }

            device.BatteryPercentage = payload["percentage"].ToObject<double>();
            device.Charging = payload["charging"].ToObject<bool>();
#pragma warning restore CS8602 // Dereference of a possibly null reference.
        }
    }
}
