using ThriftHub.Models;

namespace ThriftHub.Services
{
    public class IdentityDocumentArchiveService
    {
        private readonly AppStorageService _storage;

        public IdentityDocumentArchiveService(
            AppStorageService storage)
        {
            _storage = storage;
        }

        public async Task<(string? FrontUrl, string? BackUrl)> ArchiveReviewedDocumentsAsync(
            ApplicationUser user,
            string reviewStatus)
        {
            var statusFolder =
                reviewStatus.Equals(
                    "Approved",
                    StringComparison.OrdinalIgnoreCase)
                    ? "approved"
                    : "rejected";

            var frontUrl =
                await ArchiveSingleDocumentAsync(
                    user.IdCardFrontUrl,
                    user.FullName,
                    "front",
                    statusFolder);

            var backUrl =
                await ArchiveSingleDocumentAsync(
                    user.IdCardBackUrl,
                    user.FullName,
                    "back",
                    statusFolder);

            return (frontUrl, backUrl);
        }

        private async Task<string?> ArchiveSingleDocumentAsync(
            string? currentWebPath,
            string? fullName,
            string side,
            string statusFolder)
        {
            if (string.IsNullOrWhiteSpace(currentWebPath))
            {
                return null;
            }

            var sourcePath =
                _storage.MapWebPathToPhysicalPath(
                    currentWebPath);

            if (string.IsNullOrWhiteSpace(sourcePath) ||
                !File.Exists(sourcePath))
            {
                return currentWebPath;
            }

            var archiveDirectory =
                _storage.GetUploadsCategoryPath(
                    $"id-cards/reviewed/{statusFolder}");

            var slug =
                SanitizeNameForFile(fullName);

            var extension =
                Path.GetExtension(sourcePath);

            if (string.IsNullOrWhiteSpace(extension))
            {
                extension = ".jpg";
            }

            var fileName =
                ResolveUniqueFileName(
                    archiveDirectory,
                    $"{slug}-{side}",
                    extension);

            var destinationPath =
                Path.Combine(
                    archiveDirectory,
                    fileName);

            await using (
                var sourceStream =
                    new FileStream(
                        sourcePath,
                        FileMode.Open,
                        FileAccess.Read,
                        FileShare.Read))
            await using (
                var destinationStream =
                    new FileStream(
                        destinationPath,
                        FileMode.CreateNew,
                        FileAccess.Write,
                        FileShare.None))
            {
                await sourceStream.CopyToAsync(
                    destinationStream);
            }

            return
                $"/uploads/id-cards/reviewed/{statusFolder}/{fileName}";
        }

        private static string SanitizeNameForFile(
            string? fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
            {
                return "user";
            }

            var slug =
                new string(
                    fullName
                        .Trim()
                        .ToLowerInvariant()
                        .Select(c =>
                            char.IsLetterOrDigit(c)
                                ? c
                                : '-')
                        .ToArray());

            while (slug.Contains("--"))
            {
                slug =
                    slug.Replace(
                        "--",
                        "-",
                        StringComparison.Ordinal);
            }

            slug =
                slug.Trim('-');

            if (string.IsNullOrWhiteSpace(slug))
            {
                return "user";
            }

            if (slug.Length > 60)
            {
                slug =
                    slug[..60].Trim('-');
            }

            return slug;
        }

        private static string ResolveUniqueFileName(
            string directory,
            string baseName,
            string extension)
        {
            var candidate =
                $"{baseName}{extension}";

            var candidatePath =
                Path.Combine(
                    directory,
                    candidate);

            if (!File.Exists(candidatePath))
            {
                return candidate;
            }

            var counter = 2;

            while (true)
            {
                candidate =
                    $"{baseName}-{counter}{extension}";

                candidatePath =
                    Path.Combine(
                        directory,
                        candidate);

                if (!File.Exists(candidatePath))
                {
                    return candidate;
                }

                counter++;
            }
        }
    }
}
