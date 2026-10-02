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

    public ConsoleLogger(string? levelName = "Info")
    {
        _minLevel = Enum.TryParse<LogLevel>(levelName, ignoreCase: true, out var level) 
            ? level 
            : LogLevel.Info;
    }

    public void LogDebug(string message)
    {
        if (_minLevel <= LogLevel.Debug)
            Console.WriteLine($"[DEBUG] {message}");
    }

    public void LogInfo(string message)
    {
        if (_minLevel <= LogLevel.Info)
            Console.WriteLine($"[INFO]  {message}");
    }

    public void LogWarning(string message)
    {
        if (_minLevel <= LogLevel.Warning)
            Console.ForegroundColor = ConsoleColor.Yellow;
        if (_minLevel <= LogLevel.Warning)
            Console.WriteLine($"[WARN]  {message}");
        Console.ResetColor();
    }

    public void LogError(string message)
    {
        if (_minLevel <= LogLevel.Error)
            Console.ForegroundColor = ConsoleColor.Red;
        if (_minLevel <= LogLevel.Error)
            Console.WriteLine($"[ERROR] {message}");
        Console.ResetColor();
    }

    /// <summary>
    /// Log successful operation in green
    /// </summary>
    public void LogSuccess(string message)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"[OK]    {message}");
        Console.ResetColor();
    }
}
