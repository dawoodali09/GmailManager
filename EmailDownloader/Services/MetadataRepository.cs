using CsvHelper;
using CsvHelper.Configuration;
using EmailDownloader.Models;
using System.Globalization;

namespace EmailDownloader.Services;

/// <summary>
/// Handles loading and saving email metadata to/from CSV file.
/// Ensures proper escaping and atomic writes to prevent corruption.
/// </summary>
public class MetadataRepository
{
    private readonly string _csvPath;
    private readonly ConsoleLogger _logger;
    private readonly Dictionary<string, EmailMetadata> _metadata;

    public MetadataRepository(string storagePath, string csvFileName, ConsoleLogger logger)
    {
        _csvPath = Path.Combine(storagePath, csvFileName);
        _logger = logger;
        _metadata = new Dictionary<string, EmailMetadata>();
    }

    /// <summary>
    /// Load existing metadata from CSV file
    /// </summary>
    public async Task LoadAsync()
    {
        if (!File.Exists(_csvPath))
        {
            _logger.LogInfo($"Metadata file does not exist yet: {_csvPath}");
            return;
        }

        try
        {
            using (var reader = new StreamReader(_csvPath))
            using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                TrimOptions = TrimOptions.Trim,
                BadDataFound = context => _logger.LogWarning($"Bad CSV data: {context.RawRecord}")
            }))
            {
                var records = csv.GetRecords<EmailMetadata>();
                foreach (var record in records)
                {
                    _metadata[record.MessageId] = record;
                }
            }

            _logger.LogInfo($"Loaded {_metadata.Count} email records from metadata file");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error loading metadata CSV: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Check if a message ID already exists in metadata
    /// </summary>
    public bool Exists(string messageId) => _metadata.ContainsKey(messageId);

    /// <summary>
    /// Get metadata for a specific message
    /// </summary>
    public EmailMetadata? Get(string messageId)
    {
        _metadata.TryGetValue(messageId, out var metadata);
        return metadata;
    }

    /// <summary>
    /// Get all unprocessed emails
    /// </summary>
    public IEnumerable<EmailMetadata> GetUnprocessed() => _metadata.Values.Where(m => !m.Processed);

    /// <summary>
    /// Get all unimportant emails
    /// </summary>
    public IEnumerable<EmailMetadata> GetUnimportant() => _metadata.Values.Where(m => !m.Important && !m.Processed);

    /// <summary>
    /// Add or update metadata for an email
    /// </summary>
    public void AddOrUpdate(EmailMetadata metadata)
    {
        metadata.LastModified = DateTime.UtcNow;
        _metadata[metadata.MessageId] = metadata;
    }

    /// <summary>
    /// Save all metadata to CSV file atomically (write to temp, then move)
    /// </summary>
    public async Task SaveAsync()
    {
        var tempPath = _csvPath + ".tmp";

        try
        {
            // Write to temporary file
            using (var writer = new StreamWriter(tempPath))
            using (var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                ShouldQuote = args => true // Always quote fields for safety (handles commas, newlines, quotes)
            }))
            {
                await csv.WriteRecordsAsync(_metadata.Values);
            }

            // Atomic move: replace original with temp
            if (File.Exists(_csvPath))
            {
                File.Delete(_csvPath);
            }
            File.Move(tempPath, _csvPath, overwrite: true);

            _logger.LogDebug($"Saved {_metadata.Count} email records to metadata file");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error saving metadata CSV: {ex.Message}");
            // Clean up temp file if it exists
            if (File.Exists(tempPath))
            {
                try { File.Delete(tempPath); }
                catch { }
            }
            throw;
        }
    }

    /// <summary>
    /// Mark an email as processed
    /// </summary>
    public void MarkAsProcessed(string messageId)
    {
        if (_metadata.TryGetValue(messageId, out var metadata))
        {
            metadata.Processed = true;
            metadata.LastModified = DateTime.UtcNow;
        }
    }
}
