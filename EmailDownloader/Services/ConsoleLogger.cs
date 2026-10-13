namespace EmailDownloader.Services;

/// <summary>
/// Simple console logger that doesn't log sensitive data (credentials, tokens, secrets)
/// </summary>
public class ConsoleLogger
{
    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error
    }

    private readonly LogLevel _minLevel;
    private readonly Action<string>? _messageSink;
    private readonly bool _writeToConsole;

    public ConsoleLogger(string? levelName = "Info", Action<string>? messageSink = null, bool writeToConsole = true)
    {
        _minLevel = Enum.TryParse<LogLevel>(levelName, ignoreCase: true, out var level) 
            ? level 
            : LogLevel.Info;
        _messageSink = messageSink;
        _writeToConsole = writeToConsole;
    }

    public void LogDebug(string message)
    {
        if (_minLevel <= LogLevel.Debug)
            Write($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}][DEBUG] {message}");
    }

    public void LogInfo(string message)
    {
        if (_minLevel <= LogLevel.Info)
            Write($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}][INFO]  {message}");
    }

    public void LogWarning(string message)
    {
        if (_minLevel <= LogLevel.Warning)
            Write($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}][WARN]  {message}", ConsoleColor.Yellow);
    }

    public void LogError(string message)
    {
        if (_minLevel <= LogLevel.Error)
            Write($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}][ERROR] {message}", ConsoleColor.Red);
    }

    /// <summary>
    /// Log successful operation in green
    /// </summary>
    public void LogSuccess(string message)
    {
        Write($"[OK]    {message}", ConsoleColor.Green);
    }

    private void Write(string line, ConsoleColor? color = null)
    {
        if (_writeToConsole)
        {
            if (color.HasValue)
            {
                Console.ForegroundColor = color.Value;
            }

            Console.WriteLine(line);
            Console.ResetColor();
        }

        _messageSink?.Invoke(line);
    }
}
