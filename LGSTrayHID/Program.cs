using LGSTrayHID.HidApi;
using System.Runtime.InteropServices;
using System.Text;

using static LGSTrayHID.HidApi.HidApi;
using static LGSTrayHID.HidApi.HidApiWinApi;
using static LGSTrayHID.HidApi.HidApiHotPlug;
using System.Collections.Concurrent;
using System.ComponentModel;
using LGSTrayPrimitives;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using MessagePipe;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using LGSTrayPrimitives.IPC;

namespace LGSTrayHID
{
    internal class Program
    {
        //static readonly HidppManagerService context = new();

        //static async Task<int> _PrintDevice(HidDeviceInfo deviceInfo)
        //{
        //    var messageType = (deviceInfo).GetHidppMessageType();
        //    switch (messageType)
        //    {
        //        case HidppMessageType.NONE:
        //        case HidppMessageType.VERY_LONG:
        //            return 0;
        //    }

        //    string devPath = (deviceInfo).GetPath();
        //    Console.WriteLine(devPath);

        //    Guid containerId = new();
        //    HidDevicePtr dev;
        //    unsafe
        //    {
        //        dev = HidOpenPath((deviceInfo).Path);

        //        Task.Run(() =>
        //        {
        //            Guid containerId = new();
        //            _ = HidWinApiGetContainerId(IntPtr.Zero, &containerId);
        //        });

        //        //Console.WriteLine(containerId.ToString());
        //        //Console.WriteLine("x{0:X04}", (deviceInfo).Usage);
        //        //Console.WriteLine("x{0:X04}", (deviceInfo).UsagePage);
        //        Console.WriteLine();
        //    }


        //    //if (!context.DeviceMap.ContainsKey(containerId))
        //    //{
        //    //    context.DeviceMap[containerId] = new();
        //    //    containerMap[devPath] = containerId;
        //    //}

        //    //Guid _containerId = new (containerId.ToByteArray());
        //    //switch (messageType)
        //    //{
        //    //    case HidppMessageType.SHORT:
        //    //        await context.DeviceMap[_containerId].SetDevShort(dev);
        //    //        break;
        //    //    case HidppMessageType.LONG:
        //    //        await context.DeviceMap[_containerId].SetDevLong(dev);
        //    //        break;
        //    //}

        //    return 0;
        //}

        //static unsafe int DeviceLeft(HidHotPlugCallbackHandle callbackHandle, HidDeviceInfo* deviceInfo, HidApiHotPlugEvent hidApiHotPlugEvent, nint userData)
        //{
        //    string devPath = (*deviceInfo).GetPath();

        //    if (containerMap.TryGetValue(devPath, out var containerId))
        //    {
        //        context.DeviceMap[containerId].Dispose();
        //        context.DeviceMap.Remove(containerId);
        //        containerMap.Remove(devPath);
        //    }


        //    return 0;
        //}

        //private static readonly Dictionary<string, Guid> containerMap = new();
        //private static readonly BlockingCollection<HidDeviceInfo> queue = new();

        //private static HidApiHotPlugEventCallbackFn asdf;

        //static Semaphore _sem = new(0, 1, @"Local\LGSTray/daemonSync");

        //static async Task Daemon()
        //{
        //    Console.WriteLine("Daemon");

        //    _sem.Release();

        //    unsafe
        //    {
        //        Guid tmp;
        //        HidWinApiGetContainerId(0, &tmp);
        //    }
        //}

        static async Task Main(string[] args)
        {
            var builder = Host.CreateEmptyApplicationBuilder(null);

            builder.Services.AddLGSMessagePipe();
            builder.Services.AddHostedService<HidppManagerService>();

            var host = builder.Build();

            _ = Task.Run(async () =>
            {
                bool ret = int.TryParse(args.ElementAtOrDefault(0), out int parentPid);
                if (!ret) { return; }

                await Process.GetProcessById(parentPid).WaitForExitAsync();

                CancellationTokenSource cts = new(5000);
                await host.StopAsync(cts.Token);

                Environment.Exit(0);
            });

            await host.RunAsync();
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