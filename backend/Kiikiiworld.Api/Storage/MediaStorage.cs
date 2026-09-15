namespace Kiikiiworld.Api.Storage;

public class MediaStorage
{
    public string DataDir { get; }
    public string ImageOriginalDir { get; }
    public string ImageOptimizedDir { get; }

    public MediaStorage(string dataDir)
    {
        DataDir = Path.GetFullPath(dataDir);
        ImageOriginalDir = Path.Combine(DataDir, "media", "images", "original");
        ImageOptimizedDir = Path.Combine(DataDir, "media", "images", "optimized");

        Directory.CreateDirectory(ImageOriginalDir);
        Directory.CreateDirectory(ImageOptimizedDir);
    }

    // Legacy Media rows (seeded from the old S3-backed site) point Url/OriginalUrl at
    // external URLs, not local files — skip anything that isn't one of ours.
    public void TryDeleteByUrl(string? url)
    {
        if (string.IsNullOrEmpty(url) || !url.StartsWith("/media/")) return;

        var path = Path.Combine(DataDir, url.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        if (!Path.GetFullPath(path).StartsWith(DataDir)) return;

        if (File.Exists(path)) File.Delete(path);
    }
}
