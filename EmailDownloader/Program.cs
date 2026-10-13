using EmailDownloader.Services;

namespace EmailDownloader;

class Program
{
    static async Task Main(string[] args)
    {
        var cts = new CancellationTokenSource();

        // Handle Ctrl+C gracefully
        Console.CancelKeyPress += (sender, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        try
        {
            var settings = AppSettingsLoader.LoadSettings();
            var logger = new ConsoleLogger(settings.ApplicationConfiguration.LoggingLevel);
            var runner = new EmailProcessingRunner();

            logger.LogInfo("╔════════════════════════════════════════════════════════╗");
            logger.LogInfo("║    Email Manager - Gmail Download & Classification    ║");
            logger.LogInfo("╚════════════════════════════════════════════════════════╝");

            var result = await runner.RunOnceAsync(settings, logger, cts.Token);
            if (!result.Success)
            {
                logger.LogError(result.Message);
                return;
            }

            logger.LogSuccess("✓ Processing completed successfully!");
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("\n[INFO]  Processing cancelled by user.");
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[ERROR] Fatal error: {ex.Message}");
            Console.WriteLine($"[ERROR] {ex.StackTrace}");
            Console.ResetColor();
            Environment.Exit(1);
        }
    }

}
