

namespace Logger {
    public enum LogLevel
    {
        All = 0,
        Debug = 1,
        Information = 2,
        Warning = 3,
        Error = 4,
        Exception = 5,
        None = 6
    }

    public interface ILogger
    {
        LogLevel Level { get; }

        void Log(LogLevel level, string message, Exception? exception = null);
        bool IsEnabled(LogLevel level);

        void SetOutputName(string name);

        void Finish();
    }

    public sealed class LogEntry
    {
        public DateTime Timestamp { get; } = DateTime.UtcNow;
        public LogLevel Level { get; }
        public string Message { get; }
        public Exception? Exception { get; }

        public LogEntry(LogLevel level, string message, Exception? exception = null)
        {
            Level = level;
            Message = message;
            Exception = exception;
        }
    }

    public sealed class ConsoleLogger : ILogger
    {
        private readonly string _name;
        private string _output_name;
        private readonly LogLevel _min_level;
        public LogLevel Level { get { return _min_level; } }
        private readonly object _lock = new object();

        public ConsoleLogger(string name, LogLevel level)
        {
            _name = name;
            _min_level = level;

            _output_name = name;
        }

        public void Finish()
        {

        }

        public bool IsEnabled(LogLevel level) => level >= _min_level;

        public void SetOutputName(string name)
        {
            _output_name = name;
        }

        public void Log(LogLevel level, string message, Exception? exception = null)
        {
            if (!IsEnabled(level)) return;

            var entry = new LogEntry(level, message, exception);
            WriteLog(entry);
        }

        private void WriteLog(LogEntry entry)
        {
            lock (_lock)
            {
                var originalColor = Console.ForegroundColor;
                
                Console.ForegroundColor = GetConsoleColor(entry.Level);
                Console.WriteLine($"[{entry.Timestamp:HH:mm:ss.fff}] [{entry.Level}] {_output_name}: {entry.Message}");
                
                if (entry.Exception != null)
                {
                    Console.WriteLine($"Exception: {entry.Exception.GetType().Name}: {entry.Exception.Message}");
                    Console.WriteLine(entry.Exception.StackTrace);
                }
                
                Console.ForegroundColor = originalColor;
            }
        }

        private static ConsoleColor GetConsoleColor(LogLevel level)
        {
            return level switch
            {
                LogLevel.All => ConsoleColor.DarkGray,
                LogLevel.Debug => ConsoleColor.Gray,
                LogLevel.Information => ConsoleColor.White,
                LogLevel.Warning => ConsoleColor.Yellow,
                LogLevel.Error => ConsoleColor.Red,
                LogLevel.Exception => ConsoleColor.DarkRed,
                _ => ConsoleColor.White
            };
        }
    }

    public sealed class FileLogger : ILogger
    {
        private readonly string _name;
        private string _path_name;
        private string _output_name;
        private readonly LogLevel _min_level;
        public LogLevel Level { get { return _min_level; } }
        private readonly object _lock = new object();

        private bool _is_busy = false;
        private Queue<string> _queue = new Queue<string>();

        public string GetPathName() 
        {
            return _path_name;
        }

        public FileLogger(string name, LogLevel level)
        {
            _name = name;
            _min_level = level;


            _output_name = $"{name}.log";
            _path_name = System.IO.Path.GetFullPath("./logs");

            if(!System.IO.Path.Exists(_path_name))
            {
                System.IO.Directory.CreateDirectory(_path_name);
            }
        }

        public void Finish()
        {
            this._WriteLog(true);
        }

        public bool IsEnabled(LogLevel level) => level >= _min_level;

        public void SetOutputName(string name)
        {
            string fullname = System.IO.Path.GetFullPath(name);
            _output_name = System.IO.Path.GetFileName(fullname);
            _path_name = System.IO.Path.GetDirectoryName(fullname) ?? _path_name;
        }

        public void Log(LogLevel level, string message, Exception? exception = null)
        {
            if (!IsEnabled(level)) return;

            var entry = new LogEntry(level, message, exception);
            _Log(entry);
        }

        private void _Log(LogEntry entry)
        {
            string text = $"[{entry.Timestamp:HH:mm:ss.fff}] [{entry.Level}] {_name}: {entry.Message}" + System.Environment.NewLine;
            
            if (entry.Exception != null)
            {
                text += $"Exception: {entry.Exception.GetType().Name}: {entry.Exception.Message}" + System.Environment.NewLine;
                text += entry.Exception.StackTrace + System.Environment.NewLine;
            }

            lock (_lock)
            {
                this._queue.Enqueue(text);
            }

            this._WriteLog();
        }

