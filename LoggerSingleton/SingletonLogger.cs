using NLog;
using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace LoggerSingleton
{
    public class SingletonLogger
    {
        private static readonly ConcurrentDictionary<String, Logger> FilePathLoggers = new ConcurrentDictionary<String, Logger>();

        private static readonly SingletonLogger instance = new SingletonLogger();

        public static SingletonLogger Instance { get { return instance; } }

        public Logger Logger( string filePath )
        { 
            if ( !FilePathLoggers.ContainsKey(filePath) )
            {
                FilePathLoggers.GetOrAdd(filePath, LogManager.GetLogger(filePath));
            }

            return FilePathLoggers[filePath]; 
        }

        public void Fatal(object message, [CallerFilePath] string filePath = "") { Logger(filePath).Fatal(message); }
        public void Error(object message, [CallerFilePath] string filePath = "") { Logger(filePath).Error(message); }
        public void Warn(object message, [CallerFilePath] string filePath = "")  { Logger(filePath).Warn(message); }
        public void Info(object message, [CallerFilePath] string filePath = "")  { Logger(filePath).Info(message); }
        public void Debug(object message, [CallerFilePath] string filePath = "") { Logger(filePath).Debug(message); }
        public void Trace(object message, [CallerFilePath] string filePath = "") { Logger(filePath).Trace(message); }

        public bool IsFatalEnabled([CallerFilePath] string filePath = "") { return Logger(filePath).IsFatalEnabled; }
        public bool IsErrorEnabled([CallerFilePath] string filePath = "") { return Logger(filePath).IsErrorEnabled; }
        public bool IsWarnEnabled([CallerFilePath] string filePath = "")  { return Logger(filePath).IsWarnEnabled; }
        public bool IsInfoEnabled([CallerFilePath] string filePath = "")  { return Logger(filePath).IsInfoEnabled; }
        public bool IsDebugEnabled([CallerFilePath] string filePath = "") { return Logger(filePath).IsDebugEnabled; }
        public bool IsTraceEnabled([CallerFilePath] string filePath = "") { return Logger(filePath).IsTraceEnabled; }
    }
}
