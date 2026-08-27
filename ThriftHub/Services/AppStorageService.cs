using Microsoft.AspNetCore.Hosting;

namespace ThriftHub.Services
{
    public class AppStorageService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly string? _dataRoot;

        public AppStorageService(
            IConfiguration configuration,
            IWebHostEnvironment environment)
        {
            _environment = environment;

            _dataRoot =
                DatabasePersistenceService.ResolveDataRoot(
                    configuration,
                    environment.IsProduction());

            if (string.IsNullOrWhiteSpace(_dataRoot))
            {
                return;
            }

            Directory.CreateDirectory(_dataRoot);
            Directory.CreateDirectory(GetUploadsRoot());
            Directory.CreateDirectory(GetDataProtectionKeysPath());
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
                _environment.ContentRootPath,
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
                _environment.ContentRootPath,
                "dp-keys");
        }

        public string GetUploadsRoot()
        {
            if (UsesPersistentStorage)
            {
                return Path.Combine(
                    _dataRoot!,
                    "uploads");
            }

            return Path.Combine(
                _environment.WebRootPath,
                "uploads");
        }

        public string GetUploadsCategoryPath(
            string category)
        {
            var path =
                GetUploadsRoot();

            foreach (var part in category.Split(
                         '/',
                         '\\',
                         StringSplitOptions.RemoveEmptyEntries))
            {
                path =
                    Path.Combine(
                        path,
                        part);
            }

            Directory.CreateDirectory(path);

            return path;
        }

        public string BuildUploadsWebPath(
            string category,
            string fileName)
        {
            return
                $"/uploads/{category}/{fileName}";
        }

        public string? MapWebPathToPhysicalPath(
            string? webPath)
        {
            if (string.IsNullOrWhiteSpace(webPath))
            {
                return null;
            }

            var normalized =
                webPath
                    .Trim()
                    .Replace('\\', '/');

            if (!normalized.StartsWith(
                    "/uploads/",
                    StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var relativePath =
                normalized["/uploads/".Length..];

            var primaryPath =
                Path.Combine(
                    GetUploadsRoot(),
                    relativePath.Replace('/', Path.DirectorySeparatorChar));

            if (File.Exists(primaryPath))
            {
                return primaryPath;
            }

            if (UsesPersistentStorage)
            {
                var legacyPath =
                    Path.Combine(
                        _environment.WebRootPath,
                        "uploads",
                        relativePath.Replace('/', Path.DirectorySeparatorChar));

                if (File.Exists(legacyPath))
                {
                    return legacyPath;
                }
            }

            return primaryPath;
        }

        public void MigrateLegacyUploadsToPersistentStorage()
        {
            if (!UsesPersistentStorage)
            {
                return;
            }

            var persistentRoot =
                GetUploadsRoot();

            Directory.CreateDirectory(persistentRoot);

            var legacyRoots =
                new[]
                {
                    Path.Combine(
                        _environment.WebRootPath,
                        "uploads"),
                    Path.Combine(
                        _environment.ContentRootPath,
                        "wwwroot",
                        "uploads"),
                    Path.Combine(
                        _environment.ContentRootPath,
                        "uploads")
                };

            foreach (var legacyRoot in legacyRoots)
            {
                if (!Directory.Exists(legacyRoot))
                {
                    continue;
                }

                CopyMissingFiles(
                    legacyRoot,
                    persistentRoot);
            }
        }

        private static void CopyMissingFiles(
            string sourceDirectory,
            string destinationDirectory)
        {
            foreach (var sourcePath in Directory.EnumerateFiles(
                sourceDirectory,
                "*",
                SearchOption.AllDirectories))
            {
                var relativePath =
                    Path.GetRelativePath(
                        sourceDirectory,
                        sourcePath);

                var destinationPath =
                    Path.Combine(
                        destinationDirectory,
                        relativePath);

                var destinationFolder =
                    Path.GetDirectoryName(destinationPath);

                if (!string.IsNullOrWhiteSpace(destinationFolder))
                {
                    Directory.CreateDirectory(destinationFolder);
                }

                if (
                    File.Exists(destinationPath) &&
                    new FileInfo(destinationPath).Length > 0)
                {
                    continue;
                }

                File.Copy(
                    sourcePath,
                    destinationPath,
                    overwrite: false);
            }
        }

        public void SeedPersistentDatabaseIfNeeded()
        {
            // Schema is created by EF migrations.
            // Never copy a bundled seed database over production data.
        }
    }
}
