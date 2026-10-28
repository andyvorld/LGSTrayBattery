using System.IO;

namespace LGSTrayHID
{
    /// <summary>
    /// Minimal file logger so problems can be diagnosed without a debugger attached.
    /// Writes next to the executable, rotates at ~1 MB.
    /// </summary>
    internal static class Log
    {
        private static readonly object _lock = new();
        private static readonly string _path = Path.Combine(AppContext.BaseDirectory, "LGSTrayHID.log");
        private static bool _disabled;

        public static void Write(string message)
        {
            if (_disabled) { return; }

            try
            {
                lock (_lock)
                {
                    FileInfo fi = new(_path);
                    if (fi.Exists && fi.Length > 1_000_000)
                    {
                        fi.Delete();
                    }

                    File.AppendAllText(_path, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {message}{Environment.NewLine}");
                }
            }
            catch
            {
                _disabled = true;
            }
        }
    }
}
