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
                configuration["ThriftHub:DataPath"]?
                    .Trim()
                    .TrimEnd('/', '\\');

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
            if (!UsesPersistentStorage)
            {
                return;
            }

            var targetPath =
                GetDatabasePath();

            if (File.Exists(targetPath))
            {
                return;
            }

            var seedPath =
                Path.Combine(
                    _environment.ContentRootPath,
                    "thrifthub.db");

            if (!File.Exists(seedPath))
            {
                return;
            }

            File.Copy(
                seedPath,
                targetPath);

            CopySeedUploadsIfEmpty();
        }

        private void CopySeedUploadsIfEmpty()
        {
            var persistentUploads =
                GetUploadsRoot();

            if (Directory.EnumerateFileSystemEntries(
                    persistentUploads)
                .Any())
            {
                return;
            }

            var seedUploads =
                Path.Combine(
                    _environment.WebRootPath,
                    "uploads");

            if (!Directory.Exists(seedUploads))
            {
                return;
            }

            CopyDirectory(
                seedUploads,
                persistentUploads);
        }

        private static void CopyDirectory(
            string sourceDir,
            string destinationDir)
        {
            Directory.CreateDirectory(destinationDir);

            foreach (var file in Directory.GetFiles(sourceDir))
            {
                var destinationFile =
                    Path.Combine(
                        destinationDir,
                        Path.GetFileName(file));

                if (!File.Exists(destinationFile))
                {
                    File.Copy(
                        file,
                        destinationFile);
                }
            }

            foreach (var directory in Directory.GetDirectories(sourceDir))
            {
                CopyDirectory(
                    directory,
                    Path.Combine(
                        destinationDir,
                        Path.GetFileName(directory)));
            }
        }
    }
}
