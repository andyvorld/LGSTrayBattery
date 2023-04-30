using System;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LGSTrayIPC
{
    public class IpcReader : IpcCore
    {
        public IpcReader(string namePrefix) : base(namePrefix, MemoryMappedFileAccess.ReadWrite)
        {
        }

        public async Task<byte[]> Read(CancellationToken token = default)
        {
            return await _channel.Reader.ReadAsync(token);
        }

        protected override async Task LoopTask()
        {
            while (!_cts.IsCancellationRequested)
            {
                _mmfFull.WaitOne();
                var payloadLength = _viewAccessor.ReadUInt16(0);
                byte[] payload = new byte[payloadLength];
                _viewAccessor.ReadArray(2, payload, 0, payloadLength);
                _mmfEmpty.Set();

                await _channel.Writer.WriteAsync(payload, _cts.Token);
            }
        }
    }
}
