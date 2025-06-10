using System.ComponentModel.DataAnnotations;

namespace FileShare.Data.Entities;

[Serializable]
public class FileEntity
{
    public int Id { get; set; }
    
    [Required]
    public string OriginalName { get; set; } = string.Empty;
    
    [Required]
    public string StoredName { get; set; } = string.Empty;
    
    [Required]
    public string DownloadToken { get; set; } = string.Empty;
    
    public DateTime UploadDate { get; set; }
    public DateTime ExpiresAt { get; set; }
    public int MaxDownloads { get; set; }
    public int CurrentDownloads { get; set; }
    public long FileSizeBytes { get; set; }
    public bool IsActive { get; set; } = true;
    
    public List<DownloadLogEntity> DownloadLogs { get; set; } = [];
}