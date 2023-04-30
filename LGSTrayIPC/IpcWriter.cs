using System;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace LGSTrayIPC
{
    public class IpcWriter : IpcCore
    {
        private readonly int _timeout; 

        public IpcWriter(string namePrefix, int timeout = 1000) : base(namePrefix, MemoryMappedFileAccess.ReadWrite)
        {
        }

        public async Task Write(byte[] payload, CancellationToken token = default)
        {
            await _channel.Writer.WriteAsync(payload, token);
        }

        protected override async Task LoopTask()
        {
            while (!_cts.IsCancellationRequested)
            {
                var payload = await _channel.Reader.ReadAsync(_cts.Token);
                if (_cts.IsCancellationRequested)
                {
                    break;
                }

                ushort payloadLength = (ushort) payload.Length;

                if (!_mmfEmpty.WaitOne(_timeout))
                {

                }
                _viewAccessor.Write(0, ref payloadLength);
                _viewAccessor.WriteArray(2, payload, 0, payload.Length);
                _mmfFull.Set();
            }
        }
    }
}
