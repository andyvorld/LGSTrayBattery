using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using IniParser;
using IniParser.Model;

namespace LGSTrayCore
{
    public static class HttpServer
    {
        public static void ServeLoop(IEnumerable<IEnumerable<LogiDevice>> logiDevices, IPEndPoint localEndPoint)
        {
            Debug.WriteLine("\nHttp Server starting");

            Socket listener = new Socket(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                listener.Bind(localEndPoint);
            }
            catch (SocketException)
            {
                Debug.WriteLine($"Unable to bind to {localEndPoint}");
                return;
            }

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

                                content += "<br><hr>";

                                var version = Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
                                content += $"<i>LGSTray version: {version}</i><br>";
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
                        response += $"Content-Type: {contentType}\r\n";
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