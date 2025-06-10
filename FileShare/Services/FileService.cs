using FileShare.Data;
using FileShare.Data.Entities;
using FileShare.Models;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace FileShare.Services;

public class FileService
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _config;
    private readonly string _uploadPath;
    
    public FileService(AppDbContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
        _uploadPath = _config["FileShare:UploadPath"] ?? "/uploads";
        
        Directory.CreateDirectory(_uploadPath);
    }
    
    public async Task<string> UploadFileAsync(UploadViewModel model)
    {
        var token = Guid.NewGuid().ToString();
        var storedName = $"{token}_{model.File.FileName}";
        var filePath = Path.Combine(_uploadPath, storedName);

        await using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await model.File.CopyToAsync(stream);
        }
        
        var fileEntity = new FileEntity
        {
            OriginalName = model.File.FileName,
            StoredName = storedName,
            DownloadToken = token,
            UploadDate = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(model.ExpirationHours),
            MaxDownloads = model.MaxDownloads,
            CurrentDownloads = 0,
            FileSizeBytes = model.File.Length,
            IsActive = true
        };
        
        _context.Files.Add(fileEntity);
        await _context.SaveChangesAsync();
        
        return token;
    }
    
    public async Task<List<FileModel>> GetActiveFilesAsync()
    {
        var files = await _context.Files
            .Where(f => f.IsActive && f.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(f => f.UploadDate)
            .Select(f => new FileModel
            {
                Id = f.Id,
                OriginalName = f.OriginalName,
                DownloadToken = f.DownloadToken,
                UploadDate = f.UploadDate,
                ExpiresAt = f.ExpiresAt,
                MaxDownloads = f.MaxDownloads,
                CurrentDownloads = f.CurrentDownloads,
                FileSizeBytes = f.FileSizeBytes,
                IsActive = f.IsActive
            })
            .ToListAsync();
            
        return files;
    }
    
    public async Task<(Stream? stream, string? fileName, string? contentType)?> GetFileForDownloadAsync(string token, string ipAddress, string userAgent)
    {
        var file = await _context.Files
            .FirstOrDefaultAsync(f => f.DownloadToken == token && f.IsActive);
            
        if (file == null || file.ExpiresAt <= DateTime.UtcNow || file.CurrentDownloads >= file.MaxDownloads)
        {
            await LogDownloadAsync(file?.Id ?? 0, ipAddress, userAgent, false);
            return null;
        }
        
        var filePath = Path.Combine(_uploadPath, file.StoredName);
        if (!File.Exists(filePath))
        {
            await LogDownloadAsync(file.Id, ipAddress, userAgent, false);
            return null;
        }
        
        file.CurrentDownloads++;
        await _context.SaveChangesAsync();
        await LogDownloadAsync(file.Id, ipAddress, userAgent, true);
        
        var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        var contentType = GetContentType(file.OriginalName);
        
        return (stream, file.OriginalName, contentType);
    }
    
    public async Task CleanupExpiredFilesAsync()
    {
        var expiredFiles = await _context.Files
            .Where(f => f.ExpiresAt <= DateTime.UtcNow || !f.IsActive)
            .ToListAsync();
            
        foreach (var file in expiredFiles)
        {
            var filePath = Path.Combine(_uploadPath, file.StoredName);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            
            _context.Files.Remove(file);
        }
        
        await _context.SaveChangesAsync();
    }
    
    public bool IsAdminRequest(string ipAddress)
    {
        var adminNetworks = _config.GetSection("FileShare:AdminNetworks").Get<string[]>() ?? [];
        
        foreach (var network in adminNetworks)
        {
            if (IsIpInRange(ipAddress, network))
                return true;
        }
        
        return false;
    }
    
    private async Task LogDownloadAsync(int fileId, string ipAddress, string userAgent, bool success)
    {
        if (fileId == 0) return;
        
        var log = new DownloadLogEntity
        {
            FileId = fileId,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            DownloadDate = DateTime.UtcNow,
            Success = success
        };
        
        _context.DownloadLogs.Add(log);
        await _context.SaveChangesAsync();
    }
    
    private static string GetContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".txt" => "text/plain",
            ".pdf" => "application/pdf",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".zip" => "application/zip",
            ".rar" => "application/x-rar-compressed",
            ".mp4" => "video/mp4",
            ".mp3" => "audio/mpeg",
            _ => "application/octet-stream"
        };
    }
    
    private static bool IsIpInRange(string ipAddress, string cidr)
    {
        try
        {
            var parts = cidr.Split('/');
            var ip = IPAddress.Parse(ipAddress);
            var network = IPAddress.Parse(parts[0]);
            var prefixLength = int.Parse(parts[1]);
            
            var ipBytes = ip.GetAddressBytes();
            var networkBytes = network.GetAddressBytes();
            
            if (ipBytes.Length != networkBytes.Length)
                return false;
                
            var bytesToCheck = prefixLength / 8;
            var bitsToCheck = prefixLength % 8;
            
            for (int i = 0; i < bytesToCheck; i++)
            {
                if (ipBytes[i] != networkBytes[i])
                    return false;
            }
            
            if (bitsToCheck > 0)
            {
                var mask = (byte)(0xFF << (8 - bitsToCheck));
                return (ipBytes[bytesToCheck] & mask) == (networkBytes[bytesToCheck] & mask);
            }
            
            return true;
        }
        catch
        {
            return false;
        }
    }
}