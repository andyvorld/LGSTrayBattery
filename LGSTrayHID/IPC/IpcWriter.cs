using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LGSTrayHID.IPC
{
    public sealed class IpcWriter
    {
        public LGSTrayIPC.IpcWriter? Writer { get; private set; }
        public IpcWriter Instance { get; } = new();

        private IpcWriter()
        {
        }

        static IpcWriter()
        {
        }

        public void InitWriter(string namePrefix)
        {
            Writer = new(namePrefix);
        }
    }
}
