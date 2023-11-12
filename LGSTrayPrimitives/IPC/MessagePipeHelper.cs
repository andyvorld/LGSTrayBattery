using MessagePipe;
using Microsoft.Extensions.DependencyInjection;

namespace LGSTrayPrimitives.IPC
{
    public static class MessagePipeHelper
    {
        private static string sockpath = Path.Join(AppContext.BaseDirectory, "LGSTrayIPC.sock");

        public static void AddLGSMessagePipe(this IServiceCollection services, bool hostAsServer = false)
        {
            services.AddMessagePipe(options =>
            {
                options.EnableCaptureStackTrace = true;
            });

            services.AddMessagePipeNamedPipeInterprocess("LGSTray", config =>
            {
                config.HostAsServer = hostAsServer;
            });
        }
    }
}
