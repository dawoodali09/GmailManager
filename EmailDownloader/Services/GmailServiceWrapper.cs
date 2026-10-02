using EmailDownloader.Configuration;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;

namespace EmailDownloader.Services;

/// <summary>
/// Wraps Google Gmail API service with authentication and message operations.
/// Handles OAuth flow, credentials, and message retrieval/deletion.
/// </summary>
public class GmailServiceWrapper
{
    private readonly AppSettings _settings;
    private readonly ConsoleLogger _logger;
    private GmailService? _gmailService;

    // Gmail API scopes
    private static readonly string[] Scopes =
    {
        GmailService.Scope.GmailModify // Required for both read and delete operations
    };

    private const string ApplicationName = "Email Manager";

    public GmailServiceWrapper(AppSettings settings, ConsoleLogger logger)
    {
        _settings = settings;
        _logger = logger;
    }

    /// <summary>
    /// Authenticate with Gmail API using OAuth
    /// </summary>
    public async Task AuthenticateAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            UserCredential credential;

            var credentialsPath = _settings.GmailConfiguration.CredentialsPath;
            var tokenPath = _settings.GmailConfiguration.TokenPath;

            if (!File.Exists(credentialsPath))
            {
                throw new FileNotFoundException(
                    $"Credentials file not found: {credentialsPath}\n" +
                    "Please download credentials.json from Google Cloud Console.");
            }

            // Load OAuth credentials from file
            using (var stream = new FileStream(credentialsPath, FileMode.Open, FileAccess.Read))
            {
                credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.FromStream(stream).Secrets,
                    Scopes,
                    "user",
                    cancellationToken,
                    new FileDataStore(Path.GetDirectoryName(tokenPath) ?? ".", true));
            }

            // Create Gmail service
            _gmailService = new GmailService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });

            _logger.LogInfo("✓ Gmail authentication successful");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Authentication failed: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Get all message IDs from the mailbox (with paging support)
    /// </summary>
    public async Task<List<Message>> FetchAllMessagesAsync(CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();

        var allMessages = new List<Message>();
        string? pageToken = null;
        int pageCount = 0;

        try
        {
            do
            {
                var request = _gmailService!.Users.Messages.List("me");
                request.MaxResults = 500; // Max batch size for Gmail API
                request.PageToken = pageToken;
                request.Fields = "messages(id,threadId),nextPageToken"; // Only fetch lightweight message stubs

                var response = await request.ExecuteAsync(cancellationToken);

                if (response.Messages != null)
                {
                    allMessages.AddRange(response.Messages);
                    pageCount++;
                }

                _logger.LogDebug($"Fetched page {pageCount}: {allMessages.Count} messages total so far...");
                pageToken = response.NextPageToken;

            } while (pageToken != null);

            _logger.LogInfo($"Total messages in mailbox: {allMessages.Count} (across {pageCount} pages)");
            return allMessages;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to fetch messages: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Get full details of a specific message
    /// </summary>
    public async Task<Message> GetMessageAsync(string messageId, CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();

        try
        {
            var request = _gmailService!.Users.Messages.Get("me", messageId);
            request.Format = UsersResource.MessagesResource.GetRequest.FormatEnum.Full;
            return await request.ExecuteAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to get message {messageId}: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Delete a message from Gmail (moves to Trash)
    /// </summary>
    public async Task DeleteMessageAsync(string messageId, CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();

        try
        {
            await _gmailService!.Users.Messages.Trash("me", messageId).ExecuteAsync(cancellationToken);
            _logger.LogDebug($"Message {messageId} moved to Trash");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to delete message {messageId}: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Extract sender email and name from message headers
    /// </summary>
    public static (string email, string name) ExtractSenderInfo(Message message)
    {
        var fromHeader = message.Payload?.Headers?.FirstOrDefault(
            h => h.Name.Equals("From", StringComparison.OrdinalIgnoreCase));

        if (fromHeader == null || string.IsNullOrWhiteSpace(fromHeader.Value))
            return (string.Empty, string.Empty);

        var fromValue = fromHeader.Value;
        var email = string.Empty;
        var name = string.Empty;

        // Try to extract email from <email@example.com>
        var emailMatch = System.Text.RegularExpressions.Regex.Match(fromValue, @"<(.+?)>");
        if (emailMatch.Success)
        {
            email = emailMatch.Groups[1].Value.ToLowerInvariant();
            name = fromValue.Replace($"<{email}>", "").Trim().Trim('"');
        }
        else if (fromValue.Contains("@"))
        {
            // Might be a plain email address
            email = fromValue.Trim().ToLowerInvariant();
            name = email;
        }

        return (email, name);
    }

    /// <summary>
    /// Extract subject from message headers
    /// </summary>
    public static string ExtractSubject(Message message)
    {
        var subjectHeader = message.Payload?.Headers?.FirstOrDefault(
            h => h.Name.Equals("Subject", StringComparison.OrdinalIgnoreCase));
        return subjectHeader?.Value ?? "(no subject)";
    }

    /// <summary>
    /// Extract received date from message headers
    /// </summary>
    public static DateTime ExtractReceivedDate(Message message)
    {
        var dateHeader = message.Payload?.Headers?.FirstOrDefault(
            h => h.Name.Equals("Date", StringComparison.OrdinalIgnoreCase));

        if (dateHeader != null && DateTime.TryParse(dateHeader.Value, out var date))
            return date;

        // Fallback: use current time if we can't parse
        return DateTime.UtcNow;
    }

    /// <summary>
    /// Extract recipients from message headers
    /// </summary>
    public static string ExtractRecipients(Message message)
    {
        var recipients = new List<string>();

        foreach (var header in message.Payload?.Headers ?? new List<MessagePartHeader>())
        {
            if (header.Name.Equals("To", StringComparison.OrdinalIgnoreCase) ||
                header.Name.Equals("Cc", StringComparison.OrdinalIgnoreCase))
            {
                recipients.Add(header.Value);
            }
        }

        return string.Join("; ", recipients);
    }

    private void EnsureAuthenticated()
    {
        if (_gmailService == null)
            throw new InvalidOperationException("Not authenticated. Call AuthenticateAsync first.");
    }

    /// <summary>
    /// Get the underlying GmailService (for other operations)
    /// </summary>
    public GmailService GetService()
    {
        EnsureAuthenticated();
        return _gmailService!;
    }
}
