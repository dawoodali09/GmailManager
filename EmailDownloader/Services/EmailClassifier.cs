using EmailDownloader.Configuration;
using EmailDownloader.Models;
using Google.Apis.Gmail.v1.Data;

namespace EmailDownloader.Services;

/// <summary>
/// Classifies emails based on importance rules.
/// 
/// Classification precedence (as documented):
/// 1. Check if sender is in naughty sender list -> will be deleted (not marked as important)
/// 2. Check for unsubscribe content -> Important = false (overrides attachment rule)
/// 3. Otherwise check if important:
///    - sender/address contains "dawood" or "wajeeha" (case-insensitive) -> Important
///    - OR email has attachment -> Important
/// 4. If none of the above -> Not important
/// </summary>
public class EmailClassifier
{
    private readonly AppSettings _settings;
    private readonly ConsoleLogger _logger;
    private readonly NaughtySenderRepository _naughtySenders;

    public EmailClassifier(AppSettings settings, ConsoleLogger logger, NaughtySenderRepository naughtySenders)
    {
        _settings = settings;
        _logger = logger;
        _naughtySenders = naughtySenders;
    }

    /// <summary>
    /// Classify an email based on its content and headers
    /// </summary>
    public EmailClassification Classify(Message message, string senderEmail, string senderName)
    {
        var result = new EmailClassification();
        var reasons = new List<string>();

        // Step 1: Check if sender is naughty
        var naughtySender = _naughtySenders.FindMatch(senderEmail);
        if (naughtySender != null)
        {
            result.MatchingNaughtySender = naughtySender.Sender;
            reasons.Add($"Naughty sender: {naughtySender.Sender}");
            // Don't mark as important; will be deleted regardless of other rules
        }

        // Step 2: Check for unsubscribe content (overrides attachment rule)
        if (ContainsUnsubscribeIndicators(message))
        {
            result.ContainsUnsubscribe = true;
            result.IsImportant = false;
            reasons.Add("Contains unsubscribe indicators");
            result.MatchReasons = reasons;
            return result; // Early return: unsubscribe = not important, even if has attachment
        }

        // Step 3: Check for attachment
        result.HasAttachment = message.Payload?.Parts?.Any(p => 
            !string.IsNullOrEmpty(p.Filename) && !string.IsNullOrWhiteSpace(p.Filename)) ?? false;
        if (result.HasAttachment)
        {
            result.IsImportant = true;
            reasons.Add("Has attachment");
        }

        // Step 4: Check if sender contains important keywords
        if (ContainsImportantKeyword(senderEmail, senderName))
        {
            result.IsImportant = true;
            reasons.Add($"Sender contains important keyword (dawood/wajeeha)");
        }

        if (!result.IsImportant)
        {
            reasons.Add("No importance criteria met");
        }

        result.MatchReasons = reasons;
        return result;
    }

    /// <summary>
    /// Check if email content or headers contain unsubscribe indicators
    /// </summary>
    private bool ContainsUnsubscribeIndicators(Message message)
    {
        // Check List-Unsubscribe header
        var unsubscribeHeader = message.Payload?.Headers?.FirstOrDefault(
            h => h.Name.Equals("List-Unsubscribe", StringComparison.OrdinalIgnoreCase));
        if (unsubscribeHeader != null && !string.IsNullOrWhiteSpace(unsubscribeHeader.Value))
            return true;

        // Check body for unsubscribe keywords
        var bodyText = ExtractBodyText(message).ToLowerInvariant();
        foreach (var keyword in _settings.ClassificationRules.UnsubscribeKeywords)
        {
            if (bodyText.Contains(keyword.ToLowerInvariant()))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Check if sender email or name contains important keywords
    /// </summary>
    private bool ContainsImportantKeyword(string senderEmail, string senderName)
    {
        var combined = $"{senderEmail} {senderName}".ToLowerInvariant();

        foreach (var keyword in _settings.ClassificationRules.ImportantDomains)
        {
            if (combined.Contains(keyword.ToLowerInvariant()))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Extract the text body from a Gmail message payload
    /// Handles both plain text and multipart messages
    /// </summary>
    private string ExtractBodyText(Message message)
    {
        if (message?.Payload == null)
            return string.Empty;

        var payload = message.Payload;

        // If it's plain text or HTML, decode the body
        if (payload.MimeType != null && (payload.MimeType.StartsWith("text/")))
        {
            try
            {
                if (payload.Body?.Data != null)
                {
                    var decoded = System.Text.Encoding.UTF8.GetString(
                        Convert.FromBase64String(
                            payload.Body.Data.Replace('-', '+').Replace('_', '/')));
                    return decoded;
                }
            }
            catch
            {
                // Ignore decoding errors
            }
        }

        // If multipart, look for text/plain or text/html parts
        if (payload.Parts != null)
        {
            var textPart = payload.Parts.FirstOrDefault(
                p => p.MimeType == "text/plain" || p.MimeType == "text/html");
            if (textPart?.Body?.Data != null)
            {
                try
                {
                    var decoded = System.Text.Encoding.UTF8.GetString(
                        Convert.FromBase64String(
                            textPart.Body.Data.Replace('-', '+').Replace('_', '/')));
                    return decoded;
                }
                catch
                {
                    // Ignore decoding errors
                }
            }
        }

        return string.Empty;
    }
}
