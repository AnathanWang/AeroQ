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
    Task<TaskItem?> DequeueAsync(string queue, string workerId, CancellationToken ct);

    /// <summary>
    /// Обновить статус задачи
    /// </summary>
    Task UpdateStatusAsync(Guid taskId, TaskStatus status, string? error = null, CancellationToken ct = default);

    /// <summary>
    /// Получить задачу по ID
    /// </summary>
    Task<TaskItem?> GetByIdAsync(Guid taskId, CancellationToken ct);

    /// <summary>
    /// Удалить завершенные задачи старше указанного возраста
    /// </summary>
    Task CleanupAsync(DateTime olderThan, CancellationToken ct);
}