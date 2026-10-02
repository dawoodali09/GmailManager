using EmailDownloader.Configuration;
using EmailDownloader.Models;
using EmailDownloader.Services;
using Google.Apis.Gmail.v1.Data;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using Xunit;

namespace EmailDownloader.Tests;

public class EmailClassifierTests : IDisposable
{
    private readonly AppSettings _settings;
    private readonly ConsoleLogger _logger;
    private readonly EmailClassifier _classifier;
    private readonly string _testDir;

    public EmailClassifierTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"classifier_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDir);

        _settings = new AppSettings
        {
            ClassificationRules = new ClassificationRules
            {
                ImportantDomains = new List<string> { "dawood", "wajeeha" },
                UnsubscribeKeywords = new List<string> { "unsubscribe", "un-subscribe" }
            }
        };
        _logger = new ConsoleLogger("Error"); // Suppress logs during tests

        // Create a real naughty sender repo with no data
        var naughtySendersRepo = new NaughtySenderRepository(_testDir, "naughty.csv", _logger);
        // Don't load - just use empty

        _classifier = new EmailClassifier(_settings, _logger, naughtySendersRepo);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testDir))
                Directory.Delete(_testDir, recursive: true);
        }
        catch { }
    }

    [Fact]
    public void Classify_SenderContainsDawood_ShouldBeImportant()
    {
        // Arrange
        var message = new Message { Payload = new MessagePart() };
        var senderEmail = "dawood.ali@example.com";
        var senderName = "Dawood Ali";

        // Act
        var result = _classifier.Classify(message, senderEmail, senderName);

        // Assert
        Assert.True(result.IsImportant);
        Assert.Contains("important keyword", result.MatchReasons.FirstOrDefault() ?? "");
    }

    [Fact]
    public void Classify_SenderContainsWajeeha_ShouldBeImportant()
    {
        // Arrange
        var message = new Message { Payload = new MessagePart() };
        var senderEmail = "contact@wajeeha.com";
        var senderName = "Wajeeha";

        // Act
        var result = _classifier.Classify(message, senderEmail, senderName);

        // Assert
        Assert.True(result.IsImportant);
    }

    [Fact]
    public void Classify_EmailHasAttachment_ShouldBeImportant()
    {
        // Arrange
        var message = new Message
        {
            Payload = new MessagePart
            {
                Parts = new List<MessagePart>
                {
                    new MessagePart { Filename = "document.pdf" }
                }
            }
        };
        var senderEmail = "sender@example.com";
        var senderName = "Sender";

        // Act
        var result = _classifier.Classify(message, senderEmail, senderName);

        // Assert
        Assert.True(result.IsImportant);
        Assert.True(result.HasAttachment);
    }

    [Fact]
    public void Classify_NoAttachmentNoKeyword_ShouldNotBeImportant()
    {
        // Arrange
        var message = new Message
        {
            Payload = new MessagePart
            {
                Parts = new List<MessagePart>
                {
                    new MessagePart { Filename = "" } // Empty filename = not an attachment
                }
            }
        };
        var senderEmail = "random@example.com";
        var senderName = "Random Sender";

        // Act
        var result = _classifier.Classify(message, senderEmail, senderName);

        // Assert
        Assert.False(result.IsImportant);
    }

    [Fact]
    public void Classify_ContainsUnsubscribe_ShouldNotBeImportant()
    {
        // Arrange
        var message = new Message
        {
            Payload = new MessagePart
            {
                Parts = new List<MessagePart>
                {
                    new MessagePart 
                    { 
                        Filename = "file.pdf",
                        Body = new MessagePartBody()
                    }
                },
                Headers = new List<MessagePartHeader>
                {
                    new MessagePartHeader { Name = "List-Unsubscribe", Value = "<mailto:unsubscribe@example.com>" }
                }
            }
        };
        var senderEmail = "newsletter@example.com";
        var senderName = "Newsletter";

        // Act
        var result = _classifier.Classify(message, senderEmail, senderName);

        // Assert
        Assert.False(result.IsImportant); // Unsubscribe overrides attachment
        Assert.True(result.ContainsUnsubscribe);
    }

    [Fact]
    public void Classify_CaseInsensitiveKeywordMatching()
    {
        // Arrange
        var message = new Message { Payload = new MessagePart() };
        var senderEmail = "DAWOOD@EXAMPLE.COM"; // All uppercase
        var senderName = "DAWOOD";

        // Act
        var result = _classifier.Classify(message, senderEmail, senderName);

        // Assert
        Assert.True(result.IsImportant);
    }
}

public class MetadataRepositoryTests : IDisposable
{
    private readonly string _testDir;
    private readonly MetadataRepository _repo;
    private readonly ConsoleLogger _logger;

