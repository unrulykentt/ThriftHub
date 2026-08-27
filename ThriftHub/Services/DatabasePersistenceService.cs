using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ThriftHub.Data;

namespace ThriftHub.Services
{
    public class DatabasePersistenceService
    {
        public const string DefaultRenderDataPath =
            "/data";

        private const int MaxBackups =
            15;

        private readonly string? _dataRoot;
        private readonly string _contentRootPath;
        private readonly bool _isProduction;
        private readonly ILogger<DatabasePersistenceService> _logger;

        public DatabasePersistenceService(
            IConfiguration configuration,
            IWebHostEnvironment environment,
            ILogger<DatabasePersistenceService> logger)
        {
            _contentRootPath =
                environment.ContentRootPath;

            _isProduction =
                environment.IsProduction();

            _logger =
                logger;

            _dataRoot =
                ResolveDataRoot(
                    configuration,
                    _isProduction);
        }

        public static string? ResolveDataRoot(
            IConfiguration configuration,
            bool isProduction)
        {
            var configuredPath =
                configuration["ThriftHub:DataPath"]?
                    .Trim()
                    .TrimEnd('/', '\\');

            if (!string.IsNullOrWhiteSpace(configuredPath))
            {
                return configuredPath;
            }

            var runningOnRender =
                !string.IsNullOrWhiteSpace(
                    Environment.GetEnvironmentVariable("RENDER"));

            if (isProduction || runningOnRender)
            {
                return DefaultRenderDataPath;
            }

            return null;
        }

        public bool UsesPersistentStorage =>
            !string.IsNullOrWhiteSpace(_dataRoot);

        public string? DataRoot =>
            _dataRoot;

        public string GetDatabasePath()
        {
            if (UsesPersistentStorage)
            {
                return Path.Combine(
                    _dataRoot!,
                    "thrifthub.db");
            }

            return Path.Combine(
                _contentRootPath,
                "thrifthub.db");
        }

        public string GetDataProtectionKeysPath()
        {
            if (UsesPersistentStorage)
            {
                return Path.Combine(
                    _dataRoot!,
                    "dp-keys");
            }

            return Path.Combine(
                _contentRootPath,
                "dp-keys");
        }

        public string GetBackupsDirectory()
        {
            if (UsesPersistentStorage)
            {
                return Path.Combine(
                    _dataRoot!,
                    "backups");
            }

            return Path.Combine(
                _contentRootPath,
                "backups");
        }

        public static string BuildConnectionString(
            IConfiguration configuration,
            IWebHostEnvironment environment)
        {
            var dataRoot =
                ResolveDataRoot(
                    configuration,
                    environment.IsProduction());

            var databasePath =
                !string.IsNullOrWhiteSpace(dataRoot)
                    ? Path.Combine(
                        dataRoot,
                        "thrifthub.db")
                    : Path.Combine(
                        environment.ContentRootPath,
                        "thrifthub.db");

            if (!string.IsNullOrWhiteSpace(dataRoot))
            {
                Directory.CreateDirectory(dataRoot);
            }

            return
                $"Data Source={databasePath};Cache=Shared";
        }

        public static string GetDataProtectionPath(
            IConfiguration configuration,
            IWebHostEnvironment environment)
        {
            var dataRoot =
                ResolveDataRoot(
                    configuration,
                    environment.IsProduction());

            if (!string.IsNullOrWhiteSpace(dataRoot))
            {
                return Path.Combine(
                    dataRoot,
                    "dp-keys");
            }

            return Path.Combine(
                environment.ContentRootPath,
                "dp-keys");
        }

        public string BuildConnectionString()
        {
            var databasePath =
                GetDatabasePath();

            if (UsesPersistentStorage)
            {
                Directory.CreateDirectory(_dataRoot!);
            }

            return
                $"Data Source={databasePath};Cache=Shared";
        }

        public void PrepareStorageDirectories()
        {
            if (UsesPersistentStorage)
            {
                Directory.CreateDirectory(_dataRoot!);
            }

            Directory.CreateDirectory(
                GetBackupsDirectory());

            Directory.CreateDirectory(
                GetDataProtectionKeysPath());
        }

        public void BackupDatabaseIfExists()
        {
            var databasePath =
                GetDatabasePath();

            if (!File.Exists(databasePath))
            {
                return;
            }

            var fileInfo =
                new FileInfo(databasePath);

            if (fileInfo.Length == 0)
            {
                return;
            }

            var backupsDirectory =
                GetBackupsDirectory();

            Directory.CreateDirectory(
                backupsDirectory);

            var backupPath =
                Path.Combine(
                    backupsDirectory,
                    $"thrifthub-{DateTime.UtcNow:yyyyMMdd-HHmmss}.db");

            File.Copy(
                databasePath,
                backupPath,
                overwrite: false);

            _logger.LogInformation(
                "Database backup saved to {BackupPath}.",
                backupPath);

            PruneOldBackups(
                backupsDirectory);
        }

