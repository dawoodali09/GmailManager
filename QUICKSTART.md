# Quick Start Guide: Gmail Email Manager

## 5-Minute Setup

### 1. Get Google Credentials

1. Visit [Google Cloud Console](https://console.cloud.google.com/)
2. Create new project → Enable Gmail API
3. Create OAuth 2.0 credentials (Desktop app)
4. Download JSON file
5. Rename to `credentials.json` and place in project folder

### 2. Run Configuration

```bash
# Copy template to local config
copy appsettings.example.json appsettings.json

# Edit appsettings.json with your settings
# Default is DryRun: true (safe mode - no deletions)
```

### 3. First Run (Dry-Run/Safe Mode)

```bash
cd myGmail
dotnet build
dotnet run
```

**Output**: Shows what emails would be downloaded/deleted, but doesn't actually delete anything.

### 4. Configure Naughty Senders (Optional)

After first run, edit: `C:\Shared Storage\myEmails\naughty_senders.csv`

```csv
Sender,Enabled,Notes
spam@example.com,true,Known spam
@badomain.com,true,Block all from domain
```

### 5. Enable Actual Deletion (When Ready)

Edit `appsettings.json`:
```json
"DryRunMode": false
```

Then run again:
```bash
dotnet run
```

## What It Does

✅ Downloads all emails from Gmail
✅ Saves them locally as .EML files  
✅ Maintains metadata CSV with classification
✅ Classifies emails as important/not important
✅ Deletes unimportant emails (or logs in dry-run)

## Classification Rules

Emails are **Important** if:
- Sender contains "dawood" OR "wajeeha" (case-insensitive)
- OR email has attachments
- BUT: Emails with "unsubscribe" text are always NOT important

Emails are **Deleted** if:
- Sender is in naughty_senders.csv (blocklist)
- OR email is NOT important
- (Unless DryRunMode=true, then only logged)

## Storage Location

All emails saved to: `C:\Shared Storage\myEmails\`

Structure:
```
myEmails/
├── emails.csv              (metadata file)
├── naughty_senders.csv     (your blocklist)
├── message-id-1/
│   ├── message.eml         (email in standard format)
│   ├── attachment1.pdf     (downloaded attachments)
│   └── attachment2.jpg
└── message-id-2/
	└── message.eml
```

## Running Tests

```bash
cd myGmail.Tests
dotnet test
```

Expected: ✅ **All 17 tests pass**

Test coverage:
- Classification rules (dawood, wajeeha, attachments, unsubscribe)
- CSV reading/writing with escaped special characters
- Naughty sender matching (exact + domain)
- Metadata deduplication

## Common Commands

```bash
# Build only
dotnet build

# Run (dry-run mode by default)
dotnet run

# Run with debug logging (verbose)
# Edit appsettings.json: "LoggingLevel": "Debug"
# Then: dotnet run

# Run tests
dotnet test

# Run specific test
dotnet test --filter "EmailClassifier"
```

## Key Files

| File | Purpose | Git |
|------|---------|-----|
| `Program.cs` | Entry point | ✅ Commit |
| `Configuration/AppSettings.cs` | Config models | ✅ Commit |
| `Models/EmailMetadata.cs` | Domain models | ✅ Commit |
| `Services/*.cs` | Business logic | ✅ Commit |
| `.gitignore` | Security rules | ✅ Commit |
| `appsettings.example.json` | Template config | ✅ Commit |
| `appsettings.json` | Local config | ❌ Git-ignored |
| `credentials.json` | OAuth secrets | ❌ Git-ignored |
| `token.json` | Runtime tokens | ❌ Git-ignored |
| `README.md` | Full documentation | ✅ Commit |

## Safety Features

🔐 **Dry-Run Mode**: Default is safe (no deletions)
🔐 **Atomic CSV Writes**: Prevents corruption
🔐 **No Secret Logging**: Credentials never logged
🔐 **Safe Interruption**: Can Ctrl+C without data loss
🔐 **Deduplication**: Won't re-download emails

## Troubleshooting

**Q: Build fails**
→ Ensure .NET 8.0 SDK installed: `dotnet --version`

**Q: "Credentials file not found"**
→ Download from Google Cloud Console, name it `credentials.json`

**Q: "Invalid JSON"**
→ Copy `appsettings.example.json` to `appsettings.json`, verify JSON syntax

**Q: No emails downloaded**
→ Check Gmail API is enabled in Google Cloud Console

**Q: Tests fail**
→ Likely issue with test setup. Ensure temp folders are writable.

## Next Steps

1. ✅ Download credentials.json from Google Cloud Console
2. ✅ Create appsettings.json from template
3. ✅ Run in dry-run mode and review output
4. ✅ Configure naughty_senders.csv (optional)
5. ✅ Enable actual deletion in appsettings.json
6. ✅ Run full process

---

**Latest Build Status**: ✅ All code compiles, all 17 tests pass
**Target Framework**: .NET 8.0
**Ready to Use**: Yes
