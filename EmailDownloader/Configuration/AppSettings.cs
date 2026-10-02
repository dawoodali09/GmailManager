namespace EmailDownloader.Configuration;

/// <summary>
/// Application configuration read from appsettings.json
/// </summary>
public class AppSettings
{
    public GmailConfiguration GmailConfiguration { get; set; } = new();
    public ApplicationConfiguration ApplicationConfiguration { get; set; } = new();
    public EmailProcessing EmailProcessing { get; set; } = new();
    public ClassificationRules ClassificationRules { get; set; } = new();
}

public class GmailConfiguration
{
    public string CredentialsPath { get; set; } = "credentials.json";
    public string TokenPath { get; set; } = "token.json";
}

public class ApplicationConfiguration
{
    public string EmailStoragePath { get; set; } = @"C:\Shared Storage\myEmails";
    public string MetadataFileName { get; set; } = "emails.csv";
    public string NaughtySendersFileName { get; set; } = "naughty_senders.csv";
    public bool DryRunMode { get; set; } = true;
    public string LoggingLevel { get; set; } = "Info";
}

public class EmailProcessing
{
    public int BatchSize { get; set; } = 50;
    public int MaxAttachmentSizeMB { get; set; } = 50;
    public int RetryAttempts { get; set; } = 3;
    public int RetryDelayMs { get; set; } = 1000;
}

public class ClassificationRules
{
    public List<string> ImportantDomains { get; set; } = new();
    public List<string> UnsubscribeKeywords { get; set; } = new();
}
