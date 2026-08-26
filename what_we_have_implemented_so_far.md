# What We Have Implemented So Far 🚀

This document summarizes the configurations, fixes, and architectural improvements applied to **ThriftHub** to support seamless hosting on Render and resolve cross-platform mobile and email service bugs.

---

## 🔑 1. GitHub Integration & Push Details

### Repository Information
*   **Repository URL**: `https://github.com/unrulykentt/ThriftHub`
*   **Git Branch**: `main`
*   **Authentication**: Use a GitHub Personal Access Token stored in your environment or credential manager — never commit tokens to the repository.

### How the Git Push was Executed
Since Git was installed locally on the Windows host at `C:\Program Files\Git\cmd\git.exe` but was not part of the system's global `PATH` variable, the environment was configured dynamically in PowerShell:

```powershell
# 1. Temporarily append Git command directory to environment PATH
$env:PATH += ";C:\Program Files\Git\cmd"

# 2. Stage and commit all changes
git add .
git commit -m "Commit description details"

# 3. Push to GitHub (use your configured credentials or credential manager)
git push origin main
```

---

## 🐳 2. Render Deployment & Containerization

*   **Dockerfile Configuration**: Added a production-ready [Dockerfile](Dockerfile) utilizing a multi-stage build (.NET 9.0 SDK to build and publish; .NET 9.0 ASP.NET Runtime for the final slim image).
*   **Port Binding**: Configured the container to bind to port `8080` (via `ASPNETCORE_HTTP_PORTS`), matching Render's automatic routing.
*   **Inotify Crash Fix**: Configured environment variables in the container to bypass Linux file watcher limits that cause `System.IO.IOException` crashes on Render:
    *   `ASPNETCORE_hostBuilder__reloadConfigOnChange=false`
    *   `DOTNET_USE_POLLING_FILE_WATCHER=true`

---

## 💾 3. Database & Startup Persistence

*   **Automatic Start Migration**: Updated [Program.cs](ThriftHub/Program.cs) to automatically apply Entity Framework migrations on application startup using `context.Database.Migrate()`.
*   **Bypassed Non-Transactional Exceptions**: Configured the SQLite provider warnings to ignore `NonTransactionalMigrationOperationWarning`. This prevents database migration initialization crashes when SQLite attempts to recreate tables with foreign key constraints outside a transaction block (such as during the `FixFavoritesChanges` migration).
*   **Login Persistence**: Explicitly configured [.gitignore](.gitignore) to allow tracking `ThriftHub/thriftHub.db` while ignoring temporary WAL database logs. The database is copied during the Docker build directly into the container as `/app/thrifthub.db` (lowercase) so that all local accounts, products, and passwords function on the live site out of the box.

---

## 🖼️ 4. Static Images Case-Sensitivity Fix

*   **Linux Mismatch**: Windows has a case-insensitive filesystem, but Render's Linux environment is case-sensitive. The database saved image paths as lowercase `/uploads/...`, while the Git directories were committed as uppercase `/Uploads/...`, leading to `404 Not Found` image errors on the live site.
*   **Resolution**: Renamed the directories inside Git history to lowercase using `git mv`:
    *   `ThriftHub/wwwroot/Uploads` ➡️ `ThriftHub/wwwroot/uploads`
    *   `ThriftHub/wwwroot/uploads/Products` ➡️ `ThriftHub/wwwroot/uploads/products`
    *   `ThriftHub/wwwroot/uploads/Verification` ➡️ `ThriftHub/wwwroot/uploads/verification`
*   **Code Update**: Modified [VerificaionController.cs](ThriftHub/Controllers/VerificaionController.cs) path joins to reference lowercase `"uploads"` and `"verification"`.

---

## 🎙️ 5. Cross-Platform Voice Notes Compatibility

*   **Dynamic MIME Type Checking**: Updated [Chat.cshtml](ThriftHub/Views/Messages/Chat.cshtml) to check for supported audio formats dynamically. Added `audio/mp4` and `audio/aac` to support mobile Safari (iOS) alongside desktop formats (`audio/webm;codecs=opus`).
*   **Safari Empty MIME Bug Fallback**: Added a browser user-agent sniffing script (`isIOS`/`isSafari`). If mobile Safari returns an empty `mediaRecorder.mimeType` string, the system correctly falls back to `audio/mp4` and saves the file with a `.m4a` extension instead of mislabeling it as a `.webm` file.
*   **Mobile Record Crash Fix**: Removed the `250` milliseconds timeslice argument from `mediaRecorder.start()`. Mobile WebKit does not support chunked recording reliably, which was causing empty (0-byte) voice note files.
*   **Dynamic Audio Playback**: Reconfigured the HTML `<audio>` elements (in both the Razor page and the dynamic SignalR message append JavaScript) to render the exact MIME type matching the file extension (`type="audio/mp4"` for `.m4a`/`.mp4` files and `type="audio/webm"` for `.webm` files). This resolves playback silences on iOS devices.

---

## ✉️ 6. Robust Email Verification Service

*   **MailKit Migration**: Replaced the obsolete `System.Net.Mail.SmtpClient` in [EmailSender.cs](ThriftHub/Services/EmailSender.cs) with the modern, secure **MailKit `SmtpClient`** to negotiate modern TLS/SSL handshakes successfully on cloud container hosts.
*   **Resend API**: Email delivery was later moved to the Resend HTTP API so emails work on Render where outbound SMTP is blocked.
*   **Render environment variables required** (Dashboard → your service → Environment):
    *   `Resend__ApiKey` = your Resend API key (`re_...`)
    *   `Resend__FromEmail` = `noreply@thrifthubgh.com` (must use your verified domain — not `onboarding@resend.dev`)
    *   `Resend__FromName` = `ThriftHub`
*   **Resend domain setup**: Add `thrifthubgh.com` at [resend.com/domains](https://resend.com/domains), copy the DNS records Resend gives you into Namecheap, wait for verification, then use a `@thrifthubgh.com` sender address.

---

## 👤 7. Registration Validation Splitting

*   **Validation Bypass for Customers**: Removed the `[Required]` attributes from identity verification properties (`IdCardType`, `IdCardNumber`, `IdCardFront`) in [RegisterViewModel.cs](ThriftHub/Models/RegisterViewModel.cs).
*   **Conditional Backend Validation**: Configured [AccountController.cs](ThriftHub/Controllers/AccountController.cs) to enforce ID document checks and upload file saves only if the user registers as a **Seller**.
*   **Dynamic UI Toggling**: Added a jQuery script in [Register.cshtml](ThriftHub/Views/Account/Register.cshtml) to hide the identity verification section and disable all its inputs when "Customer" is selected. Disabling hidden inputs prevents jQuery client-side validation from blocking form submission, resolving the issue where the button did not respond.
*   **Stale Registration Cleanup**: Added logic in `Register` POST to automatically delete existing unconfirmed records if a user attempts to register again with the same email, resolving "email already exists" conflicts from previous interrupted attempts.
