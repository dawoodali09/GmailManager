using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using EmailDownloader.Configuration;

namespace EmailDownloader.Services;

/// <summary>
/// Handles downloading and storing emails locally.
/// Downloads emails in RAW format (full MIME message as .eml files).
/// Also downloads attachments with size limits.
/// </summary>
public class EmailStorage
{
    private readonly string _storagePath;
    private readonly ConsoleLogger _logger;
    private readonly long _maxAttachmentBytes;
    private readonly EmailProcessing _processingSettings;

    public EmailStorage(string storagePath, long maxAttachmentSizeMB, ConsoleLogger logger, EmailProcessing processingSettings)
    {
        _storagePath = storagePath;
        _logger = logger;
        _processingSettings = processingSettings;
        _maxAttachmentBytes = maxAttachmentSizeMB * 1024 * 1024;

        Directory.CreateDirectory(_storagePath);
    }

    /// <summary>
    /// Download an email and store it locally as .eml file
    /// Returns the local path to the stored file
    /// </summary>
    public async Task<string> DownloadEmailAsync(GmailService gmailService, string messageId)
    {
        try
        {
            var request = gmailService.Users.Messages.Get("me", messageId);
            request.Format = UsersResource.MessagesResource.GetRequest.FormatEnum.Raw;

            var message = await request.ExecuteAsync();

            // Decode the raw message (base64url)
            byte[] messageData = DecodeBase64Url(message.Raw);

            // Store in a subdirectory based on message ID (prevents collisions)
            string messageDir = Path.Combine(_storagePath, messageId);
            Directory.CreateDirectory(messageDir);

            string filePath = Path.Combine(messageDir, "message.eml");
            await File.WriteAllBytesAsync(filePath, messageData);

            _logger.LogDebug($"Downloaded email {messageId} to {filePath}");
            return filePath;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to download email {messageId}: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Download attachments from an email
    /// Returns list of downloaded attachment paths and their sizes
    /// </summary>
    public async Task<List<(string path, long size)>> DownloadAttachmentsAsync(
        GmailService gmailService, Message message)
    {
        var attachments = new List<(string path, long size)>();

        if (message?.Payload == null)
            return attachments;

        // Collect all candidate parts recursively
        var parts = new List<MessagePart>();
        void CollectParts(MessagePart p)
        {
            if (p == null) return;
            parts.Add(p);
            if (p.Parts != null)
            {
                foreach (var child in p.Parts)
                    CollectParts(child);
            }
        }

        CollectParts(message.Payload);

        // Ensure message directory exists
        string messageDir = Path.Combine(_storagePath, message.Id);
        Directory.CreateDirectory(messageDir);

        foreach (var part in parts)
        {
            try
            {
                // Determine if this part represents an attachment
                var hasFilename = !string.IsNullOrWhiteSpace(part.Filename);
                var hasAttachmentId = !string.IsNullOrWhiteSpace(part.Body?.AttachmentId);
                var hasInlineData = !string.IsNullOrWhiteSpace(part.Body?.Data);

                if (!hasFilename && !hasAttachmentId && !hasInlineData)
                    continue; // not an attachment

                // Skip obvious text-only parts
                var mime = part.MimeType ?? string.Empty;
                if (mime.StartsWith("text/") && !hasAttachmentId && !hasInlineData)
                    continue;

                // Choose filename
                string filename = part.Filename;
                if (string.IsNullOrWhiteSpace(filename))
                {
                    // Create a sensible filename
                    var ext = GetExtensionFromMime(mime);
                    if (hasAttachmentId)
                        filename = $"attachment_{part.Body!.AttachmentId}{ext}";
                    else
                        filename = $"part_{Guid.NewGuid()}{ext}";
                }

                string safeFilename = SanitizeFilename(filename);
                string attachmentPath = Path.Combine(messageDir, safeFilename);

                // Handle filename collisions
                int counter = 1;
                string dirPath = Path.GetDirectoryName(attachmentPath) ?? messageDir;
                string nameOnly = Path.GetFileNameWithoutExtension(safeFilename);
                string extOnly = Path.GetExtension(safeFilename);
                while (File.Exists(attachmentPath))
                {
                    attachmentPath = Path.Combine(dirPath, $"{nameOnly}_{counter}{extOnly}");
                    counter++;
                }

                byte[] attachmentData = Array.Empty<byte>();

                if (hasInlineData)
                {
                    attachmentData = DecodeBase64Url(part.Body!.Data!);
                }
                else if (hasAttachmentId)
                {
                    var attachmentId = part.Body!.AttachmentId!;
                    var attachmentRequest = gmailService.Users.Messages.Attachments.Get("me", message.Id, attachmentId);
                    var attachment = await attachmentRequest.ExecuteAsync();
                    if (attachment?.Data == null)
                    {
                        _logger.LogWarning($"Attachment {filename} returned empty data for message {message.Id}");
                        continue;
                    }

                    attachmentData = DecodeBase64Url(attachment.Data);
                }

                if (attachmentData.Length == 0)
                {
                    _logger.LogWarning($"Attachment {filename} from message {message.Id} is empty, skipping.");
                    continue;
                }

                // Check size limit
                if (attachmentData.Length > _maxAttachmentBytes)
                {
                    _logger.LogWarning($"Attachment {filename} exceeds size limit ({attachmentData.Length} bytes > {_maxAttachmentBytes} bytes). Skipping.");
                    continue;
                }

                await File.WriteAllBytesAsync(attachmentPath, attachmentData);
                attachments.Add((attachmentPath, attachmentData.Length));

                _logger.LogInfo($"Downloaded attachment {filename} -> {attachmentPath} ({attachmentData.Length} bytes)");
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Failed to download attachment {part.Filename} from message {message.Id}: {ex.Message}");
                // Continue processing other attachments
            }
        }

        return attachments;
    }

    private static string GetExtensionFromMime(string mime)
    {
        if (string.IsNullOrWhiteSpace(mime)) return string.Empty;

        // Minimal mapping for common types
        return mime.ToLowerInvariant() switch
        {
            "application/pdf" => ".pdf",
            "image/jpeg" => ".jpg",
            "image/jpg" => ".jpg",
            "image/png" => ".png",
            "image/gif" => ".gif",
            "text/plain" => ".txt",
            "text/html" => ".html",
            _ => string.Empty
        };
    }

    /// <summary>
    /// Get the local storage path for a message
    /// </summary>
    public string GetMessageStoragePath(string messageId)
    {
        return Path.Combine(_storagePath, messageId);
    }

    /// <summary>
    /// Decode a base64url-encoded string (used by Gmail API)
    /// Handles the base64url variant with - and _ characters
    /// </summary>
    private byte[] DecodeBase64Url(string base64Url)
    {
        string base64 = base64Url
            .Replace('-', '+')
            .Replace('_', '/');

        // Add padding if necessary
        int padding = base64.Length % 4;
        if (padding > 0)
        {
            base64 += new string('=', 4 - padding);
        }

        return Convert.FromBase64String(base64);
    }

    /// <summary>
    /// Sanitize a filename to prevent directory traversal and invalid characters
    /// </summary>
    private string SanitizeFilename(string filename)
    {
        // Remove invalid path characters
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(filename
            .Where(c => !invalidChars.Contains(c) && c != '.' && c != '/')
            .ToArray());

        // Limit length
        if (sanitized.Length > 255)
        {
            sanitized = sanitized.Substring(0, 250);
        }

        return string.IsNullOrWhiteSpace(sanitized) ? "attachment" : sanitized;
    }
}
