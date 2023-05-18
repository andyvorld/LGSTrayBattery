using LGSTrayCore;
using LGSTrayHID.MessageStructs;
using MessagePipe;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace LGSTrayUI
{
    public class LGSTrayHIDDaemon : IHostedService, IDisposable
    {
        private readonly CancellationTokenSource _cts = new();

        private readonly IDistributedSubscriber<IPCMessageType, IPCMessage> _subscriber;
        private readonly LogiDeviceCollection _logiDeviceCollection;
        private readonly LogiDeviceViewModelFactory _logiDeviceViewModelFactory;

        private Func<Task>? _diposeSubs;
        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _ = _diposeSubs?.Invoke();
                    _diposeSubs = null;
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~LGSTrayHIDDaemon()
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

        public LGSTrayHIDDaemon(
            IDistributedSubscriber<IPCMessageType, IPCMessage> subscriber, 
            LogiDeviceCollection logiDeviceCollection,
            LogiDeviceViewModelFactory logiDeviceViewModelFactory
        )
        {
            _subscriber = subscriber;
            _logiDeviceCollection = logiDeviceCollection;
            _logiDeviceViewModelFactory = logiDeviceViewModelFactory;
        }

        private async Task DaemonLoop()
        {
            using Process proc = new();
            proc.StartInfo = new()
            {
                RedirectStandardError = false,
                RedirectStandardInput = false,
                RedirectStandardOutput = false,
                FileName = Path.Combine(AppContext.BaseDirectory, "LGSTrayHID.exe"),
                Arguments = Environment.ProcessId.ToString(),
                UseShellExecute = true,
                CreateNoWindow = true
            };
            proc.Start();

            try
            {
                await proc.WaitForExitAsync(_cts.Token);
            }
            catch (Exception)
            {
                if (!proc.HasExited)
                {
                    proc.Kill();
                }
            }

            await Task.Delay(1000);
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var sub1 = await _subscriber.SubscribeAsync(
                IPCMessageType.INIT,
                x =>
                {
                    var initMessage = (InitMessage)x;

                    LogiDeviceViewModel? dev = _logiDeviceCollection.Devices.SingleOrDefault(x => x.DeviceId == initMessage.deviceId);
                    if (dev != null)
                    {
                        Application.Current.Dispatcher.BeginInvoke(() =>
                        {
                            dev.DeviceName = initMessage.deviceName;
                            dev.HasBattery = initMessage.hasBattery;
                            dev.DeviceType = initMessage.deviceType;
                        });

                        return;
                    }

                    dev = _logiDeviceViewModelFactory.CreateViewModel((x) =>
                    {
                        x.DeviceId = initMessage.deviceId;
                        x.DeviceName = initMessage.deviceName;
                        x.HasBattery = initMessage.hasBattery;
                        x.DeviceType = initMessage.deviceType;
                    });

                    Application.Current.Dispatcher.BeginInvoke(() => _logiDeviceCollection.Devices.Add(dev));
                },
                cancellationToken
            );

            var sub2 = await _subscriber.SubscribeAsync(
                IPCMessageType.UPDATE,
                x =>
                {
                    var updateMessage = (UpdateMessage)x;

                    Application.Current.Dispatcher.BeginInvoke(() =>
                    {
                        var device = _logiDeviceCollection.Devices.FirstOrDefault(dev => dev.DeviceId == updateMessage.deviceId);
                        if (device == null) { return; }

                        device.BatteryPercentage = updateMessage.batteryPercentage;
                        device.PowerSupplyStatus = updateMessage.powerSupplyStatus;
                        device.BatteryVoltage = updateMessage.batteryMVolt * 1000;
                        device.LastUpdate = DateTime.Now;
                    });
                },
                cancellationToken
            );

            _diposeSubs = async () => {
                await sub1.DisposeAsync();
                await sub2.DisposeAsync();
            };

            _ = Task.Run(async () =>
            {
                int fastFailCount = 0;

                while (!_cts.Token.IsCancellationRequested)
                {
                    DateTime then = DateTime.Now;
                    await DaemonLoop();

                    if ((DateTime.Now - then).TotalSeconds < 5)
                    {
                        fastFailCount++;
                    }
                    else
                    {
                        fastFailCount = 0;
                    }

                    if (fastFailCount > 3)
                    {
                        // Notify user?
                        break;
                    }
                }
            }, CancellationToken.None);

            return;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _cts.Cancel();

            return Task.CompletedTask;
        }
    }
}
