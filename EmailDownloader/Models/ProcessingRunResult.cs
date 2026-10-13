namespace EmailDownloader.Models;

public class ProcessingRunResult
{
    public bool Success { get; set; }
    public bool WasCancelled { get; set; }
    public bool QuotaExceeded { get; set; }
    public string Message { get; set; } = string.Empty;
}
