using LGSTrayIPC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LGSTrayHID.IPC
{
    public sealed class IpcReader
    {
        private LGSTrayIPC.IpcReader? _ipcReader;
        private CancellationTokenSource? _cts;
        private Thread? _readLoop;

        public delegate void DeviceInitHandler(MessageStructs.InitStruct msg);
        public event DeviceInitHandler? DeviceInitEvent;

        public delegate void DeviceUpdateHandler(MessageStructs.UpdateStruct msg);
        public event DeviceUpdateHandler? DeviceUpdateEvent;

        public IpcReader Instance { get; } = new();

        private IpcReader()
        {
        }

        static IpcReader()
        {
        }

        public void BeginRead (string namePrefix)
        {
            _ipcReader = new LGSTrayIPC.IpcReader(namePrefix);
            _cts = new CancellationTokenSource();
            _readLoop = new Thread(async () =>
            {
                while (!_cts.IsCancellationRequested)
                {
                    var ret = await _ipcReader.Read(_cts.Token);
                    if (_cts.IsCancellationRequested)
                    {
                        break;
                    }

                    switch ((MessageStructs.MessageType)ret[0])
                    {
                        case MessageStructs.MessageType.INIT:
                            DeviceInitEvent?.Invoke(MessageStructs.InitStruct.FromByteArray(ret));
                            break;
                        case MessageStructs.MessageType.UPDATE:
                            DeviceUpdateEvent?.Invoke(MessageStructs.UpdateStruct.FromByteArray(ret));
                            break;
                    }
                }
            });
            _readLoop.Start();
        }
    }
}
