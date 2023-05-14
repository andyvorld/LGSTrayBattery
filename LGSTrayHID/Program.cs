using LGSTrayHID.HidApi;
using System.Runtime.InteropServices;
using System.Text;

using static LGSTrayHID.HidApi.HidApi;
using static LGSTrayHID.HidApi.HidApiWinApi;
using static LGSTrayHID.HidApi.HidApiHotPlug;
using System.Collections.Concurrent;
using System.ComponentModel;
using LGSTrayCore;
using System.Diagnostics;

namespace LGSTrayHID
{
    internal class Program
    {
        static readonly HidppManagerContext context = new();

        //static fixed HidHotPlugCallbackHandle callback_handle;

        static async Task<int> _PrintDevice(HidDeviceInfo deviceInfo)
        {
            var messageType = (deviceInfo).GetHidppMessageType();
            switch (messageType)
            {
                case HidppMessageType.NONE:
                case HidppMessageType.VERY_LONG:
                    return 0;
            }

            string devPath = (deviceInfo).GetPath();
            Console.WriteLine(devPath);

            Guid containerId = new();
            HidDevicePtr dev;
            unsafe
            {
                dev = HidOpenPath((deviceInfo).Path);

                Task.Run(() =>
                {
                    Guid containerId = new();
                    _ = HidWinApiGetContainerId(IntPtr.Zero, &containerId);
                });

                //Console.WriteLine(containerId.ToString());
                //Console.WriteLine("x{0:X04}", (deviceInfo).Usage);
                //Console.WriteLine("x{0:X04}", (deviceInfo).UsagePage);
                Console.WriteLine();
            }


            //if (!context.DeviceMap.ContainsKey(containerId))
            //{
            //    context.DeviceMap[containerId] = new();
            //    containerMap[devPath] = containerId;
            //}

            //Guid _containerId = new (containerId.ToByteArray());
            //switch (messageType)
            //{
            //    case HidppMessageType.SHORT:
            //        await context.DeviceMap[_containerId].SetDevShort(dev);
            //        break;
            //    case HidppMessageType.LONG:
            //        await context.DeviceMap[_containerId].SetDevLong(dev);
            //        break;
            //}

            return 0;
        }

        static unsafe int PrintDevice(HidHotPlugCallbackHandle callbackHandle, HidDeviceInfo* deviceInfo, HidApiHotPlugEvent hidApiHotPlugEvent, nint userData)
        {
            return 0;
            //return _PrintDevice(*deviceInfo);
            //var messageType = (*deviceInfo).GetHidppMessageType();
            //switch (messageType)
            //{
            //    case HidppMessageType.NONE:
            //    case HidppMessageType.VERY_LONG:
            //        return 0;
            //}

            //Console.WriteLine((*deviceInfo).GetPath());

            //var dev = HidOpenPath((*deviceInfo).Path);

            //Guid containerId = new();
            //_ = HidWinApiGetContainerId(dev, &containerId);
            //Console.WriteLine(containerId.ToString());
            //Console.WriteLine("x{0:X04}", (*deviceInfo).Usage);
            //Console.WriteLine("x{0:X04}", (*deviceInfo).UsagePage);
            //Console.WriteLine();

            //if (!context.DeviceMap.ContainsKey(containerId))
            //{
            //    context.DeviceMap[containerId] = new();
            //}

            //switch (messageType)
            //{
            //    case HidppMessageType.SHORT:
            //        context.DeviceMap[containerId].DevShort = dev;
            //        break;
            //    case HidppMessageType.LONG:
            //        context.DeviceMap[containerId].DevLong = dev;
            //        break;
            //}

            //return 0;
        }

        static unsafe int DeviceLeft(HidHotPlugCallbackHandle callbackHandle, HidDeviceInfo* deviceInfo, HidApiHotPlugEvent hidApiHotPlugEvent, nint userData)
        {
            string devPath = (*deviceInfo).GetPath();

            if (containerMap.TryGetValue(devPath, out var containerId))
            {
                context.DeviceMap[containerId].Dispose();
                context.DeviceMap.Remove(containerId);
                containerMap.Remove(devPath);
            }


            return 0;
        }

