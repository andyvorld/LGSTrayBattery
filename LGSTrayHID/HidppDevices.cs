using LGSTrayHID.HidApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

using static LGSTrayHID.HidApi.HidApi;

namespace LGSTrayHID
{
    public class HidppDevices
    {
        public const byte SW_ID = 0x0A;
        private const byte MAX_DEVICES = 6;
        private const byte PING_PAYLOAD = 0x55;

        private readonly Dictionary<ushort, HidppDevice> _deviceCollection = new();

        private bool _ready = false;
        private TaskCompletionSource<byte[]>? _tcs;

        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private readonly Channel<Hidpp20> _channel = Channel.CreateUnbounded<Hidpp20>();

        private HidDevicePtr _devShort = IntPtr.Zero;
        public HidDevicePtr DevShort
        {
            get => _devShort;
        }

        private HidDevicePtr _devLong = IntPtr.Zero;
        public HidDevicePtr DevLong
        {
            get => _devLong;
        }

        public HidppDevices() { }

        public async Task SetDevShort(nint devShort) { 
            _devShort = devShort;
            await SetUp();
        }

        public async Task SetDevLong(nint devLong)
        {
            _devLong = devLong;
            await SetUp();
        }

        private void ReadThread(ref HidDevicePtr dev, int bufferSize)
        {
            byte[] buffer = new byte[bufferSize];
            while(true)
            {
                var ret = dev.Read(buffer, bufferSize, -1);
                if (ret <= 0)
                {
                    break;
                }

                _ = ProcessMessgage((Hidpp20) buffer);
            }

            HidClose(dev);
            dev = IntPtr.Zero;
        }

        private async Task ProcessMessgage(Hidpp20 buffer)
        {
            await _channel.Writer.WriteAsync(buffer);
        }
        
        //public async Task<byte[]> WriteRead20(HidDevicePtr hidDevicePtr, byte[] buffer, int timeout = 1000)
        //{
        //    await hidDevicePtr.WriteAsync(buffer);

        //    return await _channel.Reader.ReadAsync();
        //}

        public async Task<Hidpp20> WriteRead20(HidDevicePtr hidDevicePtr, Hidpp20 buffer, int timeout = 100, bool ignoreHID10 = true)
        {
            try
            {
                bool locked = await _semaphore.WaitAsync(100);
                if (!locked)
                {
                    return (Hidpp20) Array.Empty<byte>();
                }

                await hidDevicePtr.WriteAsync((byte[]) buffer);

                CancellationTokenSource cts = new();
                cts.CancelAfter(timeout);

                Hidpp20 ret;
                while (!cts.IsCancellationRequested)
                {
                    try
                    {
                        ret = await _channel.Reader.ReadAsync(cts.Token);

                        if (!ignoreHID10 && (ret.GetFeatureIndex() == 0x8F))
                        {
                            // HID++ 1.0 response or timeout
                            break;
                        }

                        if ((ret.GetFeatureIndex() == buffer.GetFeatureIndex()) && (ret.GetSoftwareId() == SW_ID))
                        {
                            return ret;
                        }
                    }
                    catch (OperationCanceledException) { break; }
                }

                return (Hidpp20) Array.Empty<byte>();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private async Task SetUp()
        {
            if ((_devShort == IntPtr.Zero) || (_devLong == IntPtr.Zero))
            {
                return;
            }

            _ready = true;
            
            Console.WriteLine("Device ready");

            Thread t1 = new(() => { ReadThread(ref _devShort, 7); });
            t1.Priority = ThreadPriority.BelowNormal;
            t1.Start();

            Thread t2 = new(() => { ReadThread(ref _devLong, 20); });
            t2.Priority = ThreadPriority.BelowNormal;
            t2.Start();

            byte[] buf = new byte[7] { 0x10, 0x00, 0x00, 0x10 | SW_ID, 0x00, 0x00, PING_PAYLOAD,  };
            for (byte i = 0; i < MAX_DEVICES; i++)
            {
                buf[1] = i;
                byte _PING_PAYLOAD = (byte) (PING_PAYLOAD + i);
                buf[6] = _PING_PAYLOAD;

                Hidpp20 buffer = await WriteRead20(_devShort, buf, 1000, false);

                if ((buffer.Length > 0) && (buffer[2] == 0x00) && (buffer[3] == (0x10 | SW_ID)) && (buffer[6] == _PING_PAYLOAD))
                {
                    var deviceIdx = buffer.GetDeviceIdx();
                    _deviceCollection[deviceIdx] = new(this, deviceIdx);
                }

                _PING_PAYLOAD += 1;
            }

            foreach ((_, var device) in _deviceCollection)
            {
                await device.InitAsync();
            }
        }
    }
}
