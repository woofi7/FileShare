using FileShare.Services;
using Microsoft.AspNetCore.Mvc;

namespace FileShare.Controllers;
public class DownloadController(FileService fileService) : Controller
{
    public async Task<IActionResult> Get(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return NotFound("Invalid download link.");
        }
        
        var clientIp = GetClientIpAddress();
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();
        
        var result = await fileService.GetFileForDownloadAsync(token, clientIp, userAgent);
        
        if (result == null)
        {
            return NotFound("File not found, expired, or download limit reached.");
        }
        
        var (stream, fileName, contentType) = result.Value;
        
        return File(stream!, contentType!, fileName, true);
    }
    
    private string GetClientIpAddress()
    {
        // Check for forwarded IP from reverse proxy first
        var forwardedFor = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            // X-Forwarded-For can contain multiple IPs, take the first one
            var ip = forwardedFor.Split(',')[0].Trim();
            if (IsValidIpv4(ip))
                return ip;
        }
        
        // Check X-Real-IP header
        var realIp = HttpContext.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp) && IsValidIpv4(realIp))
        {
            return realIp;
        }
        
        // Fall back to connection remote IP
        var remoteIp = HttpContext.Connection.RemoteIpAddress;
        if (remoteIp != null)
        {
            // Handle IPv4-mapped IPv6 addresses
            if (remoteIp.IsIPv4MappedToIPv6)
            {
                return remoteIp.MapToIPv4().ToString();
            }
            
            // Handle IPv6 loopback as IPv4 loopback
            if (remoteIp.Equals(System.Net.IPAddress.IPv6Loopback))
            {
                return "127.0.0.1";
            }
            
            // Return IPv4 addresses as-is
            if (remoteIp.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                return remoteIp.ToString();
            }
        }
        
        // Default fallback
        return "127.0.0.1";
    }
    
    private static bool IsValidIpv4(string ip)
    {
        return System.Net.IPAddress.TryParse(ip, out var addr) && 
               addr.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork;
    }
}