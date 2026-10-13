using EmailDownloader.Configuration;
using EmailDownloader.Models;

namespace EmailDownloader.Services;

public class EmailProcessingRunner
{
    public async Task<ProcessingRunResult> RunOnceAsync(AppSettings settings, ConsoleLogger logger, CancellationToken cancellationToken = default)
    {
        try
        {
            var credentialsPath = ResolveCredentialsPath(settings.GmailConfiguration.CredentialsPath);
            if (!File.Exists(credentialsPath))
            {
                return new ProcessingRunResult
                {
                    Success = false,
                    Message = $"Credentials file not found: {settings.GmailConfiguration.CredentialsPath}"
                };
            }

            settings.GmailConfiguration.CredentialsPath = credentialsPath;
            Directory.CreateDirectory(settings.ApplicationConfiguration.EmailStoragePath);

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

            await processor.ProcessAsync(cancellationToken);

            return new ProcessingRunResult
            {
                Success = true,
                Message = "Processing completed successfully"
            };
        }
        catch (GmailQuotaExceededException ex)
        {
            logger.LogWarning($"Quota reached: {ex.Message}");
            return new ProcessingRunResult
            {
                Success = false,
                QuotaExceeded = true,
                Message = ex.Message
            };
        }
        catch (OperationCanceledException)
        {
            return new ProcessingRunResult
            {
                Success = false,
                WasCancelled = true,
                Message = "Processing cancelled"
            };
        }
        catch (Exception ex)
        {
            return new ProcessingRunResult
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

    private static string ResolveCredentialsPath(string configuredPath)
    {
        var credentialsPath = configuredPath;
        if (!Path.IsPathRooted(credentialsPath) || !File.Exists(credentialsPath))
        {
            var probeCreds = new[]
            {
                Path.Combine(Environment.CurrentDirectory, credentialsPath),
                Path.Combine(Environment.CurrentDirectory, "EmailDownloader", credentialsPath),
                Path.Combine(AppContext.BaseDirectory, credentialsPath),
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", credentialsPath),
                Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? AppContext.BaseDirectory, credentialsPath)
            };

            var foundCred = probeCreds.FirstOrDefault(File.Exists);
            if (foundCred != null)
            {
                credentialsPath = foundCred;
            }
        }

        return credentialsPath;
    }
}
