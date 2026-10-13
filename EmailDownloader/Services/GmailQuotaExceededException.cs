namespace EmailDownloader.Services;

public class GmailQuotaExceededException : Exception
{
    public GmailQuotaExceededException(string message)
        : base(message)
    {
    }

    public GmailQuotaExceededException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
