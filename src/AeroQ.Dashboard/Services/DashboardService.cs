using AeroQ.Core.Enums;
using AeroQ.Dashboard.Data;
using Microsoft.EntityFrameworkCore;

namespace AeroQ.Dashboard.Services;

public class DashboardService
{
    private readonly DashboardDbContext _dbContext;

    public DashboardService(DashboardDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<DashboardStats> GetStatsAsync()
    {
        var stats = new DashboardStats
        {
            TotalTasks = await _dbContext.Tasks.CountAsync(),
            PendingTasks = await _dbContext.Tasks.CountAsync(t => t.Status == AeroQ.Core.Enums.TaskStatus.Pending),
            ProcessingTasks =
                await _dbContext.Tasks.CountAsync(t => t.Status == AeroQ.Core.Enums.TaskStatus.Processing),
            CompletedTasks = await _dbContext.Tasks.CountAsync(t => t.Status == AeroQ.Core.Enums.TaskStatus.Completed),
            FailedTasks = await _dbContext.Tasks.CountAsync(t => t.Status == AeroQ.Core.Enums.TaskStatus.Failed),
            ScheduledTasks = await _dbContext.Tasks.CountAsync(t => t.Status == AeroQ.Core.Enums.TaskStatus.Scheduled),
            DeadLetterCount =
                await _dbContext.Tasks.CountAsync(t => t.Status == AeroQ.Core.Enums.TaskStatus.DeadLetter),

            TasksLast24Hours = await _dbContext.Tasks.CountAsync(t => t.CreatedAt >= DateTime.UtcNow.AddHours(-24)),

            UniqueQueues = await _dbContext.Tasks.Select(t => t.Queue).Distinct().CountAsync()
        };
        return stats;
    }

    public async Task<List<QueueStats>> GetQueueStatsAsync()
    {
        return await _dbContext.Tasks.GroupBy(t => t.Queue).Select(g => new QueueStats
            {
                Queue = g.Key,
                Total = g.Count(),
                Pending = g.Count(t => t.Status == AeroQ.Core.Enums.TaskStatus.Pending),
                Processing = g.Count(t => t.Status == AeroQ.Core.Enums.TaskStatus.Processing),
                Completed = g.Count(t => t.Status == AeroQ.Core.Enums.TaskStatus.Completed),
                Failed = g.Count(t => t.Status == AeroQ.Core.Enums.TaskStatus.Failed)
            })
            .OrderByDescending(q => q.Total).ToListAsync();
    }

    public async Task<List<DeadLetterEntity>> GetDeadLettersAsync(int limit = 50)
    {
        return await _dbContext.DeadLetters.OrderByDescending(d => d.FailedAt).Take(limit).ToListAsync();
    }
    
    public async Task<List<TaskEntity>> GetTasksAsync(
        AeroQ.Core.Enums.TaskStatus? status = null,  // ← Должно быть полное имя!
        string? queue = null,
        int limit = 50)
    {
        var query = _dbContext.Tasks.AsQueryable();

        if (status.HasValue)
            query = query.Where(t => t.Status == status.Value);

        if (!string.IsNullOrEmpty(queue))
            query = query.Where(t => t.Queue == queue);

        return await query
            .OrderByDescending(t => t.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }
}

public class DashboardStats
{
    public int TotalTasks { get; set; }
    public int PendingTasks { get; set; }
    public int ProcessingTasks { get; set; }
    public int CompletedTasks { get; set; }
    public int FailedTasks { get; set; }
    public int ScheduledTasks { get; set; }
    public int DeadLetterCount { get; set; }
    public int TasksLast24Hours { get; set; }
    public int UniqueQueues { get; set; }
}

public class QueueStats
{
    public string Queue { get; set; } = "";
    public int Total { get; set; }
    public int Pending { get; set; }
    public int Processing { get; set; }
    public int Completed { get; set; }
    public int Failed { get; set; }
}