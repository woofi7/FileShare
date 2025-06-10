using System.ComponentModel.DataAnnotations;

namespace FileShare.Data.Entities;

[Serializable]
public class DownloadLogEntity
{
    public int Id { get; set; }
    public int FileId { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public DateTime DownloadDate { get; set; }
    public bool Success { get; set; }
    
    public FileEntity File { get; set; } = null!;
}