    public MetadataRepositoryTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"metadata_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDir);
        _logger = new ConsoleLogger("Error");
        _repo = new MetadataRepository(_testDir, "emails.csv", _logger);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testDir))
                Directory.Delete(_testDir, recursive: true);
        }
        catch { }
    }

    [Fact]
    public async Task LoadAsync_NoFileExists_ShouldNotThrow()
    {
        // Act & Assert
        await _repo.LoadAsync(); // Should not throw even if file doesn't exist
    }

    [Fact]
    public async Task AddOrUpdate_ThenSave_ShouldPersistData()
    {
        // Arrange
        var metadata = new EmailMetadata
        {
            MessageId = "msg123",
            Subject = "Test Subject",
            Sender = "test@example.com",
            Important = true,
            Processed = false
        };

        // Act
        _repo.AddOrUpdate(metadata);
        await _repo.SaveAsync();

        // Assert
        Assert.True(File.Exists(Path.Combine(_testDir, "emails.csv")));

        // Re-load and verify
        var repo2 = new MetadataRepository(_testDir, "emails.csv", _logger);
        await repo2.LoadAsync();
        Assert.True(repo2.Exists("msg123"));
    }

    [Fact]
    public void Exists_WithExistingMessage_ShouldReturnTrue()
    {
        // Arrange
        var metadata = new EmailMetadata { MessageId = "msg123" };
        _repo.AddOrUpdate(metadata);

        // Act
        var exists = _repo.Exists("msg123");

        // Assert
        Assert.True(exists);
    }

    [Fact]
    public void Exists_WithNonExistingMessage_ShouldReturnFalse()
    {
        // Act
        var exists = _repo.Exists("nonexistent");

        // Assert
        Assert.False(exists);
    }

    [Fact]
    public void GetUnprocessed_ShouldReturnOnlyUnprocessedEmails()
    {
        // Arrange
        _repo.AddOrUpdate(new EmailMetadata { MessageId = "msg1", Processed = false });
        _repo.AddOrUpdate(new EmailMetadata { MessageId = "msg2", Processed = true });
        _repo.AddOrUpdate(new EmailMetadata { MessageId = "msg3", Processed = false });

        // Act
        var unprocessed = _repo.GetUnprocessed().ToList();

        // Assert
        Assert.Equal(2, unprocessed.Count);
        Assert.All(unprocessed, m => Assert.False(m.Processed));
    }

    [Fact]
    public void MarkAsProcessed_ShouldUpdateFlag()
    {
        // Arrange
        _repo.AddOrUpdate(new EmailMetadata { MessageId = "msg123", Processed = false });

        // Act
        _repo.MarkAsProcessed("msg123");

        // Assert
        var metadata = _repo.Get("msg123");
        Assert.NotNull(metadata);
        Assert.True(metadata.Processed);
    }

    [Fact]
    public async Task SaveAsync_CsvWithSpecialCharacters_ShouldEscapeCorrectly()
    {
        // Arrange
        var metadata = new EmailMetadata
        {
            MessageId = "msg1",
            Subject = "Subject with \"quotes\" and, commas",
            Sender = "sender@example.com",
            Recipients = "user1@test.com; user2@test.com"
        };
        _repo.AddOrUpdate(metadata);

        // Act
        await _repo.SaveAsync();

        // Assert - read the CSV and verify escaping
        var csvPath = Path.Combine(_testDir, "emails.csv");
        Assert.True(File.Exists(csvPath));
        var content = File.ReadAllText(csvPath);
        Assert.Contains("\"", content); // Should have quotes for escaping
    }
}

public class NaughtySenderRepositoryTests : IDisposable
{
    private readonly string _testDir;
    private readonly ConsoleLogger _logger;

    public NaughtySenderRepositoryTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"naughty_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDir);
        _logger = new ConsoleLogger("Error");
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testDir))
                Directory.Delete(_testDir, recursive: true);
        }
        catch { }
    }

    [Fact]
    public async Task FindMatch_ExactEmailMatch_ShouldReturnMatch()
    {
        // Arrange
        var csv = Path.Combine(_testDir, "naughty.csv");
        using (var writer = new StreamWriter(csv))
        using (var csvWriter = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = true }))
        {
            csvWriter.WriteRecords(new[]
            {
                new NaughtySender { Sender = "spam@example.com", Enabled = true, Notes = "Test spam" }
            });
        }

        var repo = new NaughtySenderRepository(_testDir, "naughty.csv", _logger);
        await repo.LoadAsync();

        // Act
        var match = repo.FindMatch("spam@example.com");

        // Assert
        Assert.NotNull(match);
        Assert.Equal("spam@example.com", match.Sender);
    }

    [Fact]
    public async Task FindMatch_DomainMatch_ShouldReturnMatch()
    {
        // Arrange
        var csv = Path.Combine(_testDir, "naughty.csv");
        using (var writer = new StreamWriter(csv))
        using (var csvWriter = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = true }))
        {
            csvWriter.WriteRecords(new[]
            {
                new NaughtySender { Sender = "@badomain.com", Enabled = true, Notes = "Block all from domain" }
            });
        }

        var repo = new NaughtySenderRepository(_testDir, "naughty.csv", _logger);
        await repo.LoadAsync();

        // Act
        var match = repo.FindMatch("user@badomain.com");

        // Assert
        Assert.NotNull(match);
    }

    [Fact]
    public async Task FindMatch_CaseInsensitive()
    {
        // Arrange
        var csv = Path.Combine(_testDir, "naughty.csv");
        using (var writer = new StreamWriter(csv))
        using (var csvWriter = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = true }))
        {
            csvWriter.WriteRecords(new[]
            {
                new NaughtySender { Sender = "SPAM@EXAMPLE.COM", Enabled = true, Notes = "Test" }
            });
        }

        var repo = new NaughtySenderRepository(_testDir, "naughty.csv", _logger);
        await repo.LoadAsync();

        // Act
        var match = repo.FindMatch("spam@example.com");

        // Assert
        Assert.NotNull(match);
    }

    [Fact]
    public async Task FindMatch_NoMatch_ShouldReturnNull()
    {
        // Arrange
        var csv = Path.Combine(_testDir, "naughty.csv");
        using (var writer = new StreamWriter(csv))
        using (var csvWriter = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = true }))
        {
            csvWriter.WriteRecords(new[]
            {
                new NaughtySender { Sender = "spam@example.com", Enabled = true, Notes = "Test" }
            });
        }

        var repo = new NaughtySenderRepository(_testDir, "naughty.csv", _logger);
        await repo.LoadAsync();

        // Act - search for completely different domain
        var match = repo.FindMatch("legit@different.com");

        // Assert
        Assert.Null(match);
    }
}
