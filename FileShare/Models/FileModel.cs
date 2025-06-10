namespace FileShare.Models;

[Serializable]
public class FileModel
{
    public int Id { get; set; }
    public string OriginalName { get; set; } = string.Empty;
    public string DownloadToken { get; set; } = string.Empty;
    public DateTime UploadDate { get; set; }
    public DateTime ExpiresAt { get; set; }
    public int MaxDownloads { get; set; }
    public int CurrentDownloads { get; set; }
    public long FileSizeBytes { get; set; }
    public bool IsActive { get; set; }
    public string DownloadUrl => $"/d/{DownloadToken}";
    
    public string FileSizeFormatted => FormatFileSize(FileSizeBytes);
    public string TimeRemaining => GetTimeRemaining();
    
    private static string FormatFileSize(long bytes)
    {
        string[] sizes = ["B", "KB", "MB", "GB"];
        double len = bytes;
        var order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
    
    private string GetTimeRemaining()
    {
        var remaining = ExpiresAt - DateTime.UtcNow;
        if (remaining <= TimeSpan.Zero) return "Expired";
        
        if (remaining.TotalHours >= 1)
            return $"{remaining.Hours}h {remaining.Minutes}m";
        else
            return $"{remaining.Minutes}m {remaining.Seconds}s";
    }
}