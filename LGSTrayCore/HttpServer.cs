using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
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

        public static void ServeLoop(ref IEnumerable<IEnumerable<LogiDevice>> logiDevices)
        {

        }
    }
}