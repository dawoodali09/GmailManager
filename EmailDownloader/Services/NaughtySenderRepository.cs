using CsvHelper;
using CsvHelper.Configuration;
using EmailDownloader.Models;
using System.Globalization;

namespace EmailDownloader.Services;

/// <summary>
/// Loads and manages the naughty senders CSV file.
/// Supports case-insensitive sender matching (both full email and domain).
/// </summary>
public class NaughtySenderRepository
{
    private readonly string _csvPath;
    private readonly ConsoleLogger _logger;
    private readonly List<NaughtySender> _naughtySenders;

    public NaughtySenderRepository(string storagePath, string csvFileName, ConsoleLogger logger)
    {
        _csvPath = Path.Combine(storagePath, csvFileName);
        _logger = logger;
        _naughtySenders = new List<NaughtySender>();
    }

    /// <summary>
    /// Load naughty senders from CSV file (or create empty list if file doesn't exist)
    /// </summary>
    public async Task LoadAsync()
    {
        if (!File.Exists(_csvPath))
        {
            _logger.LogInfo($"Naughty senders file does not exist: {_csvPath}. Creating template...");
            await CreateTemplateAsync();
            return;
        }

        try
        {
            using (var reader = new StreamReader(_csvPath))
            using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                TrimOptions = TrimOptions.Trim,
                BadDataFound = context => _logger.LogWarning($"Bad CSV data in naughty senders: {context.RawRecord}")
            }))
            {
                var records = csv.GetRecords<NaughtySender>();
                _naughtySenders.AddRange(records.Where(r => r.Enabled)); // Only load enabled entries
            }

            _logger.LogInfo($"Loaded {_naughtySenders.Count} naughty senders");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error loading naughty senders CSV: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Check if a sender is in the naughty list (case-insensitive)
    /// Matches both full email address and domain
    /// </summary>
    public NaughtySender? FindMatch(string senderEmail)
    {
        if (string.IsNullOrWhiteSpace(senderEmail))
            return null;

        var normalizedSender = senderEmail.Trim().ToLowerInvariant();

        // Try exact match first
        var exactMatch = _naughtySenders.FirstOrDefault(
            ns => normalizedSender.Equals(ns.Sender.Trim().ToLowerInvariant()));
        if (exactMatch != null)
            return exactMatch;

        // Try domain match (extract domain from sender, e.g., user@example.com -> example.com)
        if (normalizedSender.Contains("@"))
        {
            var senderDomain = normalizedSender.Split('@')[1];
            var domainMatch = _naughtySenders.FirstOrDefault(
                ns => ns.Sender.Contains("@") && 
                      ns.Sender.Split('@')[1].Trim().ToLowerInvariant().Equals(senderDomain));
            if (domainMatch != null)
                return domainMatch;
        }

        return null;
    }

    /// <summary>
    /// Create a template naughty senders CSV if it doesn't exist
    /// </summary>
    private async Task CreateTemplateAsync()
    {
        try
        {
            var templateSenders = new[]
            {
                new NaughtySender { Sender = "example_spam@example.com", Enabled = false, Notes = "Add senders here that you want to auto-delete" },
                new NaughtySender { Sender = "spambot@spam.com", Enabled = false, Notes = "Format: email@domain.com or just @domain.com for all from domain" }
            };

            using (var writer = new StreamWriter(_csvPath))
            using (var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                ShouldQuote = args => true
            }))
            {
                await csv.WriteRecordsAsync(templateSenders);
            }

            _logger.LogInfo($"Created template naughty senders file: {_csvPath}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error creating naughty senders template: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Get all naughty senders
    /// </summary>
    public IReadOnlyList<NaughtySender> GetAll() => _naughtySenders.AsReadOnly();
}
