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

            _logger?.LogInformation(
                "Database backup saved to {BackupPath}.",
                backupPath);

            PruneOldBackups(
                backupsDirectory);
        }

        public async Task RestoreLatestBackupIfDatabaseEmptyAsync(
            ApplicationDbContext context)
        {
            var databasePath =
                GetDatabasePath();

            var userCount =
                await context.Users.CountAsync();

            if (userCount > 0)
            {
                _logger?.LogInformation(
                    "Database contains {UserCount} accounts — no restore needed.",
                    userCount);

                return;
            }

            var backupsDirectory =
                GetBackupsDirectory();

            if (!Directory.Exists(backupsDirectory))
            {
                return;
            }

            var backupFiles =
                Directory.GetFiles(
                    backupsDirectory,
                    "thrifthub-*.db")
                    .OrderByDescending(
                        path => path)
                    .ToList();

            foreach (var backupPath in backupFiles)
            {
                var backupUserCount =
                    CountUsersInDatabase(
                        backupPath);

                if (backupUserCount <= 0)
                {
                    continue;
                }

                _logger?.LogWarning(
                    "Empty database detected. Restoring {UserCount} accounts from backup {BackupPath}.",
                    backupUserCount,
                    backupPath);

                await context.Database.CloseConnectionAsync();

                File.Copy(
                    backupPath,
                    databasePath,
                    overwrite: true);

                await context.Database.MigrateAsync();

                var restoredCount =
                    await context.Users.CountAsync();

                _logger?.LogWarning(
                    "Database restore complete. {UserCount} accounts are available again.",
                    restoredCount);

                return;
            }

            _logger?.LogWarning(
                "Database has no accounts and no usable backup was found.");
        }

        public async Task LogDatabaseHealthAsync(
            ApplicationDbContext context)
        {
            var userCount =
                await context.Users.CountAsync();

            var messageCount =
                await context.Messages.CountAsync();

            _logger?.LogInformation(
                "Database health: {UserCount} accounts, {MessageCount} messages stored at {DatabasePath}. Persistent storage: {UsesPersistentStorage}.",
                userCount,
                messageCount,
                GetDatabasePath(),
                UsesPersistentStorage);
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