        private async void _WriteLog(bool force = false)
        {
            if(!force && _is_busy)
            {
                return;
            }

            try
            {
                _is_busy = true;

                string filename = System.IO.Path.GetFileNameWithoutExtension(_output_name);
                filename = $"{filename}_{DateTime.Now:yyyy-MM-dd}.log";
                filename = System.IO.Path.Join(_path_name, filename);

                string content = "";
                lock (_lock)
                {                
                    content = string.Join("", _queue);
                    _queue.Clear();
                }

                if(content.Length > 0) {
                    await File.AppendAllTextAsync(filename, content);
                }

                _is_busy = false;

            } catch (Exception ex) {
                _is_busy = false;

                System.Console.WriteLine($"[Logger] (Exception) : {ex}");
            }
        }
    }

    public sealed class LoggerEntry
    {
        public ILogger? Console;
        public ILogger? File;

        public void SetOutputFileName(string filename = "")
        {
            if(File == null) { return; }

            File.SetOutputName(filename);
        }
    }

    public static class LoggerFactory
    {
        private static LoggerEntry? _instance = null;
        public static LoggerEntry? Instance => _instance;
         
        private static readonly Dictionary<string, LoggerEntry> _loggers = new Dictionary<string, LoggerEntry>();
        private static LogLevel _default_level = LogLevel.Information;

        public static LoggerEntry CreateLogger(string name, bool console = true, bool file = true)
        {
            LoggerEntry? logger = null;
            if (_loggers.TryGetValue(name, out logger))
            {
                return logger;
            }

            logger = new LoggerEntry();
            if(console) {
                var l = new ConsoleLogger(name, _default_level);
                logger.Console = l;
            }
            if(file) {
                var l = new FileLogger(name, _default_level);
                logger.File = l;
            }

            _loggers[name] = logger;

            if(_instance == null)
            {
                _instance = logger;
            }
            return logger;
        }

    }

    public static class LoggerExtensions
    {
        public static void LogTrace(this ILogger logger, string message) 
            => logger.Log(LogLevel.All, message);

        public static void LogDebug(this ILogger logger, string message) 
            => logger.Log(LogLevel.Debug, message);

        public static void Log(this ILogger logger, string message) 
            => logger.Log(LogLevel.Information, message);

        public static void LogWarning(this ILogger logger, string message) 
            => logger.Log(LogLevel.Warning, message);

        public static void LogError(this ILogger logger, string message, Exception? ex = null) 
            => logger.Log(LogLevel.Error, message, ex);

        public static void LogException(this ILogger logger, string message, Exception? ex = null) 
            => logger.Log(LogLevel.Exception, message, ex);


        /// <summary>
        /// 
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="message"></param>
        public static void Finish(this LoggerEntry logger) 
        {
            logger.Console?.Finish();
            logger.File?.Finish();
        }

        public static LogLevel GetLevel(this LoggerEntry logger) 
        {
            return logger.Console?.Level ?? logger.File?.Level ?? LogLevel.Information;
        }

        public static void LogTrace(this LoggerEntry logger, string message) 
        {
            logger.Console?.Log(LogLevel.All, message);
            logger.File?.Log(LogLevel.All, message);
        }
        public static void LogDebug(this LoggerEntry logger, string message) 
        {
            logger.Console?.Log(LogLevel.Debug, message);
            logger.File?.Log(LogLevel.Debug, message);
        }
        public static void Log(this LoggerEntry logger, string message) 
        {
            logger.Console?.Log(LogLevel.Information, message);
            logger.File?.Log(LogLevel.Information, message);
        }

        public static void LogWarning(this LoggerEntry logger, string message) 
        {
            logger.Console?.Log(LogLevel.Warning, message);
            logger.File?.Log(LogLevel.Warning, message);
        }

        public static void LogError(this LoggerEntry logger, string message, Exception? ex = null) 
        {
            logger.Console?.Log(LogLevel.Error, message);
            logger.File?.Log(LogLevel.Error, message);
        }

        public static void LogException(this LoggerEntry logger, string message, Exception? ex = null) 
        {
            logger.Console?.Log(LogLevel.Exception, message);
            logger.File?.Log(LogLevel.Exception, message);
        }
    }
}
