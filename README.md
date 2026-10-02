# Email Manager - Gmail Download & Classification

A robust C# console application that downloads emails from Gmail, classifies them based on importance rules, and safely manages their deletion.

## Features

- ✅ Downloads all emails from Gmail to local storage (C:\Shared Storage\myEmails)
- ✅ Maintains robust CSV metadata with classification (important/not important)
- ✅ Automatic email classification based on customizable rules
- ✅ Deletes unimportant emails and emails from unwanted senders
- ✅ Safe dry-run mode (default) - no deletions without explicit configuration
- ✅ Resumable processing - handles interruptions safely
- ✅ Downloads attachments with size limits
- ✅ Stores emails in standard .EML format
- ✅ Graceful Ctrl+C handling

## Prerequisites

1. .NET 8.0 SDK or Runtime
2. Gmail API Credentials (OAuth 2.0 credentials.json)
3. Folder: `C:\Shared Storage\` (create if needed)

## Installation & Configuration

### Step 1: Create Google OAuth Credentials

1. Go to [Google Cloud Console](https://console.cloud.google.com/)
2. Create a new project (or use existing one)
3. Enable the Gmail API
4. Create OAuth 2.0 Desktop Application credentials
5. Download the credentials JSON file
6. Place it in the project root directory as `credentials.json`

**IMPORTANT**: Do NOT commit `credentials.json` to Git. It is already in `.gitignore`.

### Step 2: Configure the Application

1. Copy `appsettings.example.json` to `appsettings.json`:
   ```bash
   copy appsettings.example.json appsettings.json
   ```

2. Edit `appsettings.json` and customize:
   - `EmailStoragePath`: Where to store emails (default: C:\Shared Storage\myEmails)
   - `DryRunMode`: true (default - safe) or false (enable actual deletions)
   - `LoggingLevel`: Info, Debug, Warning, Error
   - `MaxAttachmentSizeMB`: Max size for each attachment download

Example configuration:
```json
{
  "GmailConfiguration": {
	"CredentialsPath": "credentials.json",
	"TokenPath": "token.json"
  },
  "ApplicationConfiguration": {
	"EmailStoragePath": "C:\\Shared Storage\\myEmails",
	"DryRunMode": true,
	"LoggingLevel": "Info"
  },
  "ClassificationRules": {
	"ImportantDomains": ["dawood", "wajeeha"],
	"UnsubscribeKeywords": ["unsubscribe", "un-subscribe"]
  }
}
```

**IMPORTANT**: Do NOT commit `appsettings.json` to Git. It is already in `.gitignore`.

### Step 3: Create Naughty Senders List

On first run, the application creates `C:\Shared Storage\myEmails\naughty_senders.csv` with a template.

Edit it to add senders you want to auto-delete:
```csv
Sender,Enabled,Notes
spam@example.com,true,Spam sender
@badomain.com,true,Block entire domain
newsmarketing@company.com,false,Toggle to true to enable
```

## Running the Application

### Run in Safe Dry-Run Mode (Default)

```bash
dotnet run
```

This will:
- Log all actions that would be taken
- Download emails
- Classify them
- Show what WOULD be deleted
- NOT actually delete anything

### Enable Actual Deletion

Edit `appsettings.json` and set:
```json
"DryRunMode": false
```

Then run:
```bash
dotnet run
```

## CSV Formats

### emails.csv (Metadata)

Located at: `C:\Shared Storage\myEmails\emails.csv`

Columns:
- `MessageId`: Gmail unique message ID
- `ThreadId`: Gmail thread ID
- `Subject`: Email subject
- `Sender`: Sender email address
- `Recipients`: Comma-separated recipients
- `ReceivedDate`: Email timestamp
- `HasAttachment`: true/false
- `Important`: true/false (based on classification)
- `Processed`: true/false (processed during this run)
- `LocalPath`: Local file path to stored email

Example:
```csv
"1234567890abcdef","thread-id-123","Meeting tomorrow","alice@example.com","bob@example.com","2024-01-15T10:30:00","true","true","true","/myEmails/1234567890abcdef/message.eml"
```

### naughty_senders.csv (Blocklist)

Located at: `C:\Shared Storage\myEmails\naughty_senders.csv`

Columns:
- `Sender`: Email address or @domain.com
- `Enabled`: true/false to enable/disable the rule
- `Notes`: Optional notes about why this sender is blocked

Example:
```csv
"Sender","Enabled","Notes"
"spam@example.com","true","Known spam sender"
"@adnetwork.com","true","Block entire advertising domain"
```

## Classification Rules (Priority Order)

Emails are classified as follows (in order):

1. **Naughty Sender Check**: If sender matches the naughty senders list → Will be deleted
2. **Unsubscribe Check**: If email contains "unsubscribe" or "un-subscribe" → NOT important (overrides attachment rule)
3. **Importance Rules**:
   - Sender contains "dawood" or "wajeeha" (case-insensitive) → Important
   - OR email has attachments → Important
4. **Default**: If none of above → Not important

### Example Classifications

| Condition | Result | Action |
|-----------|--------|--------|
| From dawood@company.com, no attachment | Important | Kept |
| From dawood@company.com, has attachment | Important | Kept |
| From random@example.com, has PDF attachment | Important | Kept |
| Has unsubscribe link, but has attachment | NOT Important | Deleted (dry-run logs) |
| From newsletter@badomain.com (in naughty list) | Deleted | Always deleted regardless of other rules |
| Random email, no attachment, no keywords | NOT Important | Deleted (dry-run logs) |

## Storage Structure

After running, your email storage will look like:

```
C:\Shared Storage\
└── myEmails\
	├── emails.csv                              (metadata file)
	├── naughty_senders.csv                     (blocklist)
	├── gmail_message_id_1\
	│   ├── message.eml                         (email in MIME format)
	│   ├── document.pdf                        (attachment)
	│   └── image.jpg                           (attachment)
	├── gmail_message_id_2\
	│   └── message.eml
	└── ...
