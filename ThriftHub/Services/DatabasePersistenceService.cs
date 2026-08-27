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

            if (
                runningOnRender ||
                (
                    isProduction &&
                    OperatingSystem.IsLinux()))
            {
                return DefaultRenderDataPath;
            }

            return null;
        }

        private static string BuildSqliteConnectionString(
            string databasePath)
        {
            return
                $"Data Source={databasePath};Mode=ReadWriteCreate";
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

            return BuildSqliteConnectionString(databasePath);
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

            return BuildSqliteConnectionString(databasePath);
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

            EnsureStorageWritable();

            var primaryPath =
                GetDatabasePath();

            EnsureDatabaseWritable(
                primaryPath);

            BackupDatabaseIfExists();

            SqliteConnection.ClearAllPools();

            if (UsesPersistentStorage)
            {
                await EnsurePersistentPrimaryDatabaseAsync(
                    context,
                    primaryPath);
            }
            else
            {
                await EnsureLocalPrimaryDatabaseAsync(
                    context,
                    primaryPath);
            }

            await RunMigrationsWithRecoveryAsync(
                context,
                primaryPath);

            await RestoreIfDatabaseDegradedAsync(
                context);

            await LogDatabaseHealthAsync(context);

            ValidateRenderDiskMount();
        }

        private async Task EnsurePersistentPrimaryDatabaseAsync(
            ApplicationDbContext context,
            string primaryPath)
        {
            var primaryStats =
                GetDatabaseStats(primaryPath);

            if (primaryStats.UserCount == 0)
            {
                var legacySource =
                    FindLegacyMigrationSource();

                if (legacySource != null)
                {
                    _logger.LogWarning(
                        "Migrating {UserCount} accounts from legacy database {SourcePath} into persistent storage.",
                        legacySource.UserCount,
                        legacySource.Path);

                    await context.Database.CloseConnectionAsync();

                    SqliteConnection.ClearAllPools();

                    CopyDatabaseFile(
                        legacySource.Path,
                        primaryPath);

                    EnsureDatabaseWritable(
                        primaryPath);
                }
            }

            // Never replace an existing persistent database with bundled deploy artifacts.
            // Recovery from /data/backups only happens in RestoreIfDatabaseDegradedAsync.
        }

        private async Task EnsureLocalPrimaryDatabaseAsync(
            ApplicationDbContext context,
            string primaryPath)
        {
            var primaryStats =
                GetDatabaseStats(primaryPath);

            var bestBackup =
                FindBestBackupCandidate();

            if (
                bestBackup != null &&
                primaryStats.UserCount == 0 &&
                bestBackup.UserCount > 0)
            {
                _logger.LogWarning(
                    "Restoring {UserCount} accounts from backup {SourcePath} into local database.",
                    bestBackup.UserCount,
                    bestBackup.Path);

                await context.Database.CloseConnectionAsync();

                SqliteConnection.ClearAllPools();

                CopyDatabaseFile(
                    bestBackup.Path,
                    primaryPath);

                EnsureDatabaseWritable(
                    primaryPath);
            }
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

            var currentStats =
                GetDatabaseStats(databasePath);

            if (
                onlyWhenEmpty &&
                currentStats.UserCount > 0)
            {
                return;
            }

            var bestBackup =
                FindBestBackupCandidate();

            var latestBackup =
                FindLatestBackupCandidate();

            if (
                UsesPersistentStorage &&
                latestBackup != null &&
                LooksLikeSeedOverwrite(
                    currentStats,
                    latestBackup))
            {
                bestBackup = latestBackup;
            }

            if (bestBackup == null)
            {
                if (currentStats.UserCount == 0)
                {
                    _logger.LogWarning(
                        "Database has no accounts and no usable backup was found.");
                }

                return;
            }

            if (
                !onlyWhenEmpty &&
                !ShouldRestoreFromBackup(
                    currentStats,
                    bestBackup))
            {
                return;
            }

            _logger.LogWarning(
                "Restoring database from backup {BackupPath} ({UserCount} accounts, {ProductCount} products, {MessageCount} messages).",
                bestBackup.Path,
                bestBackup.UserCount,
                bestBackup.ProductCount,
                bestBackup.MessageCount);

            await context.Database.CloseConnectionAsync();

            SqliteConnection.ClearAllPools();

            CopyDatabaseFile(
                bestBackup.Path,
                databasePath);

            EnsureDatabaseWritable(
                databasePath);

            await context.Database.MigrateAsync();

            var restoredStats =
                GetDatabaseStats(databasePath);

            _logger.LogWarning(
                "Database restore complete. {UserCount} accounts and {ProductCount} products are available again.",
                restoredStats.UserCount,
                restoredStats.ProductCount);
        }

        private static bool ShouldRestoreFromBackup(
            DatabaseCandidate current,
            DatabaseCandidate backup)
        {
            if (LooksLikeSeedOverwrite(current, backup))
            {
                return true;
            }

            if (backup.ProductCount > current.ProductCount)
            {
                return true;
            }

            if (
                current.ProductCount == 0 &&
                backup.ProductCount > 0)
            {
                return true;
            }

            if (
                backup.MessageCount > current.MessageCount &&
                backup.UserCount >= current.UserCount)
            {
                return true;
            }

            if (backup.UserCount > current.UserCount)
            {
                return true;
            }

            return false;
        }

        private static bool LooksLikeSeedOverwrite(
            DatabaseCandidate current,
            DatabaseCandidate backup)
        {
            if (backup.UserCount == 0 && backup.ProductCount == 0)
            {
                return false;
            }

            if (
                current.ProductCount == 0 &&
                backup.ProductCount > 0)
            {
                return true;
            }

            if (
                backup.ProductCount > 0 &&
                current.UserCount > backup.UserCount + 1 &&
                backup.ProductCount >= current.ProductCount)
            {
                return true;
            }

            return false;
        }

        private DatabaseCandidate? FindLatestBackupCandidate()
        {
            var latestPath =
                DiscoverBackupCandidates()
                    .OrderByDescending(
                        path => path,
                        StringComparer.OrdinalIgnoreCase)
                    .FirstOrDefault();

            if (string.IsNullOrWhiteSpace(latestPath))
            {
                return null;
            }

            var stats =
                GetDatabaseStats(latestPath);

            if (stats.UserCount <= 0 && stats.ProductCount <= 0)
            {
                return null;
            }

            return stats;
        }

        public async Task LogDatabaseHealthAsync(
            ApplicationDbContext context)
        {
            var userCount =
                await context.Users.CountAsync();

            var productCount =
                await context.Products.CountAsync();

            var messageCount =
                await context.Messages.CountAsync();

            _logger.LogInformation(
                "Database health: {UserCount} accounts, {ProductCount} products, {MessageCount} messages stored at {DatabasePath}. Persistent storage: {UsesPersistentStorage}.",
                userCount,
                productCount,
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
                "Accounts, products, and messages WILL BE LOST on every deploy until you attach a persistent disk in the Render dashboard (mount path /data).",
                _dataRoot);
        }

        private DatabaseCandidate? FindBestBackupCandidate()
        {
            DatabaseCandidate? best =
                null;

            foreach (var backupPath in DiscoverBackupCandidates())
            {
                var stats =
                    GetDatabaseStats(backupPath);

                if (stats.UserCount <= 0 && stats.ProductCount <= 0)
                {
                    continue;
                }

                if (
                    best == null ||
                    stats.ProductCount > best.ProductCount ||
                    (
                        stats.ProductCount == best.ProductCount &&
                        stats.UserCount > best.UserCount) ||
                    (
                        stats.ProductCount == best.ProductCount &&
                        stats.UserCount == best.UserCount &&
                        stats.MessageCount > best.MessageCount))
                {
                    best = stats;
                }
            }

            return best;
        }

        private DatabaseCandidate? FindLegacyMigrationSource()
        {
            DatabaseCandidate? best =
                null;

            foreach (var legacyPath in DiscoverLegacyMigrationSources())
            {
                var stats =
                    GetDatabaseStats(legacyPath);

                if (stats.UserCount <= 0)
                {
                    continue;
                }

                if (
                    best == null ||
                    stats.UserCount > best.UserCount ||
                    (
                        stats.UserCount == best.UserCount &&
                        stats.ProductCount > best.ProductCount))
                {
                    best = stats;
                }
            }

            return best;
        }

        private DatabaseCandidate GetDatabaseStats(
            string databasePath)
        {
            if (!File.Exists(databasePath))
            {
                return new DatabaseCandidate
                {
                    Path = databasePath
                };
            }

            return new DatabaseCandidate
            {
                Path = databasePath,
                UserCount = CountUsersInDatabase(databasePath),
                ProductCount = CountProductsInDatabase(databasePath),
                MessageCount = CountMessagesInDatabase(databasePath)
            };
        }

        private IEnumerable<string> DiscoverBackupCandidates()
        {
            var seen =
                new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase);

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
                    if (seen.Add(backupPath))
                    {
                        yield return backupPath;
                    }
                }
            }
        }

        private IEnumerable<string> DiscoverLegacyMigrationSources()
        {
            var seen =
                new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase);

            foreach (var path in new[]
            {
                Path.Combine(
                    _contentRootPath,
                    "thrifthub.db"),
                Path.Combine(
                    _contentRootPath,
                    "thriftHub.db"),
                Path.Combine(
                    _contentRootPath,
                    "out",
                    "thrifthub.db"),
                Path.Combine(
                    _contentRootPath,
                    "out",
                    "thriftHub.db")
            })
            {
                if (
                    !string.IsNullOrWhiteSpace(path) &&
                    seen.Add(path))
                {
                    yield return path;
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

            public int ProductCount { get; set; }

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

        private static string BuildReadOnlyConnectionString(
            string databasePath)
        {
            return
                $"Data Source={databasePath};Mode=ReadOnly";
        }

        private static int CountUsersInDatabase(
            string databasePath)
        {
            try
            {
                using var connection =
                    new SqliteConnection(
                        BuildReadOnlyConnectionString(
                            databasePath));

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
                        BuildReadOnlyConnectionString(
                            databasePath));

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

        private static int CountProductsInDatabase(
            string databasePath)
        {
            try
            {
                using var connection =
                    new SqliteConnection(
                        BuildReadOnlyConnectionString(
                            databasePath));

                connection.Open();

                using var command =
                    connection.CreateCommand();

                command.CommandText =
                    "SELECT COUNT(*) FROM Products;";

                var result =
                    command.ExecuteScalar();

                return Convert.ToInt32(result);
            }
            catch
            {
                return 0;
            }
        }

        private async Task RunMigrationsWithRecoveryAsync(
            ApplicationDbContext context,
            string primaryPath)
        {
            for (var attempt = 1; attempt <= 3; attempt++)
            {
                try
                {
                    await PrepareDatabaseForMigrationAsync(
                        context,
                        primaryPath);

                    await context.Database.MigrateAsync();

                    await context.Database.ExecuteSqlRawAsync(
                        "PRAGMA journal_mode=WAL;");

                    return;
                }
                catch (SqliteException ex)
                    when (
                        ex.SqliteErrorCode == 8 &&
                        attempt < 3)
                {
                    _logger.LogWarning(
                        ex,
                        "Migration attempt {Attempt} failed because the database is read-only. Repairing storage.",
                        attempt);

                    RepairReadOnlyDatabase(
                        primaryPath);
                }
            }
        }

        private async Task PrepareDatabaseForMigrationAsync(
            ApplicationDbContext context,
            string primaryPath)
        {
            await context.Database.CloseConnectionAsync();

            SqliteConnection.ClearAllPools();

            EnsureDatabaseWritable(
                primaryPath);

            if (!File.Exists(primaryPath))
            {
                return;
            }

            try
            {
                using var connection =
                    new SqliteConnection(
                        BuildSqliteConnectionString(
                            primaryPath));

                connection.Open();

                using var command =
                    connection.CreateCommand();

                command.CommandText =
                    """
                    DELETE FROM "__EFMigrationsLock";
                    PRAGMA journal_mode=DELETE;
                    PRAGMA wal_checkpoint(TRUNCATE);
                    """;

                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Could not reset SQLite journal mode before migration.");
            }

            SqliteConnection.ClearAllPools();

            EnsureDatabaseWritable(
                primaryPath);
        }

        public void PrepareWritableStorage()
        {
            PrepareStorageDirectories();
            EnsureStorageWritable();
        }

        private void EnsureStorageWritable()
        {
            if (UsesPersistentStorage)
            {
                EnsureDirectoryWritable(_dataRoot!);
            }

            EnsureDirectoryWritable(
                GetBackupsDirectory());

            EnsureDirectoryWritable(
                GetDataProtectionKeysPath());

            EnsureDataProtectionKeysWritable();
        }

        private void EnsureDataProtectionKeysWritable()
        {
            var keysPath =
                GetDataProtectionKeysPath();

            if (!Directory.Exists(keysPath))
            {
                return;
            }

            foreach (var filePath in Directory.GetFiles(keysPath))
            {
                MakeFileWritable(filePath);
            }
        }

        private void EnsureDatabaseWritable(
            string databasePath)
        {
            var directory =
                Path.GetDirectoryName(databasePath);

            if (!string.IsNullOrWhiteSpace(directory))
            {
                EnsureDirectoryWritable(directory);
            }

            if (!File.Exists(databasePath))
            {
                return;
            }

            MakeFileWritable(databasePath);
            MakeFileWritable($"{databasePath}-wal");
            MakeFileWritable($"{databasePath}-shm");

            if (CanWriteToDatabase(databasePath))
            {
                return;
            }

            _logger.LogWarning(
                "Database at {DatabasePath} is read-only. Attempting repair.",
                databasePath);

            RepairReadOnlyDatabase(
                databasePath);

            if (!CanWriteToDatabase(databasePath))
            {
                throw new InvalidOperationException(
                    $"Database at {databasePath} is read-only and could not be repaired. " +
                    "Ensure the Render persistent disk mounted at /data is writable.");
            }
        }

        private void RepairReadOnlyDatabase(
            string databasePath)
        {
            var directory =
                Path.GetDirectoryName(databasePath)
                ?? throw new InvalidOperationException(
                    "Database path has no directory.");

            EnsureDirectoryWritable(directory);

            SqliteConnection.ClearAllPools();

            var tempPath =
                Path.Combine(
                    directory,
                    $"thrifthub-repair-{Guid.NewGuid():N}.db");

            try
            {
                File.Copy(
                    databasePath,
                    tempPath,
                    overwrite: false);

                MakeFileWritable(tempPath);

                if (!CanWriteToDatabase(tempPath))
                {
                    throw new InvalidOperationException(
                        $"Could not create a writable copy of the database in {directory}.");
                }

                RemoveDatabaseFiles(databasePath);

                File.Move(
                    tempPath,
                    databasePath);

                MakeFileWritable(databasePath);

                _logger.LogWarning(
                    "Repaired read-only database at {DatabasePath}.",
                    databasePath);
            }
            catch
            {
                if (File.Exists(tempPath))
                {
                    try
                    {
                        File.Delete(tempPath);
                    }
                    catch
                    {
                    }
                }

                throw;
            }
        }

        private static void CopyDatabaseFile(
            string sourcePath,
            string destinationPath)
        {
            RemoveDatabaseFiles(destinationPath);

            File.Copy(
                sourcePath,
                destinationPath,
                overwrite: false);

            MakeFileWritable(destinationPath);
        }

        private static void RemoveDatabaseFiles(
            string databasePath)
        {
            foreach (var path in new[]
            {
                databasePath,
                $"{databasePath}-wal",
                $"{databasePath}-shm"
            })
            {
                if (!File.Exists(path))
                {
                    continue;
                }

                MakeFileWritable(path);

                File.Delete(path);
            }
        }

        private static bool CanWriteToDatabase(
            string databasePath)
        {
            try
            {
                using var connection =
                    new SqliteConnection(
                        BuildSqliteConnectionString(
                            databasePath));

                connection.Open();

                using var command =
                    connection.CreateCommand();

                command.CommandText =
                    "CREATE TABLE IF NOT EXISTS __thrifthub_write_test (id INTEGER); " +
                    "DELETE FROM __thrifthub_write_test; " +
                    "DROP TABLE IF EXISTS __thrifthub_write_test;";

                command.ExecuteNonQuery();

                return true;
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == 8)
            {
                return false;
            }
        }

        private static void EnsureDirectoryWritable(
            string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            Directory.CreateDirectory(path);

            if (OperatingSystem.IsLinux() ||
                OperatingSystem.IsMacOS())
            {
                File.SetUnixFileMode(
                    path,
                    UnixFileMode.UserRead |
                    UnixFileMode.UserWrite |
                    UnixFileMode.UserExecute |
                    UnixFileMode.GroupRead |
                    UnixFileMode.GroupWrite |
                    UnixFileMode.GroupExecute);
            }
        }

        private static void MakeFileWritable(
            string path)
        {
            if (!File.Exists(path))
            {
                return;
            }

            if (OperatingSystem.IsLinux() ||
                OperatingSystem.IsMacOS())
            {
                File.SetUnixFileMode(
                    path,
                    UnixFileMode.UserRead |
                    UnixFileMode.UserWrite |
                    UnixFileMode.GroupRead |
                    UnixFileMode.GroupWrite |
                    UnixFileMode.OtherRead);
            }
            else
            {
                var attributes =
                    File.GetAttributes(path);

                if (attributes.HasFlag(FileAttributes.ReadOnly))
                {
                    File.SetAttributes(
                        path,
                        attributes & ~FileAttributes.ReadOnly);
                }
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
