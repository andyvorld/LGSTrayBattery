using LGSTrayHID.HidApi;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using static LGSTrayHID.HidApi.HidApi;

namespace LGSTrayHID
{
    public class HidppDevices : IDisposable
    {
        public const byte SW_ID = 0x0A;
        private byte PING_PAYLOAD = 0x55;

        private bool _isReading = true;
        private const int READ_TIMEOUT = 100;

        private readonly Dictionary<ushort, HidppDevice> _deviceCollection = [];
        public IReadOnlyDictionary<ushort, HidppDevice> DeviceCollection => _deviceCollection;

        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private readonly Channel<byte[]> _channel = Channel.CreateBounded<byte[]>(new BoundedChannelOptions(32)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = false,
            SingleWriter = false,
        });

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

        private bool _shortReadThreadStarted;
        private bool _longReadThreadStarted;

        // 1 = short reports (wireless receiver), 2 = long reports (direct / wired device), 0 = unknown
        private int _requestMode;
        public int RequestMode => _requestMode;
        public string RequestModeName => _requestMode switch
        {
            1 => "short(7B)",
            2 => "long(20B)",
            _ => "unknown",
        };

        /// <summary>
        /// Device handle that HID++ 2.0 requests have to be sent to.
        /// Wired Logitech devices (e.g. a mouse plugged in for charging) only answer on the
        /// long report interface, the wireless receiver answers on the short one.
        /// </summary>
        public HidDevicePtr RequestTarget => _requestMode == 2 ? _devLong : _devShort;

        private int _disposeCount = 0;
        public bool Disposed => _disposeCount > 0;