```

## Logging

Console output shows:

- `[INFO]`: Important milestones (authentication, emails found, etc.)
- `[WARN]`: Warnings (attachment too large, processing errors)
- `[ERROR]`: Errors preventing processing
- `[DEBUG]`: Detailed operation logs (only if LoggingLevel=Debug)
- `[OK]`: Successful completion

Example output:
```
[INFO]  ╔════════════════════════════════════════════════════════╗
[INFO]  ║    Email Manager - Gmail Download & Classification    ║
[INFO]  ╚════════════════════════════════════════════════════════╝
[INFO]  Step 1/4: Loading existing metadata...
[INFO]  Loaded 250 email records from metadata file
[INFO]  Step 2/4: Authenticating with Gmail...
[INFO]  ✓ Gmail authentication successful
[INFO]  Step 3/4: Fetching and downloading emails...
[INFO]  Total messages in mailbox: 2850 (across 6 pages)
[INFO]  Found 12 new emails, downloaded 12
[INFO]  Step 4/4: Processing unprocessed emails...
[INFO]  [DRY-RUN] Would delete: spam@domain.com | Newsletter | Reason: Not important
[INFO]  Processed 12 unprocessed emails
[OK]    ✓ Processing completed successfully!
```

## Running Tests

```bash
cd myGmail.Tests
dotnet test
```

Tests cover:
- Email classification rules (dawood, wajeeha, attachments, unsubscribe)
- CSV reading/writing with special character escaping
- Naughty sender matching (exact and domain matching)
- Dry-run mode (no actual Gmail API calls)
- Duplicate prevention (MessageId deduplication)

## Error Handling

The application handles:
- Missing credentials.json → Clear error message
- Network failures → Logging with error details
- Partial failures → Continues processing other emails
- Application interruption (Ctrl+C) → Graceful shutdown, no data corruption
- CSV corruption → Atomic writes prevent file corruption

## Security Notes

### What's Protected

✅ Credentials are NOT committed to Git (.gitignore)
✅ OAuth tokens stored locally, not in source control
✅ No credentials logged to console
✅ Sensitive config values not exposed in logs
✅ Safe atomic CSV writes prevent corruption

### What You Must Do

1. **Never commit credentials.json** - It's in .gitignore
2. **Never commit appsettings.json with real settings** - Use appsettings.example.json as template
3. **Rotate credentials if leaked** - If credentials.json appears in Git history, consider it compromised
4. **Keep token.json folder secure** - Contains active OAuth tokens
5. **Change important keywords if needed** - Edit ClassificationRules in appsettings.json

### Files in .gitignore

```
credentials.json     # OAuth credentials (NEVER commit)
token.json          # OAuth runtime tokens (NEVER commit)
token/              # Token storage folder
appsettings.json    # Local configuration with paths
.env                # Environment variables (if used)
bin/
obj/
```

## FAQ

### Q: What happens if the app crashes mid-run?

A: The application is designed to resume safely:
- Metadata CSV uses atomic writes (temp file + move)
- Already processed emails won't be re-downloaded
- Crashed operations won't be marked as complete
- Simply restart the application

### Q: Can I run this multiple times a day?

A: Yes! The application:
- Only downloads emails not in metadata.csv
- Re-processes unprocessed emails
- Won't duplicate work
- Perfect for scheduled runs

### Q: How do I whitelist important senders?

A: Add them to `ClassificationRules.ImportantDomains` in appsettings.json:
```json
"ImportantDomains": ["dawood", "wajeeha", "my.boss@company.com"]
```

### Q: Can I change the download location?

A: Yes, edit appsettings.json:
```json
"EmailStoragePath": "D:\\MyEmails\\Archive"
```

### Q: How large can attachments be?

A: Configurable in appsettings.json:
```json
"MaxAttachmentSizeMB": 100
```

Default is 50 MB per attachment.

### Q: How do I delete the naughty_senders.csv?

A: Simply delete the file. On next run, the application will create a fresh template with disabled example entries.

## Troubleshooting

### Error: "Credentials file not found"

**Solution**: Download credentials.json from Google Cloud Console and place it in the project directory.

### Error: "Invalid JSON in appsettings.json"

**Solution**: Copy appsettings.example.json to appsettings.json and ensure valid JSON syntax.

### OAuth Authentication Hangs

**Solution**: Close any existing browser windows and try again. First authentication opens a browser for consent.

### Very Slow Download

**Solution**: This is normal for large mailboxes (10K+ emails). The application processes 500 messages per page. It will complete.

### No Emails Downloaded

**Solution**: 
1. Check LoggingLevel=Debug in appsettings.json
2. Verify authentication succeeded
3. Run in dry-run mode first to see what's happening

## Development

### Project Structure

```
myGmail/
├── Program.cs                          # Entry point
├── Configuration/
│   └── AppSettings.cs                  # Configuration classes
├── Models/
│   └── EmailMetadata.cs                # Data models
├── Services/
│   ├── ConsoleLogger.cs                # Logging
│   ├── GmailServiceWrapper.cs          # Gmail API
│   ├── EmailProcessor.cs               # Main orchestrator
│   ├── EmailClassifier.cs              # Classification logic
│   ├── EmailStorage.cs                 # Local storage
│   ├── MetadataRepository.cs           # CSV persistence
│   └── NaughtySenderRepository.cs      # Blocklist management
└── appsettings.example.json

myGmail.Tests/
├── EmailClassifierTests.cs             # Classification tests
└── MetadataRepositoryTests.cs          # CSV/storage tests
```

### Adding Custom Classification Rules

Edit `Services/EmailClassifier.cs` to add new rules. The classification method clearly documents precedence.

### Modifying Metadata Columns

Edit `Models/EmailMetadata.cs` to add/remove fields. CsvHelper will automatically handle new columns.

## License

This application is provided as-is for personal email management.

## Support

For issues, check:
1. appsettings.json is correctly formatted
2. credentials.json exists and is valid
3. C:\Shared Storage\ directory exists
4. Gmail API is enabled in Google Cloud Console
5. Run with LoggingLevel=Debug for verbose output

---

**Last Updated**: January 2025
**Version**: 1.0
**Target Framework**: .NET 8.0
