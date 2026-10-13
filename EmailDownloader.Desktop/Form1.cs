using EmailDownloader.Desktop.Configuration;
using EmailDownloader.Services;

namespace EmailDownloader.Desktop;

public partial class Form1 : Form
{
    private readonly DesktopSettingsStore _settingsStore;
    private readonly EmailProcessingRunner _runner;
    private DesktopAppSettings _settings;
    private CancellationTokenSource? _runCts;
    private CancellationTokenSource? _quotaWaitCts;
    private static readonly int[] QuotaBackoffMinutes = { 2, 5, 15, 30, 60 };
    private CancellationTokenSource? _schedulerCts;
    private Task? _schedulerTask;

    public Form1()
    {
        InitializeComponent();
        _settingsStore = new DesktopSettingsStore();
        _runner = new EmailProcessingRunner();
        _settings = new DesktopAppSettings();

        btnCancelRun = new Button { Text = "Cancel Run", Size = new Size(95, 30), Enabled = false };
        btnCancelRun.Click += (s, e) =>
        {
            _runCts?.Cancel();
            _quotaWaitCts?.Cancel();
            LogInfo("Cancel requested...");
        };
        lblStatus = new Label
        {
            AutoSize = false,
            AutoEllipsis = true,
            Text = "Status: Idle",
            Size = new Size(600, 30),
            TextAlign = ContentAlignment.MiddleLeft
        };
        flowLayoutPanelActions.Controls.Add(btnCancelRun);
        flowLayoutPanelActions.Controls.Add(lblStatus);
    }

    private readonly Button btnCancelRun;
    private readonly Label lblStatus;

    private void AppendLogLine(string line)
    {
        if (IsDisposed || !IsHandleCreated)
            return;

        try
        {
            BeginInvoke(() =>
            {
                txtLog.AppendText(line + Environment.NewLine);
                lblStatus.Text = "Status: " + line;
            });
        }
        catch (InvalidOperationException)
        {
        }
    }

    private void SetRunning(bool running)
    {
        btnRunNow.Enabled = !running;
        btnCancelRun.Enabled = running;
        if (!running)
            lblStatus.Text = "Status: Idle";
    }

    private void Form1_Load(object sender, EventArgs e)
    {
        _settings = _settingsStore.Load();
        BindSettingsToUi(_settings);
        LogInfo($"Loaded settings from {_settingsStore.SettingsPath}");

        if (_settings.AutoStartScheduler)
        {
            StartScheduler();
        }
    }

    private void btnSaveSettings_Click(object sender, EventArgs e)
    {
        var settings = BuildSettingsFromUi();
        _settingsStore.Save(settings);
        _settings = settings;
        LogInfo("Settings saved.");
    }

