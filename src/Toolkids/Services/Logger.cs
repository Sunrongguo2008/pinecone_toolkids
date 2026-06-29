using System;
using System.IO;
using System.Text;

namespace Toolkids.Services
{
    /// <summary>极简线程安全的文件日志。整个程序共用一个实例。</summary>
    public sealed class Logger
    {
        private readonly object _gate = new();
        private readonly string _logPath;

        public Logger(string logPath) => _logPath = logPath;

        public void Info(string message) => Write("INFO", message);

        public void Warn(string message) => Write("WARN", message);

        public void Error(string message, Exception? ex = null) =>
            Write("ERROR", ex == null ? message : message + " | " + ex);

        private void Write(string level, string message)
        {
            try
            {
                string line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {message}{Environment.NewLine}";
                lock (_gate)
                {
                    File.AppendAllText(_logPath, line, Encoding.UTF8);
                }
            }
            catch
            {
                // 日志失败绝不能影响主流程
            }
        }
    }
}
