using AeroQ.Core.Models;

namespace AeroQ.Core.Contracts;

/// <summary>
/// Репозиторий для хранения задач (реализуется в Server)
/// </summary>
public interface ITaskRepository
{
    /// <summary>
    /// Сохранить новую задачу
    /// </summary>
    Task SaveAsync(TaskItem task, CancellationToken ct);

    /// <summary>
    /// Получить задачу из очереди (atomic dequeue)
    /// </summary>
    Task<TaskItem?> DequeueAsync(string queue, string workerId, int lockTimeoutSeconds, CancellationToken ct);

    /// <summary>
    /// Обновить статус задачи
    /// </summary>
    Task UpdateStatusAsync(Guid taskId, AeroQ.Core.Enums.TaskStatus status, string? error = null, CancellationToken ct = default);

    /// <summary>
    /// Получить задачу по ID
    /// </summary>
    Task<TaskItem?> GetByIdAsync(Guid taskId, CancellationToken ct);

    /// <summary>
    /// Увеличить счетчик retry и вернуть задачу в очередь
    /// </summary>
    Task IncrementRetryCountAsync(Guid taskId, string error, CancellationToken ct);

    /// <summary>
    /// Переместить задачу в dead letter queue (если все попытки исчерпаны)
    /// </summary>
    Task MoveToDeadLetterAsync(Guid taskId, string error, CancellationToken ct);

    /// <summary>
    /// Освободить потерянные задачи (если воркер упал и не снял блокировку)
    /// </summary>
    Task ReleaseExpiredLocksAsync(CancellationToken ct);

    /// <summary>
    /// Удалить завершенные задачи старше указанного возраста
    /// </summary>
    Task CleanupAsync(DateTime olderThan, CancellationToken ct);
}