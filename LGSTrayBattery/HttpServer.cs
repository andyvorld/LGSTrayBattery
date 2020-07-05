using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using IniParser;
using IniParser.Model;

namespace LGSTrayBattery
{
    class HttpServer
    {
        public static bool ServerEnabled;
        private static int _tcpPort;

        public static void LoadConfig()
        {
            var parser = new FileIniDataParser();

            if (!File.Exists("./HttpConfig.ini"))
            {
                File.Create("./HttpConfig.ini").Close();
            }

            IniData data = parser.ReadFile("./HttpConfig.ini");

            if (!bool.TryParse(data["HTTPServer"]["serverEnable"], out ServerEnabled))
            {
                data["HTTPServer"]["serverEnable"] = "false";
            }

            if (!int.TryParse(data["HTTPServer"]["tcpPort"], out _tcpPort))
            {
                data["HTTPServer"]["tcpPort"] = "12321";
            }

            parser.WriteFile("./HttpConfig.ini", data);
        }

        public static async Task ServerLoop(MainWindowViewModel viewmodel)
        {
            Debug.WriteLine("\nHttp Server started");

            IPHostEntry host = Dns.GetHostEntry("localhost");
            IPAddress ipAddress = host.AddressList[0];
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, _tcpPort);
  
            Socket listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(localEndPoint);
            listener.Listen(10);

            Debug.WriteLine($"Http Server listening on port {_tcpPort}\n");

            while (true)
            {
                using (Socket client = listener.Accept())
                {
                    var bytes = new byte[1024];
                    var bytesRec = client.Receive(bytes);

                    string httpRequest = Encoding.ASCII.GetString(bytes, 0, bytesRec);

                    var matches = Regex.Match(httpRequest, @"GET (.+?) HTTP\/[0-9\.]+");
                    if (matches.Groups.Count > 0)
                    {
                        int statusCode = 200;
                        string contentType = "text";
                        string content;

                        string[] request = matches.Groups[1].ToString().Split(new string[] {"/"}, StringSplitOptions.RemoveEmptyEntries);
                        switch (request[0])
                        {
                            case ("devices"):
                                contentType = "text/html";
                                content = "<html>";

                                foreach (var logiDevice in viewmodel.LogiDevices)
                                {
                                    content += $"{logiDevice.DeviceName} : <a href=\"/device/{logiDevice.UsbSerialId}\">{logiDevice.UsbSerialId}</a><br>";
                                }

                                content += "</html>";
                                break;
                            case ("device"):
                                if (request.Length < 2)
                                {
                                    statusCode = 400;
                                    content = "Missing device id";
                                }
                                else
                                {
                                    LogiDevice targetDevice =
                                        viewmodel.LogiDevices.FirstOrDefault(x => x.UsbSerialId == request[1]);

                                    if (targetDevice == null)
                                    {
                                        statusCode = 400;
                                        content = $"Device not found, ID = {request[1]}";
                                    }
                                    else
                                    {
                                        contentType = "text/xml";
                                        await targetDevice.UpdateBatteryPercentage();
                                        content = targetDevice.XmlData();
                                    }
                                }

                                break;
                            default:
                                statusCode = 400;
                                content = $"Requested {matches.Groups[1]}";
                                break;
                        }

                        string response = $"HTTP/1.1 {statusCode}\r\n";
                        response += $"{contentType}\r\n";

                        response += "Cache-Control: no-store, must-revalidate\r\n";
                        response += "Pragma: no-cache\r\n";
                        response += "Expires: 0\r\n";

                        response += $"\r\n{content}";

                        client.Send(Encoding.ASCII.GetBytes(response));
                    }
                }
            }
            // ReSharper disable once FunctionNeverReturns
        }
    }
}