        public async Task EnsureBestDatabaseAvailableAsync(
            ApplicationDbContext context)
        {
            PrepareStorageDirectories();

            BackupDatabaseIfExists();

            var primaryPath =
                GetDatabasePath();

            var bestCandidate =
                FindBestDatabaseCandidate();

            if (
                bestCandidate != null &&
                !string.Equals(
                    bestCandidate.Path,
                    primaryPath,
                    StringComparison.OrdinalIgnoreCase) &&
                bestCandidate.UserCount >
                CountUsersInDatabase(primaryPath))
            {
                _logger.LogWarning(
                    "Recovering {UserCount} accounts from {SourcePath} into {TargetPath}.",
                    bestCandidate.UserCount,
                    bestCandidate.Path,
                    primaryPath);

                await context.Database.CloseConnectionAsync();

                var primaryDirectory =
                    Path.GetDirectoryName(primaryPath);

                if (!string.IsNullOrWhiteSpace(primaryDirectory))
                {
                    Directory.CreateDirectory(primaryDirectory);
                }

                File.Copy(
                    bestCandidate.Path,
                    primaryPath,
                    overwrite: true);
            }

            await context.Database.MigrateAsync();

            await context.Database.ExecuteSqlRawAsync(
                "PRAGMA journal_mode=WAL;");

            await RestoreIfDatabaseDegradedAsync(
                context);

            await LogDatabaseHealthAsync(context);

            ValidateRenderDiskMount();
        }

        public async Task RestoreLatestBackupIfDatabaseEmptyAsync(
            ApplicationDbContext context)
        {
            await RestoreIfDatabaseDegradedAsync(
                context,
                onlyWhenEmpty: true);
        }

        private async Task RestoreIfDatabaseDegradedAsync(
            ApplicationDbContext context,
            bool onlyWhenEmpty = false)
        {
            var databasePath =
                GetDatabasePath();

            var currentUserCount =
                await context.Users.CountAsync();

            if (
                onlyWhenEmpty &&
                currentUserCount > 0)
            {
                return;
            }

            var backupsDirectory =
                GetBackupsDirectory();

            if (!Directory.Exists(backupsDirectory))
            {
                if (currentUserCount == 0)
                {
                    _logger.LogWarning(
                        "Database has no accounts and no backup directory was found.");
                }

                return;
            }

            var backupFiles =
                Directory.GetFiles(
                    backupsDirectory,
                    "thrifthub-*.db")
                    .OrderByDescending(
                        path => path)
                    .ToList();

            DatabaseCandidate? bestBackup =
                null;

            foreach (var backupPath in backupFiles)
            {
                var backupUserCount =
                    CountUsersInDatabase(
                        backupPath);

                if (backupUserCount <= 0)
                {
                    continue;
                }

                if (
                    bestBackup == null ||
                    backupUserCount > bestBackup.UserCount)
                {
                    bestBackup =
                        new DatabaseCandidate
                        {
                            Path = backupPath,
                            UserCount = backupUserCount,
                            MessageCount =
                                CountMessagesInDatabase(
                                    backupPath)
                        };
                }
            }

            if (bestBackup == null)
            {
                if (currentUserCount == 0)
                {
                    _logger.LogWarning(
                        "Database has no accounts and no usable backup was found.");
                }

                return;
            }

            if (
                !onlyWhenEmpty &&
                currentUserCount >= bestBackup.UserCount)
            {
                return;
            }

            _logger.LogWarning(
                "Restoring {UserCount} accounts from backup {BackupPath}.",
                bestBackup.UserCount,
                bestBackup.Path);

            await context.Database.CloseConnectionAsync();

            File.Copy(
                bestBackup.Path,
                databasePath,
                overwrite: true);

            await context.Database.MigrateAsync();

            var restoredCount =
                await context.Users.CountAsync();

            _logger.LogWarning(
                "Database restore complete. {UserCount} accounts are available again.",
                restoredCount);
        }

        public async Task LogDatabaseHealthAsync(
            ApplicationDbContext context)
        {
            var userCount =
                await context.Users.CountAsync();

            var messageCount =
                await context.Messages.CountAsync();

            _logger.LogInformation(
                "Database health: {UserCount} accounts, {MessageCount} messages stored at {DatabasePath}. Persistent storage: {UsesPersistentStorage}.",
                userCount,
                messageCount,
                GetDatabasePath(),
                UsesPersistentStorage);
        }

        public void ValidateRenderDiskMount()
        {
            if (!UsesPersistentStorage)
            {
                return;
            }

            var runningOnRender =
                !string.IsNullOrWhiteSpace(
                    Environment.GetEnvironmentVariable("RENDER"));

            if (!runningOnRender)
            {
                return;
            }

            if (IsPathMounted(_dataRoot!))
            {
                _logger.LogInformation(
                    "Render persistent disk is mounted at {DataPath}.",
                    _dataRoot);

                return;
            }

            _logger.LogCritical(
                "RENDER DISK NOT MOUNTED at {DataPath}. " +
                "Accounts and messages WILL BE LOST on every deploy until you add a persistent disk in the Render dashboard.",
                _dataRoot);
        }

