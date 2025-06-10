using System.ComponentModel.DataAnnotations;

namespace FileShare.Models;

[Serializable]
public class UploadViewModel
{
    [Required]
    public IFormFile File { get; set; } = null!;
    
    [Range(1, 168, ErrorMessage = "Expiration must be between 1 and 168 hours")]
    public int ExpirationHours { get; set; } = 1;
    
    [Range(1, 1000, ErrorMessage = "Download limit must be between 1 and 1000")]
    public int MaxDownloads { get; set; } = 5;
}