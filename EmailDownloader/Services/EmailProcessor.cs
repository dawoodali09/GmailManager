using EmailDownloader.Configuration;
using EmailDownloader.Models;

namespace EmailDownloader.Services;

/// <summary>
/// Main orchestrator for email processing workflow.
/// Coordinates between Gmail, local storage, classification, and metadata persistence.
/// 
/// Workflow:
/// 1. Load existing metadata from CSV
/// 2. Fetch all messages from Gmail
/// 3. Identify and download new messages
/// 4. Process unprocessed emails (classify, apply rules, optionally delete)
/// 5. Save updated metadata
/// </summary>
public class EmailProcessor
{
    private readonly AppSettings _settings;
    private readonly ConsoleLogger _logger;
    private readonly GmailServiceWrapper _gmailService;
    private readonly EmailStorage _emailStorage;
    private readonly MetadataRepository _metadataRepo;
    private readonly NaughtySenderRepository _naughtySenders;
    private readonly EmailClassifier _classifier;

    private int _totalNewEmails;
    private int _totalDownloaded;
    private int _totalProcessed;
    private int _totalDeleted;
    private int _totalImportant;
    private int _totalNotImportant;
    private int _totalWithAttachments;
    private int _totalWithUnsubscribe;
    private int _totalNaughtySenders;
    private int _totalErrors;

    public EmailProcessor(
        AppSettings settings,
        ConsoleLogger logger,
        GmailServiceWrapper gmailService,
        EmailStorage emailStorage,
        MetadataRepository metadataRepo,
        NaughtySenderRepository naughtySenders,
        EmailClassifier classifier)
    {
        _settings = settings;
        _logger = logger;
        _gmailService = gmailService;
        _emailStorage = emailStorage;
        _metadataRepo = metadataRepo;
        _naughtySenders = naughtySenders;
        _classifier = classifier;
    }

    /// <summary>
    /// Execute the full email processing workflow
    /// </summary>
    public async Task ProcessAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInfo("================================");
            _logger.LogInfo("Email Manager - Processing Started");
            _logger.LogInfo($"Dry-run mode: {_settings.ApplicationConfiguration.DryRunMode}");
            _logger.LogInfo("================================");

            // Step 1: Load existing metadata
            _logger.LogInfo("Step 1/4: Loading existing metadata...");
            await _metadataRepo.LoadAsync();

            // Step 2: Authenticate with Gmail
            _logger.LogInfo("Step 2/4: Authenticating with Gmail...");
            await _gmailService.AuthenticateAsync(cancellationToken);

            // Step 3: Fetch and download new emails
            _logger.LogInfo("Step 3/4: Fetching and downloading emails...");
            await FetchAndDownloadNewEmailsAsync(cancellationToken);

            // Step 4: Process unprocessed emails
            _logger.LogInfo("Step 4/4: Processing unprocessed emails...");
            await ProcessUnprocessedEmailsAsync(cancellationToken);

            // Save metadata
            await _metadataRepo.SaveAsync();

            LogSummary();
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Processing cancelled by user");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Processing failed: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Fetch all messages from Gmail and download new ones
    /// </summary>
    private async Task FetchAndDownloadNewEmailsAsync(CancellationToken cancellationToken)
    {
        var allMessages = await _gmailService.FetchAllMessagesAsync(cancellationToken);

        foreach (var msgStub in allMessages)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            try
            {
                // Skip if already in metadata
                if (_metadataRepo.Exists(msgStub.Id))
                    continue;

                _totalNewEmails++;

                // Fetch full message details
                var fullMessage = await _gmailService.GetMessageAsync(msgStub.Id, cancellationToken);
                var (senderEmail, senderName) = GmailServiceWrapper.ExtractSenderInfo(fullMessage);
                var subject = GmailServiceWrapper.ExtractSubject(fullMessage);
                var receivedDate = GmailServiceWrapper.ExtractReceivedDate(fullMessage);
                var recipients = GmailServiceWrapper.ExtractRecipients(fullMessage);

                // Classify email
                var classification = _classifier.Classify(fullMessage, senderEmail, senderName);

                // Track stats
                if (classification.HasAttachment)
                    _totalWithAttachments++;
                if (classification.ContainsUnsubscribe)
                    _totalWithUnsubscribe++;
                if (classification.MatchingNaughtySender != null)
                    _totalNaughtySenders++;

                // Download email and attachments
                string localPath = await _emailStorage.DownloadEmailAsync(_gmailService.GetService(), msgStub.Id);
                await _emailStorage.DownloadAttachmentsAsync(_gmailService.GetService(), fullMessage);
                _totalDownloaded++;

                // Create metadata record
                var metadata = new EmailMetadata
                {
                    MessageId = msgStub.Id,
                    ThreadId = msgStub.ThreadId ?? string.Empty,
                    Subject = subject,
                    Sender = senderEmail,
                    Recipients = recipients,
                    ReceivedDate = receivedDate,
                    HasAttachment = classification.HasAttachment,
                    Important = classification.IsImportant,
                    Processed = false, // Will be processed in next phase
                    LocalPath = localPath
                };

                _metadataRepo.AddOrUpdate(metadata);

                _logger.LogDebug($"Downloaded: {subject} from {senderEmail}");
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Failed to process message {msgStub.Id}: {ex.Message}");
                _totalErrors++;
            }
        }

