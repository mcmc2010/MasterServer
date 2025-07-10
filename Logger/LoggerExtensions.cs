
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using Microsoft.Extensions.Logging;

namespace Logger.Extensions {


    /// <summary>
    /// 
    /// </summary>
    public class ServiceLogger : Microsoft.Extensions.Logging.ILogger
    {
        private readonly string _name;
        private readonly string _pathname;
        private readonly string _filename;

        // 存储当前作用域状态的栈
        private readonly Stack<object?> _scope = new Stack<object?>();
        // 嵌套类：管理作用域生命周期
        private class LoggerScope<T> : IDisposable
        {
            private readonly ServiceLogger _logger;
            private readonly T _state;

            public LoggerScope(T state, ServiceLogger logger)
            {
                _state = state;
                _logger = logger;
                // 将作用域状态添加到当前上下文中
                _logger._scope.Push(state);
            }

            public void Dispose()
            {
                if (_logger._scope.Count > 0)
                {
                    // 作用域结束时，移除状态
                    _logger._scope.Pop();
                }
            }
        }

        public ServiceLogger(string name, string pathname, string filename)
        {
            _name = name;
            _pathname = pathname;
            _filename = filename;

            if (_pathname.Length > 0 && !Path.Exists(_pathname))
            {
                Directory.CreateDirectory(_pathname);
            }
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {

            return new LoggerScope<TState>(state, this);
        }

        public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => true;
        

        private object? GetValueFromState(string key, object? def)
        {
            // 假设 _scope 是 Stack<object>，且按后进先出顺序存储
            foreach (var entry in this._scope)
            {
                if (entry is IReadOnlyList<KeyValuePair<string, object>> pairs)
                {
                    var pair = pairs.FirstOrDefault(kvp => kvp.Key == key);
                    if (pair.Key != null)
                    {
                        return pair.Value;
                    }
                }
                else if (entry is IDictionary<string, object> dict)
                {
                    var pair = dict.FirstOrDefault(kvp => kvp.Key == key);
                    if (pair.Key != null)
                    {
                        return pair.Value;
                    }
                }
            }

            // 最终未找到则返回默认值
            return def;
        }

        private string LogFormatter(string text)
        {
            if (text.Contains("{RemoteIP}"))
            {
                var ip = GetValueFromState("RemoteIP", "");
                if (ip is IPAddress ipa)
                {
                    text = text.Replace("{RemoteIP}", ipa.ToString());
                }
                else
                {
                    text = text.Replace("{RemoteIP}", (string)(ip ?? ""));
                }
            }

            string[] args = new string[] {
                "Scheme", "Methed", "Path", "StatusCode"
            };
            foreach (var v in args)
            {
                if (text.Contains($"{{{v}}}"))
                {
                    var value = GetValueFromState(v, null);
                    if (value == null) { continue; }

                    if (value is string t)
                    {
                        text = text.Replace($"{{{v}}}", t);
                    }
                    else if (value is int i)
                    {
                        text = text.Replace($"{{{v}}}", $"{i}");
                    }
                    else if (value is float f)
                    {
                        text = text.Replace($"{{{v}}}", $"{f:F3}");
                    }
                }
            }
            return text;
        }

        public void Log<TState>(
            Microsoft.Extensions.Logging.LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            var message = "";
            message = formatter(state, exception);
            message = this.LogFormatter(message);

            string content = $"{DateTime.Now:HH:mm:ss} [{logLevel}] {message}{Environment.NewLine}";
            // 输出控制台
            System.Console.Write(content);

            // 不写入文件
            if (_name == "Microsoft.AspNetCore.Routing.EndpointMiddleware")
            {
                return;
            }

            // 输出文件
            string filename = Path.GetFileNameWithoutExtension(_filename);
            filename = $"{filename}_{DateTime.Now:yyyy-MM-dd}{Path.GetExtension(_filename)}";
            filename = Path.Join(_pathname, filename);
            
            File.AppendAllText(filename, content);
        }
    }

    public class LoggerProvider : Microsoft.Extensions.Logging.ILoggerProvider
    {
        private readonly string _pathname;
        private readonly string _filename;

        public LoggerProvider(string filename)
        {
            string fullname = Path.GetFullPath(filename);
            _pathname = Path.GetDirectoryName(fullname)??"";
            _filename = Path.GetFileName(fullname);
        }

        public Microsoft.Extensions.Logging.ILogger CreateLogger(string name) 
        {
            return new ServiceLogger(name, _pathname, _filename);
        }

        public void Dispose() { }
    }

}