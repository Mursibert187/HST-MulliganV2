using System;
using System.IO;
using System.Text;
using HstMulligan.Core.Abstractions;

namespace HstMulligan.Plugin.Diagnostics
{
    public sealed class FileLogger : ILogger, IDisposable
    {
        private readonly string _path;
        private readonly LogLevel _min;
        private readonly object _gate = new object();
        private const long MaxBytes = 2 * 1024 * 1024;

        public FileLogger(string path, LogLevel min = LogLevel.Info)
        {
            _path = path ?? throw new ArgumentNullException(nameof(path));
            _min = min;
            try
            {
                var dir = Path.GetDirectoryName(_path);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            }
            catch { }
        }

        public bool IsEnabled(LogLevel level) => level >= _min;

        public void Log(LogLevel level, string message, Exception exception = null)
        {
            if (!IsEnabled(level)) return;
            var sb = new StringBuilder();
            sb.Append(DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"))
              .Append(' ').Append(level.ToString().ToUpperInvariant())
              .Append(' ').Append(message ?? "");
            if (exception != null)
                sb.Append(" :: ").Append(exception);
            sb.AppendLine();
            try
            {
                lock (_gate)
                {
                    Rotate();
                    File.AppendAllText(_path, sb.ToString(), Encoding.UTF8);
                }
            }
            catch { }
        }

        private void Rotate()
        {
            try
            {
                var info = new FileInfo(_path);
                if (!info.Exists || info.Length <= MaxBytes) return;
                var backup = _path + ".1";
                if (File.Exists(backup)) File.Delete(backup);
                File.Move(_path, backup);
            }
            catch { }
        }

        public void Dispose() { }
    }
}
