using System.IO.MemoryMappedFiles;
using System.Runtime.Versioning;
using System.Threading.Channels;

namespace LGSTrayIPC
{
    public abstract class IpcCore : IDisposable
    {
        #region Disposable
        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _cts.Cancel();
                    _mmf.Dispose();
                    _viewAccessor.Dispose();
                }

                disposedValue = true;
            }
        }

        ~IpcCore()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        void IDisposable.Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion

        private static readonly long MMF_SIZE = 1024;

        protected readonly EventWaitHandle _mmfEmpty;
        protected readonly EventWaitHandle _mmfFull;
        protected readonly MemoryMappedFile _mmf;
        protected readonly MemoryMappedViewAccessor _viewAccessor;

        protected readonly CancellationTokenSource _cts = new();
        protected Thread _loopThread;
        protected Channel<byte[]> _channel = Channel.CreateBounded<byte[]>(options: new (1)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = false,
            SingleWriter = false,
        });

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "Feature not on Linux")]
        protected IpcCore(string namePrefix, MemoryMappedFileAccess mmfa)
        {
            _mmf = MemoryMappedFile.CreateOrOpen(namePrefix + "_mmf", MMF_SIZE, mmfa);

            _mmfEmpty = new(true, EventResetMode.AutoReset, namePrefix + "__mmfEmpty", out var createdNewEmpty);
            _mmfFull = new(false, EventResetMode.AutoReset, namePrefix + "__mmfFull", out var createdNewFull);

            _viewAccessor = _mmf.CreateViewAccessor();

            _loopThread = new Thread(async () =>
            {
                await LoopTask();
            });
            _loopThread.Start();
        }

        protected abstract Task LoopTask();
    }
}