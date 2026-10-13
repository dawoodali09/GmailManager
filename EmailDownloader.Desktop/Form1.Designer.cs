namespace EmailDownloader.Desktop;

partial class Form1
{
    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    ///  Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    ///  Required method for Designer support - do not modify
    ///  the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        tableLayoutPanelMain = new TableLayoutPanel();
        tableLayoutPanelSettings = new TableLayoutPanel();
        labelCredentials = new Label();
        txtCredentialsPath = new TextBox();
        btnBrowseCredentials = new Button();
        labelTokenPath = new Label();
        txtTokenPath = new TextBox();
        btnBrowseTokenPath = new Button();
        labelStoragePath = new Label();
        txtStoragePath = new TextBox();
        btnBrowseStorage = new Button();
        labelMetadataFile = new Label();
        txtMetadataFileName = new TextBox();
        labelNaughtyFile = new Label();
        txtNaughtyFileName = new TextBox();
        chkDryRun = new CheckBox();
        labelLoggingLevel = new Label();
        cmbLoggingLevel = new ComboBox();
        labelRepeatInterval = new Label();
        numRepeatIntervalMinutes = new NumericUpDown();
        chkAutoStart = new CheckBox();
        flowLayoutPanelActions = new FlowLayoutPanel();
        btnSaveSettings = new Button();
        btnRunNow = new Button();
        btnStartScheduler = new Button();
        btnStopScheduler = new Button();
        txtLog = new TextBox();
        tableLayoutPanelMain.SuspendLayout();
        tableLayoutPanelSettings.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)numRepeatIntervalMinutes).BeginInit();
        flowLayoutPanelActions.SuspendLayout();
        SuspendLayout();
        // 
        // tableLayoutPanelMain
        // 
        tableLayoutPanelMain.ColumnCount = 1;
        tableLayoutPanelMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        tableLayoutPanelMain.Controls.Add(tableLayoutPanelSettings, 0, 0);
        tableLayoutPanelMain.Controls.Add(flowLayoutPanelActions, 0, 1);
        tableLayoutPanelMain.Controls.Add(txtLog, 0, 2);
        tableLayoutPanelMain.Dock = DockStyle.Fill;
        tableLayoutPanelMain.Location = new Point(0, 0);
        tableLayoutPanelMain.Name = "tableLayoutPanelMain";
        tableLayoutPanelMain.Padding = new Padding(10);
        tableLayoutPanelMain.RowCount = 3;
        tableLayoutPanelMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 260F));
        tableLayoutPanelMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
        tableLayoutPanelMain.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        tableLayoutPanelMain.Size = new Size(1100, 700);
        tableLayoutPanelMain.TabIndex = 0;
        // 
        // tableLayoutPanelSettings
        // 
        tableLayoutPanelSettings.ColumnCount = 3;
        tableLayoutPanelSettings.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 230F));
        tableLayoutPanelSettings.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        tableLayoutPanelSettings.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130F));
        tableLayoutPanelSettings.Controls.Add(labelCredentials, 0, 0);
        tableLayoutPanelSettings.Controls.Add(txtCredentialsPath, 1, 0);
        tableLayoutPanelSettings.Controls.Add(btnBrowseCredentials, 2, 0);
        tableLayoutPanelSettings.Controls.Add(labelTokenPath, 0, 1);
        tableLayoutPanelSettings.Controls.Add(txtTokenPath, 1, 1);
        tableLayoutPanelSettings.Controls.Add(btnBrowseTokenPath, 2, 1);
        tableLayoutPanelSettings.Controls.Add(labelStoragePath, 0, 2);
        tableLayoutPanelSettings.Controls.Add(txtStoragePath, 1, 2);
        tableLayoutPanelSettings.Controls.Add(btnBrowseStorage, 2, 2);
        tableLayoutPanelSettings.Controls.Add(labelMetadataFile, 0, 3);
        tableLayoutPanelSettings.Controls.Add(txtMetadataFileName, 1, 3);
        tableLayoutPanelSettings.Controls.Add(labelNaughtyFile, 0, 4);
        tableLayoutPanelSettings.Controls.Add(txtNaughtyFileName, 1, 4);
        tableLayoutPanelSettings.Controls.Add(chkDryRun, 0, 5);
        tableLayoutPanelSettings.Controls.Add(labelLoggingLevel, 0, 6);
        tableLayoutPanelSettings.Controls.Add(cmbLoggingLevel, 1, 6);
        tableLayoutPanelSettings.Controls.Add(labelRepeatInterval, 0, 7);
        tableLayoutPanelSettings.Controls.Add(numRepeatIntervalMinutes, 1, 7);
        tableLayoutPanelSettings.Controls.Add(chkAutoStart, 0, 8);
        tableLayoutPanelSettings.Dock = DockStyle.Fill;
        tableLayoutPanelSettings.Location = new Point(13, 13);
        tableLayoutPanelSettings.Name = "tableLayoutPanelSettings";
        tableLayoutPanelSettings.RowCount = 9;
        tableLayoutPanelSettings.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
        tableLayoutPanelSettings.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
        tableLayoutPanelSettings.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
        tableLayoutPanelSettings.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
        tableLayoutPanelSettings.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
        tableLayoutPanelSettings.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
        tableLayoutPanelSettings.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
        tableLayoutPanelSettings.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
        tableLayoutPanelSettings.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
        tableLayoutPanelSettings.Size = new Size(1074, 254);
        tableLayoutPanelSettings.TabIndex = 0;
        // 
        // labelCredentials
        // 
        labelCredentials.Anchor = AnchorStyles.Left;
        labelCredentials.AutoSize = true;
        labelCredentials.Location = new Point(3, 6);
        labelCredentials.Name = "labelCredentials";
        labelCredentials.Size = new Size(139, 15);
        labelCredentials.TabIndex = 0;
        labelCredentials.Text = "Credentials file location";
        // 
        // txtCredentialsPath
        // 
        txtCredentialsPath.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        txtCredentialsPath.Location = new Point(233, 3);
        txtCredentialsPath.Name = "txtCredentialsPath";
        txtCredentialsPath.Size = new Size(708, 23);
        txtCredentialsPath.TabIndex = 1;
        // 
        // btnBrowseCredentials
        // 
        btnBrowseCredentials.Location = new Point(947, 3);
        btnBrowseCredentials.Name = "btnBrowseCredentials";
        btnBrowseCredentials.Size = new Size(93, 22);
        btnBrowseCredentials.TabIndex = 2;
        btnBrowseCredentials.Text = "Browse...";
        btnBrowseCredentials.UseVisualStyleBackColor = true;
        btnBrowseCredentials.Click += btnBrowseCredentials_Click;
        // 
        // labelTokenPath
        // 
        labelTokenPath.Anchor = AnchorStyles.Left;
        labelTokenPath.AutoSize = true;
        labelTokenPath.Location = new Point(3, 34);
        labelTokenPath.Name = "labelTokenPath";
        labelTokenPath.Size = new Size(105, 15);
        labelTokenPath.TabIndex = 3;
        labelTokenPath.Text = "Token file location";
        // 
        // txtTokenPath
        // 
        txtTokenPath.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        txtTokenPath.Location = new Point(233, 31);
        txtTokenPath.Name = "txtTokenPath";
        txtTokenPath.Size = new Size(708, 23);
        txtTokenPath.TabIndex = 4;
        // 
        // btnBrowseTokenPath
        // 
        btnBrowseTokenPath.Location = new Point(947, 31);
        btnBrowseTokenPath.Name = "btnBrowseTokenPath";
        btnBrowseTokenPath.Size = new Size(93, 22);
        btnBrowseTokenPath.TabIndex = 5;
        btnBrowseTokenPath.Text = "Browse...";
        btnBrowseTokenPath.UseVisualStyleBackColor = true;
        btnBrowseTokenPath.Click += btnBrowseTokenPath_Click;
        // 
        // labelStoragePath
        // 
        labelStoragePath.Anchor = AnchorStyles.Left;
        labelStoragePath.AutoSize = true;
        labelStoragePath.Location = new Point(3, 62);
        labelStoragePath.Name = "labelStoragePath";
        labelStoragePath.Size = new Size(142, 15);
        labelStoragePath.TabIndex = 6;
        labelStoragePath.Text = "Email storage folder path";
        // 
        // txtStoragePath
        // 
        txtStoragePath.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        txtStoragePath.Location = new Point(233, 59);
        txtStoragePath.Name = "txtStoragePath";
        txtStoragePath.Size = new Size(708, 23);
        txtStoragePath.TabIndex = 7;
        // 
        // btnBrowseStorage
        // 
        btnBrowseStorage.Location = new Point(947, 59);
        btnBrowseStorage.Name = "btnBrowseStorage";
        btnBrowseStorage.Size = new Size(93, 22);
        btnBrowseStorage.TabIndex = 8;
        btnBrowseStorage.Text = "Browse...";
        btnBrowseStorage.UseVisualStyleBackColor = true;
        btnBrowseStorage.Click += btnBrowseStorage_Click;
        // 
        // labelMetadataFile
        // 
        labelMetadataFile.Anchor = AnchorStyles.Left;
        labelMetadataFile.AutoSize = true;
        labelMetadataFile.Location = new Point(3, 90);
        labelMetadataFile.Name = "labelMetadataFile";
        labelMetadataFile.Size = new Size(95, 15);
        labelMetadataFile.TabIndex = 9;
        labelMetadataFile.Text = "Metadata file (.csv)";
        // 
        // txtMetadataFileName
        // 
        txtMetadataFileName.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        txtMetadataFileName.Location = new Point(233, 87);
        txtMetadataFileName.Name = "txtMetadataFileName";
        txtMetadataFileName.Size = new Size(708, 23);
        txtMetadataFileName.TabIndex = 10;
        // 
        // labelNaughtyFile
        // 
        labelNaughtyFile.Anchor = AnchorStyles.Left;
        labelNaughtyFile.AutoSize = true;
        labelNaughtyFile.Location = new Point(3, 118);
        labelNaughtyFile.Name = "labelNaughtyFile";
        labelNaughtyFile.Size = new Size(130, 15);
        labelNaughtyFile.TabIndex = 11;
        labelNaughtyFile.Text = "Naughty sender file (.csv)";
        // 
        // txtNaughtyFileName
        // 
        txtNaughtyFileName.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        txtNaughtyFileName.Location = new Point(233, 115);
        txtNaughtyFileName.Name = "txtNaughtyFileName";
        txtNaughtyFileName.Size = new Size(708, 23);
        txtNaughtyFileName.TabIndex = 12;
        // 
        // chkDryRun
        // 
        chkDryRun.AutoSize = true;
        chkDryRun.Location = new Point(3, 143);
        chkDryRun.Name = "chkDryRun";
        chkDryRun.Size = new Size(92, 19);
        chkDryRun.TabIndex = 13;
        chkDryRun.Text = "Dry run mode";
        chkDryRun.UseVisualStyleBackColor = true;
        // 
        // labelLoggingLevel
        // 
        labelLoggingLevel.Anchor = AnchorStyles.Left;
        labelLoggingLevel.AutoSize = true;
        labelLoggingLevel.Location = new Point(3, 174);
        labelLoggingLevel.Name = "labelLoggingLevel";
        labelLoggingLevel.Size = new Size(76, 15);
        labelLoggingLevel.TabIndex = 14;
        labelLoggingLevel.Text = "Logging level";
        // 
        // cmbLoggingLevel
        // 
        cmbLoggingLevel.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbLoggingLevel.FormattingEnabled = true;
        cmbLoggingLevel.Items.AddRange(new object[] { "Debug", "Info", "Warning", "Error" });
        cmbLoggingLevel.Location = new Point(233, 171);
        cmbLoggingLevel.Name = "cmbLoggingLevel";
        cmbLoggingLevel.Size = new Size(190, 23);
        cmbLoggingLevel.TabIndex = 15;
        // 
        // labelRepeatInterval
        // 
        labelRepeatInterval.Anchor = AnchorStyles.Left;
        labelRepeatInterval.AutoSize = true;
        labelRepeatInterval.Location = new Point(3, 202);
        labelRepeatInterval.Name = "labelRepeatInterval";
        labelRepeatInterval.Size = new Size(151, 15);
        labelRepeatInterval.TabIndex = 16;
        labelRepeatInterval.Text = "Repeat interval (in minutes)";
        // 
        // numRepeatIntervalMinutes
        // 
        numRepeatIntervalMinutes.Location = new Point(233, 199);
        numRepeatIntervalMinutes.Maximum = new decimal(new int[] { 10080, 0, 0, 0 });
        numRepeatIntervalMinutes.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
        numRepeatIntervalMinutes.Name = "numRepeatIntervalMinutes";
        numRepeatIntervalMinutes.Size = new Size(120, 23);
        numRepeatIntervalMinutes.TabIndex = 17;
        numRepeatIntervalMinutes.Value = new decimal(new int[] { 60, 0, 0, 0 });
        // 
        // chkAutoStart
        // 
        chkAutoStart.AutoSize = true;
        chkAutoStart.Location = new Point(3, 227);
        chkAutoStart.Name = "chkAutoStart";
        chkAutoStart.Size = new Size(181, 19);
        chkAutoStart.TabIndex = 18;
        chkAutoStart.Text = "Auto-start scheduler on launch";
        chkAutoStart.UseVisualStyleBackColor = true;
        // 
        // flowLayoutPanelActions
        // 
        flowLayoutPanelActions.Controls.Add(btnSaveSettings);
        flowLayoutPanelActions.Controls.Add(btnRunNow);
        flowLayoutPanelActions.Controls.Add(btnStartScheduler);
        flowLayoutPanelActions.Controls.Add(btnStopScheduler);
        flowLayoutPanelActions.Dock = DockStyle.Fill;
        flowLayoutPanelActions.Location = new Point(13, 273);
        flowLayoutPanelActions.Name = "flowLayoutPanelActions";
        flowLayoutPanelActions.Size = new Size(1074, 42);
        flowLayoutPanelActions.TabIndex = 1;
        // 
        // btnSaveSettings
        // 
        btnSaveSettings.Location = new Point(3, 3);
        btnSaveSettings.Name = "btnSaveSettings";
        btnSaveSettings.Size = new Size(95, 30);
        btnSaveSettings.TabIndex = 0;
        btnSaveSettings.Text = "Save Settings";
        btnSaveSettings.UseVisualStyleBackColor = true;
        btnSaveSettings.Click += btnSaveSettings_Click;
        // 
        // btnRunNow
        // 
        btnRunNow.Location = new Point(104, 3);
        btnRunNow.Name = "btnRunNow";
        btnRunNow.Size = new Size(95, 30);
        btnRunNow.TabIndex = 1;
        btnRunNow.Text = "Run Now";
        btnRunNow.UseVisualStyleBackColor = true;
        btnRunNow.Click += btnRunNow_Click;
        // 
        // btnStartScheduler
        // 
        btnStartScheduler.Location = new Point(205, 3);
        btnStartScheduler.Name = "btnStartScheduler";
        btnStartScheduler.Size = new Size(110, 30);
        btnStartScheduler.TabIndex = 2;
        btnStartScheduler.Text = "Start Scheduler";
        btnStartScheduler.UseVisualStyleBackColor = true;
        btnStartScheduler.Click += btnStartScheduler_Click;
        // 
        // btnStopScheduler
        // 
        btnStopScheduler.Enabled = false;
        btnStopScheduler.Location = new Point(321, 3);
        btnStopScheduler.Name = "btnStopScheduler";
        btnStopScheduler.Size = new Size(110, 30);
        btnStopScheduler.TabIndex = 3;
        btnStopScheduler.Text = "Stop Scheduler";
        btnStopScheduler.UseVisualStyleBackColor = true;
        btnStopScheduler.Click += btnStopScheduler_Click;
        // 
        // txtLog
        // 
        txtLog.Dock = DockStyle.Fill;
        txtLog.Location = new Point(13, 321);
        txtLog.Multiline = true;
        txtLog.Name = "txtLog";
        txtLog.ReadOnly = true;
        txtLog.ScrollBars = ScrollBars.Both;
        txtLog.Size = new Size(1074, 366);
        txtLog.TabIndex = 2;
        // 
        // Form1
        // 
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1100, 700);
        Controls.Add(tableLayoutPanelMain);
        Name = "Form1";
        Text = "Email Manager Desktop";
        Load += Form1_Load;
        FormClosing += Form1_FormClosing;
        tableLayoutPanelMain.ResumeLayout(false);
        tableLayoutPanelMain.PerformLayout();
        tableLayoutPanelSettings.ResumeLayout(false);
        tableLayoutPanelSettings.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)numRepeatIntervalMinutes).EndInit();
        flowLayoutPanelActions.ResumeLayout(false);
        ResumeLayout(false);
    }

    #endregion

    private TableLayoutPanel tableLayoutPanelMain;
    private TableLayoutPanel tableLayoutPanelSettings;
    private Label labelCredentials;
    private TextBox txtCredentialsPath;
    private Button btnBrowseCredentials;
    private Label labelTokenPath;
    private TextBox txtTokenPath;
    private Button btnBrowseTokenPath;
    private Label labelStoragePath;
    private TextBox txtStoragePath;
    private Button btnBrowseStorage;
    private Label labelMetadataFile;
    private TextBox txtMetadataFileName;
    private Label labelNaughtyFile;
    private TextBox txtNaughtyFileName;
    private CheckBox chkDryRun;
    private Label labelLoggingLevel;
    private ComboBox cmbLoggingLevel;
    private Label labelRepeatInterval;
    private NumericUpDown numRepeatIntervalMinutes;
    private CheckBox chkAutoStart;
    private FlowLayoutPanel flowLayoutPanelActions;
    private Button btnSaveSettings;
    private Button btnRunNow;
    private Button btnStartScheduler;
    private Button btnStopScheduler;
    private TextBox txtLog;
}
