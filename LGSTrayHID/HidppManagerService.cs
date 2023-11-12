using LGSTrayPrimitives.MessageStructs;
using MessagePipe;
using Microsoft.Extensions.Hosting;

namespace LGSTrayHID
{
    public class HidppManagerService : IHostedService
    {
        private readonly IDistributedPublisher<IPCMessageType, IPCMessage> _publisher;
        private readonly IDistributedSubscriber<IPCMessageRequestType, BatteryUpdateRequestMessage> _subscriber;

        public HidppManagerService(
            IDistributedPublisher<IPCMessageType, IPCMessage> publisher,
            IDistributedSubscriber<IPCMessageRequestType, BatteryUpdateRequestMessage> subscriber
            )
        {
            _publisher = publisher;
            _subscriber = subscriber;

            HidppManagerContext.Instance.HidppDeviceEvent += async (type, message) =>
            {
                if (message is InitMessage initMessage)
                {
                    Console.WriteLine(initMessage.deviceName);
                }

                await _publisher.PublishAsync(type, message);
            };
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            HidppManagerContext.Instance.Start(cancellationToken);

            _ = await _subscriber.SubscribeAsync(
                IPCMessageRequestType.BATTERY_UPDATE_REQUEST,
                x =>
                {
                    _ = HidppManagerContext.Instance.ForceBatteryUpdates();
                },
                cancellationToken
            );

            return;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
