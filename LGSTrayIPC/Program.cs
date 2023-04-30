using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LGSTrayIPC
{
    internal class Program
    {
        public static async Task Main(string[] args)
        {
            string nameSpace = @"Local\LGSTRAY_TEST_1234";

            if (args.Contains("--daemon"))
            {
                IpcWriter ipc = new(nameSpace);

                int count = 0;
                System.Timers.Timer timer = new(5000);
                timer.Elapsed += delegate { Environment.Exit(0); };
                timer.Start();

                while (true)
                {
                    string payload = $"Hello {count}";

                    await ipc.Write(Encoding.ASCII.GetBytes(payload));
                    Console.WriteLine($"W: {payload}");

                    count++;
                    timer.Interval = 5000;
                    timer.Enabled = true;

                    await Task.Delay(100);
                }
            }

            var fork = new Process();
            {
                fork.StartInfo = new ProcessStartInfo()
                {
                    UseShellExecute = false,
                    FileName = Environment.ProcessPath,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                };
                fork.StartInfo.ArgumentList.Add("--daemon");

                fork.EnableRaisingEvents = true;
                fork.Exited += delegate { Console.WriteLine("Fork died"); };

                fork.Start();
                fork.OutputDataReceived += (sender, args) => { Console.WriteLine(args.Data); };
                fork.BeginOutputReadLine();
            }

            {
                //IpcReader ipc = new(nameSpace);

                while (true)
                {
                    //var payload = await ipc.Read();
                    //string payloadString = Encoding.ASCII.GetString(payload);

                    //Console.WriteLine($"R: {payloadString}");

                    await Task.Delay(100);
                }
            }

            //Thread t1 = new(async () =>
            //{
            //    IpcWriter ipc = new(nameSpace);

            //    int count = 0;
            //    while (true)
            //    {
            //        string payload = $"Hello {count}";
            //        Console.WriteLine($"W: {payload}");

            //        await ipc.Write(Encoding.ASCII.GetBytes(payload));

            //        count++;

            //        await Task.Delay(500);
            //    }
            //});

            //Thread t2 = new(async () =>
            //{
            //    IpcReader ipc = new(nameSpace);

            //    while (true)
            //    {
            //        var payload = await ipc.Read();
            //        string payloadString = Encoding.ASCII.GetString(payload);

            //        Console.WriteLine($"R: {payloadString}");
            //    }
            //});

            //t1.Start();
            //t2.Start();

            //t1.Join();
            //t2.Join();
        }
    }
}
