using LGSTrayHID.MessageStructs;
using MessagePipe;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LGSTrayHID
{
    public class HidppManagerService : IHostedService
    {
        private readonly IDistributedPublisher<IPCMessageType, IPCMessage> _publisher;

        public HidppManagerService(IDistributedPublisher<IPCMessageType, IPCMessage> publisher)
        {
            _publisher = publisher;

            HidppManagerContext.Instance.HidppDeviceEvent += async (type, message) =>
            {
                if (message is InitMessage initMessage)
                {
                    Console.WriteLine(initMessage.deviceName);
                }

                await _publisher.PublishAsync(type, message);
            };
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            HidppManagerContext.Instance.Start(cancellationToken);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
