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

            return Path.Combine(
                GetUploadsRoot(),
                relativePath.Replace('/', Path.DirectorySeparatorChar));
        }

        public void SeedPersistentDatabaseIfNeeded()
        {
            // Schema is created by EF migrations.
            // Never copy a bundled seed database over production data.
        }
    }
}
