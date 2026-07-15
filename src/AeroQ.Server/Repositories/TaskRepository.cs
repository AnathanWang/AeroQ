using AeroQ.Core.Contracts;
using AeroQ.Core.Enums;
using AeroQ.Core.Models;
using AeroQ.Server.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Serilog;
using ILogger = Serilog.ILogger;
using TaskStatus = AeroQ.Core.Enums.TaskStatus;

namespace AeroQ.Server.Repositories;

/// <summary>
/// Репозиторий для работы с задачами в PostgreSQL
/// </summary>
public class TaskRepository : ITaskRepository
{
    private readonly AeroQDbContext _dbContext;
    private readonly ILogger<TaskRepository> _logger;

    public TaskRepository(AeroQDbContext dbContext, ILogger<TaskRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Сохранить новую задачу в бд
    /// </summary>
    public async Task SaveAsync(TaskItem task, CancellationToken ct)
    {
        // Конвертируем TaskItem (из Core) в TaskEntity(для бд)
        var entity = new TaskEntity
        {
            Id = task.Id,
            Queue = task.Queue,
            Type = task.Type,
            Payload = task.Payload,
            Status = task.Status,
            Priority = task.Priority,
            RetryCount = task.RetryCount,
            MaxRetries = task.MaxRetries,
            Error = task.Error,
            ScheduledAt = task.ScheduledAt,
            CreatedAt = task.CreatedAt,
            CompletedAt = task.CompletedAt,
            LockedBy = task.LockedBy,
            LockedUntil = task.LockedUntil,
            IdempotencyKey = task.IdempotencyKey
        };

        _dbContext.Tasks.Add(entity);
        await _dbContext.SaveChangesAsync(ct);
        
        _logger.LogDebug("Задача {TaskId} сохранена в БД", task.Id);
    }

    public async Task<TaskItem?> DequeueAsync(string queue, string workerId, int lockTimeoutSeconds, CancellationToken ct)
    {
        var sql = @"
            UPDATE tasks
            SET status = @ProcessingStatus,
                locked_by = @WorkerId,
                locked_until = NOW() + INTERVAL '1 second' * @LockTimeout,
                started_at = NOW()
            WHERE id = (
                SELECT id FROM tasks
                WHERE queue = @Queue
                    AND status = @PendingStatus
                    AND (scheduled_at IS NULL OR scheduled_at <= NOW())
                ORDER BY priority DESC, created_at ASC
                FOR UPDATE SKIP LOCKED
                LIMIT 1
            )
            RETURNING id, queue, type, payload, status, priority, retry_count,
                      max_retries, error, scheduled_at, created_at, started_at,
                      completed_at, locked_by, locked_until, idempotency_key";

        var parameters = new[]
        {
            new Npgsql.NpgsqlParameter("@Queue", (object)queue),
            new Npgsql.NpgsqlParameter("@WorkerId", (object)workerId),
            new Npgsql.NpgsqlParameter("@PendingStatus", (object)(int)AeroQ.Core.Enums.TaskStatus.Pending),
            new Npgsql.NpgsqlParameter("@ProcessingStatus", (object)(int)TaskStatus.Processing),
            new Npgsql.NpgsqlParameter("@LockTimeout", (object)lockTimeoutSeconds)
        };

        var entities = await _dbContext.Tasks
            .FromSqlRaw(sql, parameters)
            .ToListAsync(ct);

        var entity = entities.FirstOrDefault();

        if (entity == null)
        {
            _logger.LogDebug("Очередь '{Queue}' пуста", queue);
            return null;
        }
        
        _logger.LogInformation("Воркер {WorkerId} взял задачу {TaskId} из очереди '{Queue}'", workerId, entity.Id, queue);

        return MapToTaskItem(entity);
    }

    public async Task UpdateStatusAsync(Guid taskId, AeroQ.Core.Enums.TaskStatus status, string? error = null, CancellationToken ct = default)
    {
        var entity = await _dbContext.Tasks.FindAsync(new object[] { taskId }, ct);

        if (entity == null)
        {
            _logger.LogWarning("Задача {TaskId} не найдена для обновления статуса", taskId);
            return;
        }

        entity.Status = status;
        entity.Error = error;

        if (status == AeroQ.Core.Enums.TaskStatus.Completed)
        {
            entity.CompletedAt = DateTime.UtcNow;
            entity.LockedBy = null;
            entity.LockedUntil = null;
        }
        else if (status == AeroQ.Core.Enums.TaskStatus.Failed)
        {
            entity.LockedBy = null;
            entity.LockedUntil = null;
        }

        await _dbContext.SaveChangesAsync(ct);
        
        _logger.LogDebug("Статус задачи {TaskId} обновлен на {Status}", taskId, status);
    }

    public async Task<TaskItem?> GetByIdAsync(Guid taskId, CancellationToken ct)
    {
        var entity = await _dbContext.Tasks.FindAsync(new object[] { taskId }, ct);

        return entity == null ? null : MapToTaskItem(entity);
    }

    public async Task IncrementRetryCountAsync(Guid taskId, string error, CancellationToken ct)
    {
        var entity = await _dbContext.Tasks.FindAsync(new object[] { taskId }, ct);

        if (entity == null)
        {
            _logger.LogWarning("Задача {TaskId} не найдена для увеличения retry count", taskId);
            return;
        }

        entity.RetryCount++;
        entity.Error = error;
        entity.Status = TaskStatus.Pending;
        entity.LockedBy = null;
        entity.LockedUntil = null;

        await _dbContext.SaveChangesAsync(ct);
        
        _logger.LogInformation("Задача {TaskId} вернулась в очередь. Retry #{RetryCount}", taskId, entity.RetryCount);
    }

    public async Task MoveToDeadLetterAsync(Guid taskId, string error, CancellationToken ct)
    {
        var entity = await _dbContext.Tasks.FindAsync(new object[] { taskId }, ct);

        if (entity == null)
        {
            _logger.LogWarning("Задача {TaskId} не найдена для перемещения в dead letter", taskId);
            return;
        }

        var deadLetter = new DeadLetterEntity
        {
            OriginalTaskId = entity.Id,
            Queue = entity.Queue,
            Type = entity.Type,
            Payload = entity.Payload,
            Error = error,
            RetryCount = entity.RetryCount,
            FailedAt = DateTime.UtcNow
        };

        _dbContext.DeadLetters.Add(deadLetter);

        _dbContext.Tasks.Remove(entity);

        await _dbContext.SaveChangesAsync(ct);
        
        _logger.LogWarning("Задача {TaskId} перемещена в dead letter queue", taskId);
    }

    public async Task ReleaseExpiredLocksAsync(CancellationToken ct)
    {
        var sql = @"
            UPDATE tasks
            SET status = @PendingStatus,
                locked_by = NULL,
                locked_until = NULL
            WHERE status = @ProcessingStatus
              AND locked_until < NOW()";

        var parameters = new object[]
        {
            new Npgsql.NpgsqlParameter("@PendingStatus", (object)(int)AeroQ.Core.Enums.TaskStatus.Pending),
            new Npgsql.NpgsqlParameter("@ProcessingStatus", (object)(int)AeroQ.Core.Enums.TaskStatus.Processing)
        };

        var affectedRows = await _dbContext.Database.ExecuteSqlRawAsync(sql, parameters, ct);

        if (affectedRows > 0)
        {
            _logger.LogWarning("Освобождено {Count} потерянных задач", affectedRows);
        }
    }

    public async Task CleanupAsync(DateTime olderThan, CancellationToken ct)
    {
        var oldTasks = await _dbContext.Tasks.Where(t => t.Status == TaskStatus.Completed && t.CompletedAt < olderThan)
            .ToListAsync(ct);
        
        _dbContext.Tasks.RemoveRange(oldTasks);
        await _dbContext.SaveChangesAsync(ct);
        
        _logger.LogInformation("Удалено {Count} старых завершенных задач", oldTasks.Count);
    }

    private static TaskItem MapToTaskItem(TaskEntity entity)
    {
        return new TaskItem
        {
            Id = entity.Id,
            Queue = entity.Queue,
            Type = entity.Type,
            Payload = entity.Payload,
            Status = entity.Status,
            Priority = entity.Priority,
            RetryCount = entity.RetryCount,
            MaxRetries = entity.MaxRetries,
            Error = entity.Error,
            ScheduledAt = entity.ScheduledAt,
            CreatedAt = entity.CreatedAt,
            CompletedAt = entity.CompletedAt,
            LockedBy = entity.LockedBy,
            LockedUntil = entity.LockedUntil,
            IdempotencyKey = entity.IdempotencyKey
        };
    }
}