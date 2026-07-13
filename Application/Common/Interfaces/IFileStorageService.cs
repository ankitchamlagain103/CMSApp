namespace Application.Common.Interfaces
{
    // Storage abstraction for uploaded document files. The implementation decides where the
    // bytes live (local disk today); callers only ever hold the returned relative path -- it is
    // an opaque handle, stored on the owning row and passed back to read/delete the file.
    public interface IFileStorageService
    {
        // Persists the stream under a generated unique name (keeping the original extension)
        // inside the given subdirectory, and returns the relative path to store.
        Task<string> SaveAsync(Stream content, string originalFileName, string subdirectory, CancellationToken cancellationToken = default);

        // Returns a readable stream for the stored file, or null when it no longer exists.
        Task<Stream> OpenReadAsync(string relativePath, CancellationToken cancellationToken = default);

        // Best-effort removal; missing files are not an error.
        void Delete(string relativePath);
    }
}
