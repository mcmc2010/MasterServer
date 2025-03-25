
using System.IO;
using Microsoft.Extensions.Logging;

namespace Logger.Extensions {

    /// <summary>
    /// 
    /// </summary>
    public class FileLogger : Microsoft.Extensions.Logging.ILogger
    {

        private readonly string _pathname;
        private readonly string _filename;

        public FileLogger(string pathname, string filename)
        {
            _pathname = pathname;
            _filename = filename;

            if(_pathname.Length > 0 && !Path.Exists(_pathname))
            {
                Directory.CreateDirectory(_pathname);
            }
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => true;

        public void Log<TState>(
            Microsoft.Extensions.Logging.LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            var message = formatter(state, exception);

            string filename = Path.GetFileNameWithoutExtension(_filename);
            filename = $"{filename}_{DateTime.Now:yyyy-MM-dd}{Path.GetExtension(_filename)}";
            filename = Path.Join(_pathname, filename);
            string content = $"{DateTime.Now:HH:mm:ss} [{logLevel}] {message}{Environment.NewLine}";
            File.AppendAllText(filename, content);
        }
    }

    public class FileLoggerProvider : Microsoft.Extensions.Logging.ILoggerProvider
    {
        private readonly string _pathname;
        private readonly string _filename;

        public FileLoggerProvider(string filename)
        {
            string fullname = Path.GetFullPath(filename);
            _pathname = Path.GetDirectoryName(fullname)??"";
            _filename = Path.GetFileName(fullname);
        }

        public Microsoft.Extensions.Logging.ILogger CreateLogger(string name) 
        {
            return new FileLogger(_pathname, _filename);
        }

        public void Dispose() { }
    }

}