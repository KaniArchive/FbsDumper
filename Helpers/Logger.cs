using FbsDumper.CLI;
using Kokuban;
using Microsoft.Extensions.Logging;
using ZLogger;

namespace FbsDumper.Helpers;

public static class Log
{
    private static ILoggerFactory? _loggerFactory;
    private static ILogger? _logger;
    private static bool _isInitialized;

    public static ILogger? Global
    {
        get
        {
            EnsureInitialized();
            return _logger;
        }
    }

    public static void Info(string message)
    {
        EnsureInitialized();
        _logger?.ZLogInformation($"{message}");
    }

    public static void Error(string message)
    {
        EnsureInitialized();
        _logger?.ZLogError($"{message}");
    }

    public static void Error(string message, Exception exception)
    {
        EnsureInitialized();
        _logger?.ZLogError(exception, $"{message}");
    }

    public static void Warning(string message)
    {
        if (Parser.SuppressWarnings) return;
        EnsureInitialized();
        _logger?.ZLogWarning($"{message}");
    }

    public static void Debug(string message)
    {
        EnsureInitialized();
        _logger?.ZLogDebug($"{message}");
    }

    public static void EnableDebugLogging()
    {
        if (_isInitialized) Shutdown();
        Initialize(LogLevel.Debug);
    }

    public static void Shutdown()
    {
        if (!_isInitialized) return;
        _loggerFactory?.Dispose();
        _loggerFactory = null;
        _logger = null;
        _isInitialized = false;
    }

    private static void EnsureInitialized()
    {
        if (_isInitialized) return;
        Initialize(LogLevel.Information);
    }

    private static void Initialize(LogLevel minimumLevel)
    {
        _loggerFactory = LoggerFactory.Create(logging =>
        {
            logging.ClearProviders();
            logging.SetMinimumLevel(minimumLevel);

            logging.AddZLoggerConsole(options =>
            {
                options.UsePlainTextFormatter(formatter =>
                {
                    formatter.SetPrefixFormatter($"{0} {1} ",
                        (in template, in info) =>
                        {
                            var timestamp = Chalk.Gray + info.Timestamp.Local.ToString("HH:mm:ss");
                            var logLevel = GetColoredLogLevel(info.LogLevel);
                            template.Format(timestamp, logLevel);
                        });
                });
                options.LogToStandardErrorThreshold = LogLevel.Error;
            });
        });

        _logger = _loggerFactory.CreateLogger("FbsDumper");
        _isInitialized = true;
    }

    private static string GetColoredLogLevel(LogLevel logLevel)
    {
        return logLevel switch
        {
            LogLevel.Trace => Chalk.Magenta + "[TRC]",
            LogLevel.Debug => Chalk.Cyan + "[DBG]",
            LogLevel.Information => Chalk.Blue + "[INF]",
            LogLevel.Warning => Chalk.Yellow + "[WRN]",
            LogLevel.Error => Chalk.Red + "[ERR]",
            LogLevel.Critical => Chalk.BgRed.White + "[CRT]",
            _ => Chalk.White + "[???]"
        };
    }
}

public static partial class LogMessages
{
    [ZLoggerMessage(LogLevel.Information, "Disassembling types ({current}/{total})...")]
    public static partial void LogProgress(this ILogger? logger, int current, int total);

    [ZLoggerMessage(LogLevel.Error, "Dummy assembly directory '{path}' not found.")]
    public static partial void LogDummyDirNotFound(this ILogger? logger, string path);

    [ZLoggerMessage(LogLevel.Error, "libil2cpp.so path '{path}' not found.")]
    public static partial void LogGameAssemblyNotFound(this ILogger? logger, string path);

    [ZLoggerMessage(LogLevel.Error, "{fileName} not found in '{directory}'.")]
    public static partial void LogFileNotFound(this ILogger? logger, string fileName, string directory);

    [ZLoggerMessage(LogLevel.Warning, "unknown system type {typeName}")]
    private static partial void LogUnknownSystemTypeInternal(this ILogger logger, string typeName);

    public static void LogUnknownSystemType(this ILogger? logger, string typeName)
    {
        if (!Parser.SuppressWarnings) logger.LogUnknownSystemTypeInternal(typeName);
    }

    [ZLoggerMessage(LogLevel.Debug, "\t0x{address:X}: {mnemonic} {operand}")]
    public static partial void LogInstruction(this ILogger logger, ulong address, string mnemonic, string? operand);

    [ZLoggerMessage(LogLevel.Warning, "Skipping call for 0x{address:X} because {reason}")]
    private static partial void LogSkippingCallInternal(this ILogger logger, ulong address, string reason);

    public static void LogSkippingCall(this ILogger logger, ulong address, string reason)
    {
        if (!Parser.SuppressWarnings) logger.LogSkippingCallInternal(address, reason);
    }
}