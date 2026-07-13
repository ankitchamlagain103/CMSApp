using Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Infrastructure.Files
{
    // Local-disk implementation of IFileStorageService. Root comes from FileStorage:RootPath
    // (relative paths resolve against the content root). Stored names are generated GUIDs with
    // the original extension, so user-supplied file names never touch the file system.
    public class LocalFileStorageService : IFileStorageService
    {
        private readonly string _rootPath;

        public LocalFileStorageService(IConfiguration configuration, IHostEnvironment hostEnvironment)
        {
            var configuredRoot = configuration["FileStorage:RootPath"];
            if (string.IsNullOrWhiteSpace(configuredRoot))
            {
                configuredRoot = "Uploads";
            }

            _rootPath = Path.IsPathRooted(configuredRoot)
                ? configuredRoot
                : Path.Combine(hostEnvironment.ContentRootPath, configuredRoot);
        }

        public async Task<string> SaveAsync(Stream content, string originalFileName, string subdirectory, CancellationToken cancellationToken = default)
        {
            var extension = Path.GetExtension(originalFileName)?.ToLowerInvariant() ?? string.Empty;
            var storedFileName = Guid.NewGuid().ToString("N") + extension;
            var relativePath = Path.Combine(subdirectory, storedFileName).Replace('\\', '/');

            var absolutePath = ResolveWithinRoot(relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(absolutePath));

            using (var fileStream = new FileStream(absolutePath, FileMode.CreateNew, FileAccess.Write))
            {
                await content.CopyToAsync(fileStream, cancellationToken);
            }

            return relativePath;
        }

        public Task<Stream> OpenReadAsync(string relativePath, CancellationToken cancellationToken = default)
        {
            var absolutePath = ResolveWithinRoot(relativePath);
            if (!File.Exists(absolutePath))
            {
                return Task.FromResult<Stream>(null);
            }

            Stream fileStream = new FileStream(absolutePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            return Task.FromResult(fileStream);
        }

        public void Delete(string relativePath)
        {
            try
            {
                var absolutePath = ResolveWithinRoot(relativePath);
                if (File.Exists(absolutePath))
                {
                    File.Delete(absolutePath);
                }
            }
            catch (IOException)
            {
                // Best-effort by contract: a locked/missing file must not fail the caller.
            }
        }

        // Rejects any relative path that escapes the storage root (defense against a tampered
        // path landing in the database).
        private string ResolveWithinRoot(string relativePath)
        {
            var combinedPath = Path.GetFullPath(Path.Combine(_rootPath, relativePath));
            var normalizedRoot = Path.GetFullPath(_rootPath);
            if (!combinedPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Resolved file path escapes the storage root.");
            }

            return combinedPath;
        }
    }
}
