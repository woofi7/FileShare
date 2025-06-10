namespace FileShare.Services;

public class CleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CleanupService> _logger;
    private readonly TimeSpan _interval;
    
    public CleanupService(IServiceProvider serviceProvider, ILogger<CleanupService> logger, IConfiguration config)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        var hours = config.GetValue("FileShare:CleanupIntervalHours", 6);
        _interval = TimeSpan.FromHours(hours);
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var fileService = scope.ServiceProvider.GetRequiredService<FileService>();
                
                await fileService.CleanupExpiredFilesAsync();
                _logger.LogInformation("Cleanup completed at {Time}", DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during cleanup");
            }
            
            await Task.Delay(_interval, stoppingToken);
        }
    }
}