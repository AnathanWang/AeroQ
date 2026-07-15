using AeroQ.Core.Contracts;
using AeroQ.Server.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;

namespace AeroQ.Server.BackgroundServices;

/// <summary>
/// Фоновый сервис для обслуживания очереди:
/// 1. Освобождает "зависшие" задачи (если воркер упал)
/// 2. Очищает старые завершенные задачи
/// </summary>
public class TaskMaintenanceBackgroundService : BackgroundService
{
    private readonly AeroQOptions _options;
    private readonly ILogger<TaskMaintenanceBackgroundService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public TaskMaintenanceBackgroundService(IServiceScopeFactory scopeFactory, IOptions<AeroQOptions> options,
        ILogger<TaskMaintenanceBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🔧 Task Maintenance Service запущен.");
        
        using var periodicTimer = new PeriodicTimer(TimeSpan.FromSeconds(30));
    
        while (!stoppingToken.IsCancellationRequested && await periodicTimer.WaitForNextTickAsync(stoppingToken)){
            try
            {
                _logger.LogDebug("🔄 Запуск цикла обслуживания задач...");

                using var scope = _scopeFactory.CreateScope();
                var repository = scope.ServiceProvider.GetRequiredService<ITaskRepository>();

                await repository.ReleaseExpiredLocksAsync(stoppingToken);

                var cleanupThreshold = DateTime.UtcNow.AddHours(-_options.MaxCleanupAgeHours);
                await repository.CleanupAsync(cleanupThreshold, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Ошибка во время обслуживания задач");
            }
        }
        
        _logger.LogInformation("🛑 Task Maintenance Service остановлен.");
    }
}