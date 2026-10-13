using EmailDownloader.Configuration;
using EmailDownloader.Services;
using Xunit;

namespace EmailDownloader.Tests;

public class EmailProcessingRunnerTests
{
    [Fact]
    public async Task RunOnceAsync_MissingCredentials_ShouldFailWithMessage()
    {
        var settings = new AppSettings
        {
            GmailConfiguration = new GmailConfiguration
            {
                CredentialsPath = Path.Combine(Path.GetTempPath(), $"missing_{Guid.NewGuid()}.json"),
                TokenPath = Path.Combine(Path.GetTempPath(), $"token_{Guid.NewGuid()}.json")
            },
            ApplicationConfiguration = new ApplicationConfiguration
            {
                EmailStoragePath = Path.Combine(Path.GetTempPath(), $"storage_{Guid.NewGuid()}"),
                MetadataFileName = "emails.csv",
                NaughtySendersFileName = "naughty_senders.csv",
                DryRunMode = true,
                LoggingLevel = "Error"
            }
        };

        var logger = new ConsoleLogger("Error", writeToConsole: false);
        var runner = new EmailProcessingRunner();

        var result = await runner.RunOnceAsync(settings, logger);

        Assert.False(result.Success);
        Assert.Contains("Credentials file not found", result.Message);
    }

    [Fact]
    public void ConsoleLogger_WithSink_ShouldForwardMessages()
    {
        var lines = new List<string>();
        var logger = new ConsoleLogger("Info", messageSink: lines.Add, writeToConsole: false);

        logger.LogInfo("hello");
        logger.LogError("boom");

        Assert.Equal(2, lines.Count);
        Assert.Contains("hello", lines[0]);
        Assert.Contains("boom", lines[1]);
    }
}
