using EmailDownloader.Configuration;
using EmailDownloader.Services;
using System.Text.Json;

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
            // Load configuration from appsettings.json
            var settings = LoadSettings();
            var logger = new ConsoleLogger(settings.ApplicationConfiguration.LoggingLevel);

            logger.LogInfo("╔════════════════════════════════════════════════════════╗");
            logger.LogInfo("║    Email Manager - Gmail Download & Classification    ║");
            logger.LogInfo("╚════════════════════════════════════════════════════════╝");

            // Validate configuration
            if (!File.Exists(settings.GmailConfiguration.CredentialsPath))
            {
                logger.LogError($"Credentials file not found: {settings.GmailConfiguration.CredentialsPath}");
                logger.LogError("Please download credentials.json from Google Cloud Console and place it in the project directory.");
                return;
            }

            // Create storage directory if needed
            Directory.CreateDirectory(settings.ApplicationConfiguration.EmailStoragePath);

            // Initialize services
            var gmailService = new GmailServiceWrapper(settings, logger);
            var emailStorage = new EmailStorage(
                settings.ApplicationConfiguration.EmailStoragePath,
                settings.EmailProcessing.MaxAttachmentSizeMB,
                logger);

            var metadataRepo = new MetadataRepository(
                settings.ApplicationConfiguration.EmailStoragePath,
                settings.ApplicationConfiguration.MetadataFileName,
                logger);

            var naughtySendersRepo = new NaughtySenderRepository(
                settings.ApplicationConfiguration.EmailStoragePath,
                settings.ApplicationConfiguration.NaughtySendersFileName,
                logger);

            await naughtySendersRepo.LoadAsync();

            var classifier = new EmailClassifier(settings, logger, naughtySendersRepo);

            var processor = new EmailProcessor(
                settings,
                logger,
                gmailService,
                emailStorage,
                metadataRepo,
                naughtySendersRepo,
                classifier);

            // Run the processor
            await processor.ProcessAsync(cts.Token);

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

    /// <summary>
    /// Load application settings from appsettings.json
    /// </summary>
    static AppSettings LoadSettings()
    {
        const string settingsFile = "appsettings.json";

        if (!File.Exists(settingsFile))
        {
            throw new FileNotFoundException(
                $"Configuration file '{settingsFile}' not found. " +
                $"Please copy 'appsettings.example.json' to '{settingsFile}' and update the values.");
        }

        try
        {
            var json = File.ReadAllText(settingsFile);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            });

            if (settings == null)
            {
                throw new InvalidOperationException("Failed to deserialize appsettings.json");
            }

            return settings;
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                $"Invalid JSON in '{settingsFile}': {ex.Message}", ex);
        }
    }
}
