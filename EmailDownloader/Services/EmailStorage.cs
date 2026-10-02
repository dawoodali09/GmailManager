using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;

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

    public EmailStorage(string storagePath, long maxAttachmentSizeMB, ConsoleLogger logger)
    {
        _storagePath = storagePath;
        _logger = logger;
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

        if (message?.Payload?.Parts == null)
            return attachments;

        foreach (var part in message.Payload.Parts)
        {
            if (string.IsNullOrEmpty(part.Filename) || part.MimeType == null)
                continue; // Skip non-attachment parts

            try
            {
                // Skip text parts (they're not true attachments)
                if (part.MimeType.StartsWith("text/"))
                    continue;

                // Get the attachment
                string partId = part.PartId ?? string.Empty;
                if (string.IsNullOrEmpty(partId))
                    continue;

                var attachmentRequest = gmailService.Users.Messages.Attachments.Get(
                    "me", message.Id, partId);
                var attachment = await attachmentRequest.ExecuteAsync();

                if (attachment?.Data == null)
                    continue;

                byte[] attachmentData = DecodeBase64Url(attachment.Data);

                // Check size limit
                if (attachmentData.Length > _maxAttachmentBytes)
                {
                    _logger.LogWarning($"Attachment {part.Filename} exceeds size limit ({attachmentData.Length} bytes > {_maxAttachmentBytes} bytes). Skipping.");
                    continue;
                }

                // Store attachment in message subdirectory
                string messageDir = Path.Combine(_storagePath, message.Id);
                Directory.CreateDirectory(messageDir);

                // Sanitize filename
                string safeFilename = SanitizeFilename(part.Filename);
                string attachmentPath = Path.Combine(messageDir, safeFilename);

                // Handle filename collisions
                int counter = 1;
                string dirPath = Path.GetDirectoryName(attachmentPath) ?? messageDir;
                string nameOnly = Path.GetFileNameWithoutExtension(safeFilename);
                string ext = Path.GetExtension(safeFilename);
                while (File.Exists(attachmentPath))
                {
                    attachmentPath = Path.Combine(dirPath, $"{nameOnly}_{counter}{ext}");
                    counter++;
                }

                await File.WriteAllBytesAsync(attachmentPath, attachmentData);
                attachments.Add((attachmentPath, attachmentData.Length));

                _logger.LogDebug($"Downloaded attachment {part.Filename} ({attachmentData.Length} bytes)");
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Failed to download attachment {part.Filename} from message {message.Id}: {ex.Message}");
                // Continue processing other attachments
            }
        }

        return attachments;
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