        private DatabaseCandidate? FindBestDatabaseCandidate()
        {
            DatabaseCandidate? best =
                null;

            foreach (var candidatePath in DiscoverDatabaseCandidates())
            {
                if (!File.Exists(candidatePath))
                {
                    continue;
                }

                var userCount =
                    CountUsersInDatabase(
                        candidatePath);

                if (userCount <= 0)
                {
                    continue;
                }

                var messageCount =
                    CountMessagesInDatabase(
                        candidatePath);

                if (
                    best == null ||
                    userCount > best.UserCount ||
                    (
                        userCount == best.UserCount &&
                        messageCount > best.MessageCount))
                {
                    best =
                        new DatabaseCandidate
                        {
                            Path = candidatePath,
                            UserCount = userCount,
                            MessageCount = messageCount
                        };
                }
            }

            return best;
        }

        private IEnumerable<string> DiscoverDatabaseCandidates()
        {
            var seen =
                new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase);

            foreach (var path in new[]
            {
                GetDatabasePath(),
                Path.Combine(
                    _contentRootPath,
                    "thrifthub.db"),
                Path.Combine(
                    _contentRootPath,
                    "out",
                    "thrifthub.db"),
                "/opt/render/project/src/thrifthub.db"
            })
            {
                if (
                    string.IsNullOrWhiteSpace(path) ||
                    !seen.Add(path))
                {
                    continue;
                }

                yield return path;
            }

            foreach (var backupsDirectory in new[]
            {
                GetBackupsDirectory(),
                Path.Combine(
                    _contentRootPath,
                    "backups")
            })
            {
                if (!Directory.Exists(backupsDirectory))
                {
                    continue;
                }

                foreach (var backupPath in Directory.GetFiles(
                    backupsDirectory,
                    "thrifthub-*.db"))
                {
                    if (!seen.Add(backupPath))
                    {
                        continue;
                    }

                    yield return backupPath;
                }
            }
        }

        private static bool IsPathMounted(
            string path)
        {
            if (!OperatingSystem.IsLinux())
            {
                return true;
            }

            try
            {
                var normalizedPath =
                    path.Replace('\\', '/').TrimEnd('/');

                foreach (var line in File.ReadLines("/proc/mounts"))
                {
                    var parts =
                        line.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                    if (parts.Length < 2)
                    {
                        continue;
                    }

                    var mountPoint =
                        parts[1].TrimEnd('/');

                    if (
                        string.Equals(
                            mountPoint,
                            normalizedPath,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }
            catch
            {
            }

            return false;
        }

        private sealed class DatabaseCandidate
        {
            public string Path { get; set; } = string.Empty;

            public int UserCount { get; set; }

            public int MessageCount { get; set; }
        }

        public void LogPersistenceWarnings()
        {
            if (UsesPersistentStorage)
            {
                _logger?.LogInformation(
                    "Persistent storage enabled at {DataPath}. Accounts and messages survive redeploys when this disk is mounted on Render.",
                    _dataRoot);

                return;
            }

            if (_isProduction)
            {
                _logger?.LogCritical(
                    "PRODUCTION IS NOT USING PERSISTENT STORAGE. " +
                    "Every deploy will erase accounts and messages unless a Render disk is mounted and ThriftHub__DataPath is set.");
            }
        }

        private static int CountUsersInDatabase(
            string databasePath)
        {
            try
            {
                using var connection =
                    new SqliteConnection(
                        $"Data Source={databasePath};Mode=ReadOnly;Cache=Shared");

                connection.Open();

                using var command =
                    connection.CreateCommand();

                command.CommandText =
                    "SELECT COUNT(*) FROM AspNetUsers;";

                var result =
                    command.ExecuteScalar();

                return Convert.ToInt32(result);
            }
            catch
            {
                return 0;
            }
        }

        private static int CountMessagesInDatabase(
            string databasePath)
        {
            try
            {
                using var connection =
                    new SqliteConnection(
                        $"Data Source={databasePath};Mode=ReadOnly;Cache=Shared");

                connection.Open();

                using var command =
                    connection.CreateCommand();

                command.CommandText =
                    "SELECT COUNT(*) FROM Messages;";

                var result =
                    command.ExecuteScalar();

                return Convert.ToInt32(result);
            }
            catch
            {
                return 0;
            }
        }

        private void PruneOldBackups(
            string backupsDirectory)
        {
            var backupFiles =
                Directory.GetFiles(
                    backupsDirectory,
                    "thrifthub-*.db")
                    .OrderByDescending(
                        path => path)
                    .Skip(MaxBackups)
                    .ToList();

            foreach (var backupPath in backupFiles)
            {
                try
                {
                    File.Delete(backupPath);
                }
                catch
                {
                }
            }
        }
    }
}
