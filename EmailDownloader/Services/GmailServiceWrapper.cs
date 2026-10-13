using EmailDownloader.Configuration;
using Google;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System.Linq;

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
    private readonly Random _jitter = new();

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

            // Resolve credentials path across common locations
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

                var found = probeCreds.FirstOrDefault(File.Exists);
                if (found != null)
                {
                    credentialsPath = found;
                }
            }

            if (!File.Exists(credentialsPath))
            {
                throw new FileNotFoundException(
                    $"Credentials file not found: {credentialsPath}\n" +
                    "Please download credentials.json from Google Cloud Console.");
            }

            // Load OAuth credentials from file
            using (var stream = new FileStream(credentialsPath, FileMode.Open, FileAccess.Read))
            {
                var secrets = GoogleClientSecrets.FromStream(stream).Secrets;

                var tokenFolder = Path.GetDirectoryName(tokenPath);
                if (string.IsNullOrWhiteSpace(tokenFolder))
                {
                    tokenFolder = Path.GetDirectoryName(credentialsPath) ?? AppContext.BaseDirectory;
                }

                credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    secrets,
                    Scopes,
                    "user",
                    cancellationToken,
                    new FileDataStore(tokenFolder, true));
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

    private int EffectiveDelayMs() => Math.Max(_settings.EmailProcessing.RequestDelayMs, (int)Math.Ceiling(60000.0 / Math.Max(1, _settings.EmailProcessing.MaxRequestsPerMinute)));

    private async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> action)
    {
        var attempts = Math.Max(1, _settings.EmailProcessing.RetryAttempts);
        var delay = Math.Max(100, _settings.EmailProcessing.RetryDelayMs);

        for (int i = 1; ; i++)
        {
            try
            {
                return await action();
            }
            catch (Exception ex)
            {
                if (IsQuotaExceeded(ex))
                {
                    _logger.LogWarning("Gmail API quota/rate limit reached. Ending current run and deferring to next scheduled run.");
                    throw new GmailQuotaExceededException("Gmail API quota/rate limit exceeded.", ex);
                }

                // If last attempt, rethrow
                if (i >= attempts)
                {
                    throw;
                }

                // Log and back off
                _logger.LogWarning($"Request failed (attempt {i}/{attempts}): {ex.Message}. Retrying after backoff...");

                // Exponential backoff with jitter
                var backoff = delay * (int)Math.Pow(2, i - 1);
                var jitter = _jitter.Next(0, Math.Min(1000, backoff / 2));
                var wait = Math.Min(backoff + jitter, 60000);
                await Task.Delay(wait);
            }
        }
    }

    private static bool IsQuotaExceeded(Exception ex)
    {
        if (ex is GmailQuotaExceededException)
        {
            return true;
        }

        if (ex is GoogleApiException googleEx)
        {
            var code = googleEx.HttpStatusCode;
            if ((int)code == 429)
            {
                return true;
            }

            var reasons = googleEx.Error?.Errors?.Select(e => e.Reason?.ToLowerInvariant()).Where(r => !string.IsNullOrWhiteSpace(r)).ToList() ?? new List<string>();
            if (reasons.Any(r => r is "ratelimitexceeded" or "userratelimitexceeded" or "quotaexceeded" or "dailylimitexceeded"))
            {
                return true;
            }
        }

        var msg = ex.Message.ToLowerInvariant();
        return msg.Contains("quota") || msg.Contains("rate limit") || msg.Contains("too many requests");
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
            var maxResults = Math.Max(10, _settings.EmailProcessing.MaxResultsPerPage);
            var delayMs = EffectiveDelayMs();

            do
            {
                var request = _gmailService!.Users.Messages.List("me");
                request.MaxResults = maxResults;
                request.PageToken = pageToken;
                request.Fields = "messages(id,threadId),nextPageToken"; // Only fetch lightweight message stubs

                var response = await ExecuteWithRetryAsync(() => request.ExecuteAsync(cancellationToken));

                if (response.Messages != null)
                {
                    allMessages.AddRange(response.Messages);
                    pageCount++;
                }

                _logger.LogDebug($"Fetched page {pageCount}: {allMessages.Count} messages total so far...");
                pageToken = response.NextPageToken;

                // Respect rate limits by delaying between pages
                if (pageToken != null)
                    await Task.Delay(delayMs, cancellationToken);

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
    /// Stream mailbox message stubs page by page so callers can start work before the full listing completes
    /// </summary>
    public async IAsyncEnumerable<List<Message>> ListMessagePagesAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();

        string? pageToken = null;
        int pageCount = 0;
        int total = 0;
        var maxResults = Math.Max(10, _settings.EmailProcessing.MaxResultsPerPage);
        var delayMs = EffectiveDelayMs();

        do
        {
            var request = _gmailService!.Users.Messages.List("me");
            request.MaxResults = maxResults;
            request.PageToken = pageToken;
            request.Fields = "messages(id,threadId),nextPageToken";

            var response = await ExecuteWithRetryAsync(() => request.ExecuteAsync(cancellationToken));
            pageToken = response.NextPageToken;
            pageCount++;

            var page = response.Messages?.ToList() ?? new List<Message>();
            total += page.Count;
            _logger.LogInfo($"Listing mailbox: page {pageCount}, {total} messages discovered so far");

            if (page.Count > 0)
                yield return page;

            if (pageToken != null)
                await Task.Delay(delayMs, cancellationToken);

        } while (pageToken != null);

        _logger.LogInfo($"Mailbox listing complete: {total} messages across {pageCount} pages");
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

            // Rate limit before executing
            await Task.Delay(EffectiveDelayMs(), cancellationToken);

            return await ExecuteWithRetryAsync(() => request.ExecuteAsync(cancellationToken));
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to get message {messageId}: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Return message ids that changed since a given history id using the History API
    /// </summary>
    public async Task<List<string>> GetMessageIdsSinceHistoryAsync(long startHistoryId, CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        var ids = new List<string>();
        string? pageToken = null;

        try
        {
            do
            {
                var request = _gmailService!.Users.History.List("me");
                request.StartHistoryId = (ulong)startHistoryId;
                request.PageToken = pageToken;
                request.Fields = "history(messagesAdded(messages(id,threadId))),nextPageToken";

                var response = await ExecuteWithRetryAsync(() => request.ExecuteAsync(cancellationToken));
                if (response.History != null)
                {
                    foreach (var h in response.History)
                    {
                        if (h.MessagesAdded != null)
                        {
                            foreach (var ma in h.MessagesAdded)
                            {
                                if (ma.Message != null && !string.IsNullOrWhiteSpace(ma.Message.Id))
                                    ids.Add(ma.Message.Id);
                            }
                        }
                    }
                }

                pageToken = response.NextPageToken;
                if (pageToken != null)
                    await Task.Delay(EffectiveDelayMs(), cancellationToken);

            } while (pageToken != null);

            return ids.Distinct().ToList();
        }
        catch (Exception ex)
        {
            if (ex is GmailQuotaExceededException)
                throw;

            _logger.LogWarning($"History API failed: {ex.Message}. Falling back to full listing.");
            return new List<string>();
        }
    }

    /// <summary>
    /// Get current profile history id (if available)
    /// </summary>
    public async Task<long?> GetCurrentHistoryIdAsync(CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        try
        {
            var profile = await ExecuteWithRetryAsync(() => _gmailService!.Users.GetProfile("me").ExecuteAsync(cancellationToken));
            if (profile.HistoryId != null)
            {
                try
                {
                    return (long)profile.HistoryId.Value;
                }
                catch { }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Failed to get profile/history id: {ex.Message}");
        }

        return null;
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