        public HidppDevices() { }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (Interlocked.Increment(ref _disposeCount) == 1)
            {
                _isReading = false;

                _devShort = IntPtr.Zero;
                _devLong = IntPtr.Zero;
            }
        }

        ~HidppDevices()
        {
            Dispose(disposing: false);
        }

        public async Task SetDevShort(nint devShort)
        {
            if (devShort == IntPtr.Zero) { return; }
            if (_devShort == devShort) { return; }

            _devShort = devShort;
            await SetUp();
        }

        public async Task SetDevLong(nint devLong)
        {
            if (devLong == IntPtr.Zero) { return; }
            if (_devLong == devLong) { return; }

            _devLong = devLong;
            await SetUp();
        }

        private void StartReadThread(HidDevicePtr dev, int bufferSize, string name)
        {
            Thread thread = new(async () => { await ReadThread(dev, bufferSize, name); })
            {
                Priority = ThreadPriority.BelowNormal,
                IsBackground = true,
            };
            thread.Start();
        }

        private async Task ReadThread(HidDevicePtr dev, int bufferSize, string name)
        {
            byte[] buffer = new byte[bufferSize];

            try
            {
                while (_isReading)
                {
                    var ret = dev.Read(buffer, bufferSize, READ_TIMEOUT);
                    if (!_isReading) { break; }

                    if (ret < 0)
                    {
                        Log.Write($"read thread {name} stopped (device error)");
                        break;
                    }
                    else if (ret == 0)
                    {
                        continue;
                    }

                    await ProcessMessgage(buffer);
                }
            }
            catch (Exception ex)
            {
                Log.Write($"read thread {name} crashed: {ex.GetType().Name}: {ex.Message}");
            }

            HidClose(dev);
        }

        private async Task ProcessMessgage(byte[] buffer)
        {
            if ((buffer[2] == 0x41) && ((buffer[4] & 0x40) == 0))
            {
                // Device (re)connected
                byte deviceIdx = buffer[1];

                if (!_deviceCollection.TryGetValue(deviceIdx, out HidppDevice? device) || device == null)
                {
                    device = new(this, deviceIdx);
                    _deviceCollection[deviceIdx] = device;
                }

                Log.Write($"device {deviceIdx} announced on {RequestModeName}");

                HidppDevice target = device;
                new Thread(async () =>
                {
                    try
                    {
                        await Task.Delay(500);
                        await target.InitAsync();
                    }
                    catch (Exception ex)
                    {
                        Log.Write($"init of device {deviceIdx} failed: {ex.GetType().Name}: {ex.Message}");
                    }
                })
                { IsBackground = true }.Start();
            }
            else
            {
                try
                {
                    await _channel.Writer.WriteAsync(buffer);
                }
                catch (Exception ex)
                {
                    Log.Write($"channel write failed: {ex.Message}");
                }
            }
        }

        public async Task<byte[]> WriteRead10(HidDevicePtr hidDevicePtr, byte[] buffer, int timeout = 100)
        {
            ObjectDisposedException.ThrowIf(_disposeCount > 0, this);

            if (hidDevicePtr == IntPtr.Zero || _requestMode == 2)
            {
                // HID++ 1.0 queries are receiver-only
                return [];
            }

            bool locked = await _semaphore.WaitAsync(100);
            if (!locked)
            {
                return [];
            }

            try
            {
                await hidDevicePtr.WriteAsync((byte[])buffer);

                CancellationTokenSource cts = new();
                cts.CancelAfter(timeout);

                byte[] ret;
                while (!cts.IsCancellationRequested)
                {
                    try
                    {
                        ret = await _channel.Reader.ReadAsync(cts.Token);

                        if ((ret[2] == 0x8F) || (ret[2] == buffer[2]))
                        {
                            return ret;
                        }
                    }
                    catch (OperationCanceledException) { break; }
                }

                return [];
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<Hidpp20> WriteRead20(HidDevicePtr hidDevicePtr, Hidpp20 buffer, int timeout = 100, bool ignoreHID10 = true)
        {
            ObjectDisposedException.ThrowIf(_disposeCount > 0, this);

            HidDevicePtr target = _requestMode == 2 ? _devLong : hidDevicePtr;
            if (target == IntPtr.Zero || _requestMode == 0)
            {
                return (Hidpp20)Array.Empty<byte>();
            }

            if (_requestMode == 2 && timeout < 500)
            {
                // Wired devices can take a moment to wake up
                timeout = 500;
            }

            byte[] payload = (byte[])buffer;
            if (_requestMode == 2)
            {
                // Wired devices only implement HID++ 2.0 on the 20 byte report
                byte[] expanded = new byte[20];
                Array.Copy(payload, expanded, Math.Min(payload.Length, 20));
                expanded[0] = 0x11;
                payload = expanded;
            }

            bool locked = await _semaphore.WaitAsync(100);
            if (!locked)
            {
                return (Hidpp20)Array.Empty<byte>();
            }

            try
            {
                int wr = await target.WriteAsync(payload);

                CancellationTokenSource cts = new();
                cts.CancelAfter(timeout);

                Hidpp20 ret;
                int frames = 0;
                byte[]? last = null;
                while (!cts.IsCancellationRequested)
                {
                    try
                    {
                        ret = await _channel.Reader.ReadAsync(cts.Token);
                        frames++;
                        last = (byte[])ret;

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

                // Slot pings of an empty receiver are expected to fail, don't spam the log with them
                if ((buffer.GetFeatureIndex() != 0x00) || (_requestMode == 2))
                {
                    LogRequestProblem(
                        $"no answer feat=0x{buffer.GetFeatureIndex():X2} mode={RequestModeName} rx={frames} wr={wr} last={(last == null ? "-" : BitConverter.ToString(last, 0, Math.Min(last.Length, 7)))}");
                }

                return (Hidpp20)Array.Empty<byte>();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private int _requestProblemLogBudget = 25;

        private void LogRequestProblem(string message)
        {
            if (Interlocked.Decrement(ref _requestProblemLogBudget) < 0) { return; }

            Log.Write(message);
        }

        public async Task<bool> Ping20(byte deviceId, int timeout = 100, bool ignoreHIDPP10 = true)
        {
            ObjectDisposedException.ThrowIf(_disposeCount > 0, this);

            byte pingPayload = ++PING_PAYLOAD;
            Hidpp20 buffer = new byte[7] { 0x10, deviceId, 0x00, 0x10 | SW_ID, 0x00, 0x00, pingPayload };
            Hidpp20 ret = await WriteRead20(RequestTarget, buffer, timeout, ignoreHIDPP10);
            if (ret.Length == 0)
            {
                return false;
            }

            return (ret.GetFeatureIndex() == 0x00) && (ret.GetSoftwareId() == SW_ID) && (ret.GetParam(2) == pingPayload);
        }

        /// <summary>
        /// Sends a request and returns the first reply frame, without any filtering.
        /// Used to find out which report interface the attached device actually answers on.
        /// </summary>
        private async Task<byte[]?> RequestRawAsync(HidDevicePtr dev, byte[] payload, int timeout)
        {
            if (dev == IntPtr.Zero || !_isReading) { return null; }

            bool locked = await _semaphore.WaitAsync(200);
            if (!locked) { return null; }

            try
            {
                await dev.WriteAsync(payload);

                CancellationTokenSource cts = new();
                cts.CancelAfter(timeout);

                try
                {
                    return await _channel.Reader.ReadAsync(cts.Token);
                }
                catch (OperationCanceledException)
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                Log.Write($"probe failed: {ex.GetType().Name}: {ex.Message}");
                return null;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private async Task DetectRequestModeAsync()
        {
            if (_devShort != IntPtr.Zero)
            {
                // A wireless receiver answers the HID++ 1.0 "number of devices" query,
                // a wired device stays silent on the short interface.
                byte[]? ret = await RequestRawAsync(_devShort, [0x10, 0xFF, 0x81, 0x02, 0x00, 0x00, 0x00], 500);
                bool receiverAnswer = (ret != null) && (ret.Length > 3) && (ret[2] == 0x81) && (ret[1] == 0xFF);
                Log.Write($"short probe => {(ret == null ? "no reply" : BitConverter.ToString(ret, 0, Math.Min(ret.Length, 7)))}");

                if (receiverAnswer)
                {
                    _requestMode = 1;
                    return;
                }

                // The short interface is there but useless -> HID++ 2.0 has to use the long report
                if (_devLong != IntPtr.Zero)
                {
                    _requestMode = 2;
                    return;
                }

                // No long interface available, fall back to short
                _requestMode = 1;
                return;
            }

            if (_devLong != IntPtr.Zero)
            {
                _requestMode = 2;
            }
        }

        private async Task SetUp()
        {
            if (Disposed) { return; }
            if (_devShort == IntPtr.Zero && _devLong == IntPtr.Zero) { return; }

            if (_devShort != IntPtr.Zero && !_shortReadThreadStarted)
            {
                _shortReadThreadStarted = true;
                StartReadThread(_devShort, 7, "short");
            }

            if (_devLong != IntPtr.Zero && !_longReadThreadStarted)
            {
                _longReadThreadStarted = true;
                StartReadThread(_devLong, 20, "long");
            }

            await DetectRequestModeAsync();
            if (_requestMode == 0) { return; }

            Log.Write($"HID++ transport: {RequestModeName}");

            if (_requestMode == 1)
            {
                // Receiver: ask which slots are populated and force an arrival announcement
                byte[] ret = await WriteRead10(_devShort, [0x10, 0xFF, 0x81, 0x02, 0x00, 0x00, 0x00], 1000);
                byte numDeviceFound = 0;
                if (ret.Length > 5 && (ret[2] == 0x81) && (ret[3] == 0x02))
                {
                    numDeviceFound = ret[5];
                }

                if (numDeviceFound > 0)
                {
                    await WriteRead10(_devShort, [0x10, 0xFF, 0x80, 0x02, 0x02, 0x00, 0x00], 1000);
                }

                await Task.Delay(500);
            }

            if (_deviceCollection.Count == 0)
            {
                // Fail to enumerate devices -> ping every slot
                for (byte i = 1; i <= 6; i++)
                {
                    var ping = await Ping20(i, 200, false);
                    if (ping)
                    {
                        _deviceCollection[i] = new(this, i);

                        if (_requestMode == 2)
                        {
                            // A wired device answers on every index, one instance is enough
                            break;
                        }
                    }
                }

                foreach ((_, var device) in _deviceCollection)
                {
                    await device.InitAsync();
                }

                // A directly attached device answers on several indexes; keep one instance per
                // physical device (same identifier) to avoid duplicated polling.
                var seenIdentifiers = new HashSet<string>();
                foreach (var (idx, device) in _deviceCollection.ToList())
                {
                    if (string.IsNullOrEmpty(device.Identifier) || !seenIdentifiers.Add(device.Identifier))
                    {
                        Log.Write($"dropping duplicate device {idx} (id={device.Identifier})");
                        _deviceCollection.Remove(idx);
                    }
                }
            }
        }
    }
}
