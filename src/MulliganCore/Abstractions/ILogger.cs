using System;

namespace HstMulligan.Core.Abstractions
{
    public enum LogLevel
    {
        Trace = 0,
        Debug = 1,
        Info  = 2,
        Warn  = 3,
        Error = 4,
    }

    public interface ILogger
    {
        bool IsEnabled(LogLevel level);
        void Log(LogLevel level, string message, Exception exception = null);
    }

    public static class LoggerExtensions
    {
        public static void Trace(this ILogger log, string msg) => log?.Log(LogLevel.Trace, msg);
        public static void Debug(this ILogger log, string msg) => log?.Log(LogLevel.Debug, msg);
        public static void Info (this ILogger log, string msg) => log?.Log(LogLevel.Info,  msg);
        public static void Warn (this ILogger log, string msg, Exception ex = null) => log?.Log(LogLevel.Warn,  msg, ex);
        public static void Error(this ILogger log, string msg, Exception ex = null) => log?.Log(LogLevel.Error, msg, ex);
    }

    public sealed class NullLogger : ILogger
    {
        public static readonly NullLogger Instance = new NullLogger();
        public bool IsEnabled(LogLevel level) => false;
        public void Log(LogLevel level, string message, Exception exception = null) { }
    }
}