        _logger.LogInfo($"Found {_totalNewEmails} new emails, downloaded {_totalDownloaded}");
    }

    /// <summary>
    /// Process emails that haven't been processed yet
    /// </summary>
    private async Task ProcessUnprocessedEmailsAsync(CancellationToken cancellationToken)
    {
        var unprocessed = _metadataRepo.GetUnprocessed().ToList();

        foreach (var metadata in unprocessed)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            try
            {
                // Fetch full message to re-evaluate (might have changed)
                var fullMessage = await _gmailService.GetMessageAsync(metadata.MessageId, cancellationToken);
                var (senderEmail, senderName) = GmailServiceWrapper.ExtractSenderInfo(fullMessage);

                // Re-classify
                var classification = _classifier.Classify(fullMessage, senderEmail, senderName);

                // Update metadata with classification results
                metadata.Important = classification.IsImportant;
                metadata.HasAttachment = classification.HasAttachment;
                metadata.Sender = senderEmail; // Update in case it changed

                bool shouldDelete = false;
                string deleteReason = string.Empty;

                // Apply deletion rules in order
                // 1. Naughty sender
                if (classification.MatchingNaughtySender != null)
                {
                    shouldDelete = true;
                    deleteReason = $"Naughty sender: {classification.MatchingNaughtySender}";
                }
                // 2. Not important
                else if (!metadata.Important)
                {
                    shouldDelete = true;
                    deleteReason = "Not important";
                }

                // Execute deletion if needed
                if (shouldDelete)
                {
                    if (_settings.ApplicationConfiguration.DryRunMode)
                    {
                        _logger.LogInfo($"[DRY-RUN] Would delete: {metadata.Sender} | {metadata.Subject} | Reason: {deleteReason}");
                    }
                    else
                    {
                        await _gmailService.DeleteMessageAsync(metadata.MessageId, cancellationToken);
                        _logger.LogInfo($"[DELETED] {metadata.Sender} | {metadata.Subject} | Reason: {deleteReason}");
                        _totalDeleted++;
                    }
                }

                // Mark as processed
                _metadataRepo.MarkAsProcessed(metadata.MessageId);
                _totalProcessed++;

                if (metadata.Important)
                    _totalImportant++;
                else
                    _totalNotImportant++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Failed to process email {metadata.MessageId}: {ex.Message}");
                _totalErrors++;
            }
        }

        _logger.LogInfo($"Processed {_totalProcessed} unprocessed emails");
    }

    /// <summary>
    /// Log processing summary statistics
    /// </summary>
    private void LogSummary()
    {
        _logger.LogInfo("================================");
        _logger.LogInfo("Processing Summary");
        _logger.LogInfo("================================");
        _logger.LogInfo($"Total emails fetched from Gmail: {_totalNewEmails}");
        _logger.LogInfo($"Total emails downloaded: {_totalDownloaded}");
        _logger.LogInfo($"Total emails processed: {_totalProcessed}");
        _logger.LogInfo($"  - Marked as important: {_totalImportant}");
        _logger.LogInfo($"  - Marked as not important: {_totalNotImportant}");
        _logger.LogInfo($"Total emails with attachments: {_totalWithAttachments}");
        _logger.LogInfo($"Total emails with unsubscribe: {_totalWithUnsubscribe}");
        _logger.LogInfo($"Total naughty senders matched: {_totalNaughtySenders}");
        _logger.LogInfo($"Total deleted: {_totalDeleted}");
        _logger.LogInfo($"Total errors: {_totalErrors}");
        _logger.LogInfo($"Storage location: {_settings.ApplicationConfiguration.EmailStoragePath}");
        _logger.LogInfo("================================");
    }
}