    private void btnBrowseCredentials_Click(object sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Select credentials file",
            Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
            CheckFileExists = true
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            txtCredentialsPath.Text = dialog.FileName;
        }
    }

    private void btnBrowseTokenPath_Click(object sender, EventArgs e)
    {
        using var dialog = new SaveFileDialog
        {
            Title = "Select token file location",
            Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
            FileName = Path.GetFileName(string.IsNullOrWhiteSpace(txtTokenPath.Text) ? "token.json" : txtTokenPath.Text)
        };

        var currentPath = txtTokenPath.Text;
        if (!string.IsNullOrWhiteSpace(currentPath))
        {
            var currentDirectory = Path.GetDirectoryName(currentPath);
            if (!string.IsNullOrWhiteSpace(currentDirectory) && Directory.Exists(currentDirectory))
            {
                dialog.InitialDirectory = currentDirectory;
            }
        }

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            txtTokenPath.Text = dialog.FileName;
        }
    }

    private void btnBrowseStorage_Click(object sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Select email storage folder",
            UseDescriptionForTitle = true
        };

        if (!string.IsNullOrWhiteSpace(txtStoragePath.Text) && Directory.Exists(txtStoragePath.Text))
        {
            dialog.InitialDirectory = txtStoragePath.Text;
        }

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            txtStoragePath.Text = dialog.SelectedPath;
        }
    }

    private async void btnRunNow_Click(object sender, EventArgs e)
    {
        await RunWithQuotaRecoveryAsync(CancellationToken.None, startedByScheduler: false);
    }

    private async Task RunWithQuotaRecoveryAsync(CancellationToken cancellationToken, bool startedByScheduler)
    {
        int attempt = 0;
        while (!cancellationToken.IsCancellationRequested)
        {
            var quotaHit = await ExecuteSingleRunAsync(cancellationToken, startedByScheduler);
            if (!quotaHit)
                return;

            var minutes = QuotaBackoffMinutes[Math.Min(attempt, QuotaBackoffMinutes.Length - 1)];
            attempt++;
            var delay = TimeSpan.FromMinutes(minutes);
            LogInfo($"Quota reached. Waiting {minutes} minute(s) before resuming automatically (resume at {DateTime.Now.Add(delay):yyyy-MM-dd HH:mm:ss}). Use Cancel Run to stop waiting.");

            var waitCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _quotaWaitCts = waitCts;
            SetRunning(true);
            try
            {
                await Task.Delay(delay, waitCts.Token);
            }
            catch (OperationCanceledException)
            {
                LogInfo("Quota wait cancelled.");
                return;
            }
            finally
            {
                _quotaWaitCts = null;
                waitCts.Dispose();
                SetRunning(false);
            }

            LogInfo("Quota wait finished. Resuming...");
        }
    }

    private void btnStartScheduler_Click(object sender, EventArgs e)
    {
        StartScheduler();
    }

    private void btnStopScheduler_Click(object sender, EventArgs e)
    {
        StopScheduler();
    }

    private DesktopAppSettings BuildSettingsFromUi()
    {
        return new DesktopAppSettings
        {
            CredentialsPath = txtCredentialsPath.Text.Trim(),
            TokenPath = txtTokenPath.Text.Trim(),
            EmailStoragePath = txtStoragePath.Text.Trim(),
            MetadataFileName = txtMetadataFileName.Text.Trim(),
            NaughtySendersFileName = txtNaughtyFileName.Text.Trim(),
            DryRunMode = chkDryRun.Checked,
            LoggingLevel = cmbLoggingLevel.SelectedItem?.ToString() ?? "Info",
            RepeatIntervalMinutes = (int)numRepeatIntervalMinutes.Value,
            AutoStartScheduler = chkAutoStart.Checked,
            ImportantDomains = _settings.ImportantDomains,
            UnsubscribeKeywords = _settings.UnsubscribeKeywords,
            BatchSize = _settings.BatchSize,
            MaxAttachmentSizeMB = _settings.MaxAttachmentSizeMB,
            RetryAttempts = _settings.RetryAttempts,
            RetryDelayMs = _settings.RetryDelayMs,
            MaxResultsPerPage = _settings.MaxResultsPerPage,
            RequestDelayMs = _settings.RequestDelayMs,
            MaxRequestsPerMinute = _settings.MaxRequestsPerMinute
        };
    }

    private void BindSettingsToUi(DesktopAppSettings settings)
    {
        txtCredentialsPath.Text = settings.CredentialsPath;
        txtTokenPath.Text = settings.TokenPath;
        txtStoragePath.Text = settings.EmailStoragePath;
        txtMetadataFileName.Text = settings.MetadataFileName;
        txtNaughtyFileName.Text = settings.NaughtySendersFileName;
        chkDryRun.Checked = settings.DryRunMode;

        var level = settings.LoggingLevel;
        if (string.IsNullOrWhiteSpace(level) || !cmbLoggingLevel.Items.Contains(level))
        {
            level = "Info";
        }

        cmbLoggingLevel.SelectedItem = level;
        numRepeatIntervalMinutes.Value = Math.Clamp(settings.RepeatIntervalMinutes, 1, 10080);
        chkAutoStart.Checked = settings.AutoStartScheduler;
    }

    private void LogInfo(string message)
    {
        txtLog.AppendText($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}");
    }

    private async Task<bool> ExecuteSingleRunAsync(CancellationToken cancellationToken, bool startedByScheduler)
    {
        if (_runCts != null)
        {
            LogInfo(startedByScheduler
                ? "Scheduled run skipped: a run is already in progress."
                : "A run is already in progress.");
            return false;
        }

        bool quotaHit = false;

        SetRunning(true);
        var runCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _runCts = runCts;

        try
        {
            var currentSettings = BuildSettingsFromUi();
            _settingsStore.Save(currentSettings);
            _settings = currentSettings;

            var logger = new ConsoleLogger(currentSettings.LoggingLevel, AppendLogLine, writeToConsole: false);
            var coreSettings = currentSettings.ToCoreSettings();

            LogInfo(startedByScheduler ? "Scheduled run started..." : "Run started...");
            var result = await Task.Run(() => _runner.RunOnceAsync(coreSettings, logger, runCts.Token));

            if (result.Success)
            {
                LogInfo(startedByScheduler ? "Scheduled run completed successfully." : "Run completed successfully.");
            }
            else if (result.QuotaExceeded)
            {
                quotaHit = true;
                LogInfo("Google API quota/rate limit reached. Run paused.");
            }
            else if (result.WasCancelled)
            {
                LogInfo(startedByScheduler ? "Scheduled run cancelled." : "Run cancelled.");
            }
            else
            {
                LogInfo(startedByScheduler ? $"Scheduled run failed: {result.Message}" : $"Run failed: {result.Message}");
            }
        }
        finally
        {
            _runCts = null;
            runCts.Dispose();
            SetRunning(false);
        }

        return quotaHit;
    }

    private void StartScheduler()
    {
        if (_schedulerCts != null)
        {
            LogInfo("Scheduler is already running.");
            return;
        }

        var currentSettings = BuildSettingsFromUi();
        _settingsStore.Save(currentSettings);
        _settings = currentSettings;

        _schedulerCts = new CancellationTokenSource();
        _schedulerTask = SchedulerLoopAsync(_schedulerCts.Token);

        btnStartScheduler.Enabled = false;
        btnStopScheduler.Enabled = true;
        LogInfo($"Scheduler started. Interval: {currentSettings.RepeatIntervalMinutes} minute(s).");
    }

    private void StopScheduler()
    {
        if (_schedulerCts == null)
        {
            return;
        }

        _schedulerCts.Cancel();
        _schedulerCts.Dispose();
        _schedulerCts = null;
        _schedulerTask = null;

        btnStartScheduler.Enabled = true;
        btnStopScheduler.Enabled = false;
        LogInfo("Scheduler stopped.");
    }

    private async Task SchedulerLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await RunWithQuotaRecoveryAsync(cancellationToken, startedByScheduler: true);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                LogInfo($"Scheduler execution error: {ex.Message}");
            }

            var intervalMinutes = Math.Max(1, BuildSettingsFromUi().RepeatIntervalMinutes);
            var delay = TimeSpan.FromMinutes(intervalMinutes);

            LogInfo($"Next run at {DateTime.Now.Add(delay):yyyy-MM-dd HH:mm:ss} (in {intervalMinutes} minute(s)).");

            try
            {
                await Task.Delay(delay, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private void Form1_FormClosing(object sender, FormClosingEventArgs e)
    {
        StopScheduler();
        _quotaWaitCts?.Cancel();

        if (_runCts != null)
        {
            _runCts.Cancel();
        }
    }
}
