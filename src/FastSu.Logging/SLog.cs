using System.Runtime.CompilerServices;
using NLog;

// ReSharper disable ExplicitCallerInfoArgument

namespace FastSu;

public static class SLog
{
    private static readonly SLogger Logger = LogManager.GetLogger(string.Empty);

    public static void Debug(string msg,
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int line = 0) => Logger.Debug(msg, filePath, line);

    public static void Debug(Exception ex, string msg,
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int line = 0) => Logger.Debug(ex, msg, filePath, line);

    public static void Info(string msg,
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int line = 0) => Logger.Info(msg, filePath, line);

    public static void Warn(string msg,
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int line = 0) => Logger.Warn(msg, filePath, line);

    public static void Error(string msg,
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int line = 0) => Logger.Error(msg, filePath, line);

    public static void Error(Exception ex, string msg,
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int line = 0) => Logger.Error(ex, msg, filePath, line);
}