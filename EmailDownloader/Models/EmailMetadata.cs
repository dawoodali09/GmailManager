namespace EmailDownloader.Models;

/// <summary>
/// Represents metadata for a single email, persisted to CSV
/// </summary>
public class EmailMetadata
{
    public string MessageId { get; set; } = string.Empty;
    public string ThreadId { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Sender { get; set; } = string.Empty;
    public string? Recipients { get; set; }
    public DateTime ReceivedDate { get; set; }
    public bool HasAttachment { get; set; }
    public bool Important { get; set; }
    public bool Processed { get; set; }
    public string LocalPath { get; set; } = string.Empty;
    public DateTime LastModified { get; set; } = DateTime.UtcNow;
    public bool PushToSec { get; set; }
}

/// <summary>
/// Classification result for an email
/// </summary>
public class EmailClassification
{
    public bool IsImportant { get; set; }
    public string Reason { get; set; } = string.Empty;
    public bool ContainsUnsubscribe { get; set; }
    public bool HasAttachment { get; set; }
    public string? MatchingNaughtySender { get; set; }
    public IList<string> MatchReasons { get; set; } = new List<string>();
}

/// <summary>
/// Represents a naughty sender (spam/unwanted)
/// </summary>
public class NaughtySender
{
    public string Sender { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public string? Notes { get; set; }
}

/// <summary>
/// Result of processing an email
/// </summary>
public class EmailProcessingResult
{
    public string MessageId { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? Error { get; set; }
    public bool WasDownloaded { get; set; }
    public bool WasDeleted { get; set; }
    public string? DeletionReason { get; set; }
}
