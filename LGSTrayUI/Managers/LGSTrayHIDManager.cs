using LGSTrayCore.MessageStructs;
using MessagePipe;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace LGSTrayUI.Managers
{
    public class LGSTrayHIDManager : IHostedService, IDisposable
    {
        private readonly CancellationTokenSource _cts = new();

        private readonly IDistributedSubscriber<IPCMessageType, IPCMessage> _subscriber;
        private readonly LogiDeviceCollection _logiDeviceCollection;
        private readonly LogiDeviceViewModelFactory _logiDeviceViewModelFactory;
        private readonly AppSettings _appSettings;

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

        public LGSTrayHIDManager(
            IDistributedSubscriber<IPCMessageType, IPCMessage> subscriber,
            LogiDeviceCollection logiDeviceCollection,
            LogiDeviceViewModelFactory logiDeviceViewModelFactory,
            IOptions<AppSettings> appSettings
        )
        {
            _subscriber = subscriber;
            _logiDeviceCollection = logiDeviceCollection;
            _logiDeviceViewModelFactory = logiDeviceViewModelFactory;
            _appSettings = appSettings.Value;
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
            if (!_appSettings.DeviceManager.Native)
            {
                return;
            }

            var sub1 = await _subscriber.SubscribeAsync(
                IPCMessageType.INIT,
                x =>
                {
                    var initMessage = (InitMessage)x;
                    _logiDeviceCollection.OnInitMessage(initMessage);
                },
                cancellationToken
            );

            var sub2 = await _subscriber.SubscribeAsync(
                IPCMessageType.UPDATE,
                x =>
                {
                    var updateMessage = (UpdateMessage)x;
                    _logiDeviceCollection.OnUpdateMessage(updateMessage);
                },
                cancellationToken
            );

            _diposeSubs = async () =>
            {
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

                    if ((DateTime.Now - then).TotalSeconds < 20)
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
