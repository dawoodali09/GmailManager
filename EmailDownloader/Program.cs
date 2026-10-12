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

            // Validate configuration - resolve credentials path from several common locations
            var credentialsPath = settings.GmailConfiguration.CredentialsPath;
            if (!Path.IsPathRooted(credentialsPath) || !File.Exists(credentialsPath))
            {
                var probeCreds = new[]
                {
                    Path.Combine(Environment.CurrentDirectory, credentialsPath),
                    Path.Combine(Environment.CurrentDirectory, "EmailDownloader", credentialsPath),
                    Path.Combine(AppContext.BaseDirectory, credentialsPath),
                    Path.Combine(AppContext.BaseDirectory, "..", "..", "..", credentialsPath), // project dir when running from bin/*/net*/
                    Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? AppContext.BaseDirectory, credentialsPath)
                };

                var foundCred = probeCreds.FirstOrDefault(File.Exists);
                if (foundCred != null)
                {
                    credentialsPath = foundCred;
                }
            }

            if (!File.Exists(credentialsPath))
            {
                logger.LogError($"Credentials file not found: {settings.GmailConfiguration.CredentialsPath}");
                logger.LogError($"Searched: {Path.GetFullPath(settings.GmailConfiguration.CredentialsPath)}; {Path.Combine(AppContext.BaseDirectory, settings.GmailConfiguration.CredentialsPath)}; {Path.Combine(Environment.CurrentDirectory, settings.GmailConfiguration.CredentialsPath)}");
                logger.LogError("Please download credentials.json from Google Cloud Console and place it in the project directory or update the path in appsettings.json.");
                return;
            }

            // Create storage directory if needed
            Directory.CreateDirectory(settings.ApplicationConfiguration.EmailStoragePath);

            // Initialize services
            var gmailService = new GmailServiceWrapper(settings, logger);
            var emailStorage = new EmailStorage(
                settings.ApplicationConfiguration.EmailStoragePath,
                settings.EmailProcessing.MaxAttachmentSizeMB,
                logger,
                settings.EmailProcessing);

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
        const string settingsFileName = "appsettings.json";

        // Try a few common locations: current working directory and the application base directory
        var probePaths = new[]
        {
            Path.Combine(Environment.CurrentDirectory, settingsFileName),
            Path.Combine(AppContext.BaseDirectory, settingsFileName)
        };

        var foundPath = probePaths.FirstOrDefault(File.Exists);
        if (foundPath == null)
        {
            throw new FileNotFoundException(
                $"Configuration file '{settingsFileName}' not found. " +
                $"Please copy 'appsettings.example.json' to '{settingsFileName}' and update the values. " +
                $"(Searched: {string.Join(", ", probePaths)})");
        }

        try
        {
            var json = File.ReadAllText(foundPath);
            try
            {
                var settings = JsonSerializer.Deserialize<AppSettings>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (settings == null)
                {
                    throw new InvalidOperationException($"Failed to deserialize {foundPath}");
                }

                return settings;
            }
            catch (JsonException)
            {
                // Attempt a simple sanitization for common accidental duplication at start of file
                // e.g. files that start with "{{" instead of "{" (caused by earlier incorrect copy)
                var trimmed = json.TrimStart();
                while (trimmed.StartsWith("{{"))
                {
                    // remove one of the duplicate opening braces
                    trimmed = trimmed.Substring(1);
                }

                // Try deserialize again with sanitized content
                var settings = JsonSerializer.Deserialize<AppSettings>(trimmed, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (settings == null)
                {
                    throw new InvalidOperationException($"Failed to deserialize {foundPath} after sanitization");
                }

                return settings;
            }
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                $"Invalid JSON in '{foundPath}': {ex.Message}", ex);
        }
    }
}
