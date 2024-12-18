using System.Runtime.CompilerServices;
using NLog;

// ReSharper disable ExplicitCallerInfoArgument

namespace FastSu;

public readonly struct SLogger
{
    private readonly Logger _logger;

    public static implicit operator SLogger(Logger logger)
    {
        SLogger l = new SLogger(logger);
        return l;
    }

    public SLogger(string name)
    {
        _logger = LogManager.GetLogger(name);
    }

    public SLogger(Logger logger)
    {
        _logger = logger;
    }

    public void Debug(string msg,
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int line = 0)
    {
        _logger.ForDebugEvent()
            .Message(msg)
            .Log(callerFilePath: filePath, callerLineNumber: line);
    }

    public void Debug(Exception ex, string msg,
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int line = 0)
    {
        _logger.ForDebugEvent()
            .Exception(ex)
            .Message(msg)
            .Log(callerFilePath: filePath, callerLineNumber: line);
    }

    public void Info(string msg,
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int line = 0)
    {
        _logger.ForInfoEvent()
            .Message(msg)
            .Log(callerFilePath: filePath, callerLineNumber: line);
    }

    public void Warn(string msg,
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int line = 0)
    {
        _logger.ForWarnEvent()
            .Message(msg)
            .Log(callerFilePath: filePath, callerLineNumber: line);
    }

    public void Error(string msg,
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int line = 0)
    {
        _logger.ForErrorEvent()
            .Message(msg)
            .Log(callerFilePath: filePath, callerLineNumber: line);
    }

    public void Error(Exception ex, string msg,
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int line = 0)
    {
        _logger.ForErrorEvent()
            .Exception(ex)
            .Message(msg)
            .Log(callerFilePath: filePath, callerLineNumber: line);
    }
}