        private static readonly Dictionary<string, Guid> containerMap = new();
        private static readonly BlockingCollection<HidDeviceInfo> queue = new();

        private static HidApiHotPlugEventCallbackFn asdf;

        static Semaphore _sem = new(0, 1, @"Local\LGSTray/daemonSync");

        static async Task Daemon()
        {
            Console.WriteLine("Daemon");

            _sem.Release();

            unsafe
            {
                Guid tmp;
                HidWinApiGetContainerId(0, &tmp);
            }
        }

        static async Task Main(string[] args)
        {
            Random rng = new Random();

            List<byte> string1 = new();
            List<byte> string2 = new();

            for (int i = 0; i < 256; i++)
            {
                string1.Add((byte)(97 + rng.Next(26)));
                string2.Add((byte)(97 + rng.Next(26)));
            }

            MessageStructs.UpdateStruct asdf = new()
            {
                deviceId = Encoding.ASCII.GetString(string1.ToArray()),
                batteryPercentage = 50,
                status = PowerSupplyStatus.POWER_SUPPLY_STATUS_FULL,
                batteryMVolt = 3400
            };

            var bytearr = asdf.ToByteArray();
            var zxcv = MessageStructs.UpdateStruct.FromByteArray(bytearr);

            Console.WriteLine("Hello, World!");

            //if (args.Contains("--daemon"))
            //{
            //    await Daemon();
            //    return;
            //}

            //var fork = new Process();
            //{
            //    fork.StartInfo = new ProcessStartInfo()
            //    {
            //        UseShellExecute = false,
            //        FileName = Environment.ProcessPath,
            //        CreateNoWindow = true,
            //        RedirectStandardOutput = true,
            //        RedirectStandardError = true,
            //    };
            //    fork.StartInfo.ArgumentList.Add("--daemon");

            //    fork.EnableRaisingEvents = true;
            //    fork.Exited += delegate { Console.WriteLine("Fork died"); };

            //    fork.Start();
            //    fork.OutputDataReceived += (sender, args) => { Console.WriteLine(args.Data); };
            //    fork.BeginOutputReadLine();

            //    _sem.WaitOne();
            //}

            //Console.WriteLine(Environment.ProcessPath);

            //while (true)
            //{
            //    await Task.Delay(1000);
            //}

            return;
            //unsafe
            //{
            //    asdf = (_, dev, eventType, _) =>
            //    {
            //        if (eventType == HidApiHotPlugEvent.HID_API_HOTPLUG_EVENT_DEVICE_ARRIVED)
            //        {
            //            queue.Add(*dev);
            //        }
            //        else
            //        {
            //            string path = (*dev).GetPath();

            //        }

            //        return 0;
            //    };

            //    int ret = HidInit();

            //    HidHotplugRegisterCallback(0x046D, 0x00, HidApiHotPlugEvent.HID_API_HOTPLUG_EVENT_DEVICE_ARRIVED, HidApiHotPlugFlag.HID_API_HOTPLUG_ENUMERATE, asdf, IntPtr.Zero, (int*) IntPtr.Zero);
            //    HidHotplugRegisterCallback(0x046D, 0x00, HidApiHotPlugEvent.HID_API_HOTPLUG_EVENT_DEVICE_LEFT, HidApiHotPlugFlag.NONE, DeviceLeft, IntPtr.Zero, (int*)IntPtr.Zero);
            //}

            //var tmp = LogiDeviceCollection.Instance.Devices;

            //new Thread(async () =>
            //{
            //    while (true)
            //    {
            //        foreach (var device in tmp)
            //        {
            //            await device.UpdateBatteryAsync();
            //        }
            //        await Task.Delay(10000);
            //    }
            //}).Start();

            //while (true)
            //{
            //    var dev = queue.Take();

            //    await _PrintDevice(dev);
            //}
        }
    }
}