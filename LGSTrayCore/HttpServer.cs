using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using IniParser;
using IniParser.Model;

namespace LGSTrayCore
{
    public class HttpServer
    {
        public static bool ServerEnabled = false;
        private static string _tcpAddr;
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

            _tcpAddr = data["HTTPServer"]["tcpAddr"];
            if (_tcpAddr == null)
            {
                data["HTTPServer"]["tcpAddr"] = "localhost";
            }

            parser.WriteFile("./HttpConfig.ini", data);
        }

        public static void ServeLoop(IEnumerable<IEnumerable<LogiDevice>> logiDevices)
        {
            Debug.WriteLine("\nHttp Server starting");

            IPAddress ipAddress;
            if (!IPAddress.TryParse(_tcpAddr, out ipAddress))
            {
                try
                {
                    IPHostEntry host = Dns.GetHostEntry(_tcpAddr);
                    ipAddress = host.AddressList[0];
                }
                catch (SocketException)
                {
                    Debug.WriteLine("Invalid hostname, defaulting to loopback");
                    ipAddress = IPAddress.Loopback;
                }
            }

            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, _tcpPort);

            Socket listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(localEndPoint);
            listener.Listen(10);

            Debug.WriteLine($"Http Server listening on {localEndPoint}\n");

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

                        string[] request = matches.Groups[1].ToString().Split(new string[] { "/" }, StringSplitOptions.RemoveEmptyEntries);

                        IEnumerable<LogiDevice> devices = logiDevices.SelectMany(x => x);

                        switch ((request.Length) > 0 ? request[0] : "")
                        {
                            case "devices":
                                contentType = "text/html";
                                content = "<html>";

                                foreach (var logiDevice in devices)
                                {
                                    content += $"{logiDevice.DeviceName} : <a href=\"/device/{logiDevice.DeviceID}\">{logiDevice.DeviceID}</a><br>";
                                }

                                content += "</html>";
                                break;
                            case "device":
                                if (request.Length < 2)
                                {
                                    statusCode = 400;
                                    content = "Missing device id";
                                }
                                else
                                {
                                    LogiDevice targetDevice =
                                        devices.FirstOrDefault(x => x.DeviceID == request[1]);

                                    if (targetDevice == null)
                                    {
                                        statusCode = 404;
                                        content = $"Device not found, ID = {request[1]}";
                                    }
                                    else
                                    {
                                        contentType = "text/xml";
                                        content = targetDevice.GetXmlData();
                                    }
                                }
                                break;
                            default:
                                statusCode = 400;
                                content = $"Requested {matches.Groups[1]}";
                                break;
                        }

                        string response = $"HTTP/1.1 {statusCode}\r\n";
                        response += $"ContentType: {contentType}\r\n";
                        response += $"Access-Control-Allow-Origin: *\r\n";
                        response += "Cache-Control: no-store, must-revalidate\r\n";
                        response += "Pragma: no-cache\r\n";
                        response += "Expires: 0\r\n";

                        response += $"\r\n{content}";

                        client.Send(Encoding.ASCII.GetBytes(response));
                    }
                }
            }
        }
    }
}