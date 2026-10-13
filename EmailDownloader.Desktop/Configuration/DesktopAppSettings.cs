using EmailDownloader.Configuration;

namespace EmailDownloader.Desktop.Configuration;

public class DesktopAppSettings
{
    public string CredentialsPath { get; set; } = "credentials.json";
    public string TokenPath { get; set; } = "token.json";
    public string EmailStoragePath { get; set; } = @"C:\Shared Storage\myEmails";
    public string MetadataFileName { get; set; } = "emails.csv";
    public string NaughtySendersFileName { get; set; } = "naughty_senders.csv";
    public bool DryRunMode { get; set; } = true;
    public string LoggingLevel { get; set; } = "Info";

    public int BatchSize { get; set; } = 50;
    public int MaxAttachmentSizeMB { get; set; } = 50;
    public int RetryAttempts { get; set; } = 3;
    public int RetryDelayMs { get; set; } = 1000;
    public int MaxResultsPerPage { get; set; } = 100;
    public int RequestDelayMs { get; set; } = 200;
    public int MaxRequestsPerMinute { get; set; } = 300;

    public int RepeatIntervalMinutes { get; set; } = 60;
    public bool AutoStartScheduler { get; set; } = false;

    public List<string> ImportantDomains { get; set; } = new() { "dawood", "wajeeha" };
    public List<string> UnsubscribeKeywords { get; set; } = new() { "unsubscribe", "un-subscribe" };

    public AppSettings ToCoreSettings()
    {
        return new AppSettings
        {
            GmailConfiguration = new GmailConfiguration
            {
                CredentialsPath = CredentialsPath,
                TokenPath = TokenPath
            },
            ApplicationConfiguration = new EmailDownloader.Configuration.ApplicationConfiguration
            {
                EmailStoragePath = EmailStoragePath,
                MetadataFileName = MetadataFileName,
                NaughtySendersFileName = NaughtySendersFileName,
                DryRunMode = DryRunMode,
                LoggingLevel = LoggingLevel
            },
            EmailProcessing = new EmailProcessing
            {
                BatchSize = BatchSize,
                MaxAttachmentSizeMB = MaxAttachmentSizeMB,
                RetryAttempts = RetryAttempts,
                RetryDelayMs = RetryDelayMs,
                MaxResultsPerPage = MaxResultsPerPage,
                RequestDelayMs = RequestDelayMs,
                MaxRequestsPerMinute = MaxRequestsPerMinute
            },
            ClassificationRules = new ClassificationRules
            {
                ImportantDomains = ImportantDomains ?? new List<string>(),
                UnsubscribeKeywords = UnsubscribeKeywords ?? new List<string>()
            }
        };
    }

    public static DesktopAppSettings FromCoreSettings(AppSettings settings)
    {
        return new DesktopAppSettings
        {
            CredentialsPath = settings.GmailConfiguration.CredentialsPath,
            TokenPath = settings.GmailConfiguration.TokenPath,
            EmailStoragePath = settings.ApplicationConfiguration.EmailStoragePath,
            MetadataFileName = settings.ApplicationConfiguration.MetadataFileName,
            NaughtySendersFileName = settings.ApplicationConfiguration.NaughtySendersFileName,
            DryRunMode = settings.ApplicationConfiguration.DryRunMode,
            LoggingLevel = settings.ApplicationConfiguration.LoggingLevel,
            BatchSize = settings.EmailProcessing.BatchSize,
            MaxAttachmentSizeMB = settings.EmailProcessing.MaxAttachmentSizeMB,
            RetryAttempts = settings.EmailProcessing.RetryAttempts,
            RetryDelayMs = settings.EmailProcessing.RetryDelayMs,
            MaxResultsPerPage = settings.EmailProcessing.MaxResultsPerPage,
            RequestDelayMs = settings.EmailProcessing.RequestDelayMs,
            MaxRequestsPerMinute = settings.EmailProcessing.MaxRequestsPerMinute,
            ImportantDomains = settings.ClassificationRules.ImportantDomains?.ToList() ?? new List<string>(),
            UnsubscribeKeywords = settings.ClassificationRules.UnsubscribeKeywords?.ToList() ?? new List<string>()
        };
    }
}
