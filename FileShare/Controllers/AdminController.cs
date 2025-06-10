using FileShare.Models;
using FileShare.Services;
using Microsoft.AspNetCore.Mvc;

namespace FileShare.Controllers;
public class AdminController(FileService fileService) : Controller
{
    public async Task<IActionResult> Index()
    {
        var clientIp = GetClientIpAddress();
        if (!fileService.IsAdminRequest(clientIp))
        {
            return StatusCode(403, "Access denied. Admin interface only accessible from internal network.");
        }
        
        var files = await fileService.GetActiveFilesAsync();
        return View(files);
    }
    
    public IActionResult Upload()
    {
        var clientIp = GetClientIpAddress();
        if (!fileService.IsAdminRequest(clientIp))
        {
            return StatusCode(403, "Access denied.");
        }
        
        return View(new UploadViewModel());
    }
    
    [HttpPost]
    public async Task<IActionResult> Upload(UploadViewModel model)
    {
        var clientIp = GetClientIpAddress();
        if (!fileService.IsAdminRequest(clientIp))
        {
            return StatusCode(403, "Access denied.");
        }
        
        if (!ModelState.IsValid)
        {
            return View(model);
        }
        
        try
        {
            var token = await fileService.UploadFileAsync(model);
            TempData["Success"] = $"File uploaded successfully! Download link: /d/{token}";
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", $"Upload failed: {ex.Message}");
            return View(model);
        }
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