using AeroQ.Core.Models;

namespace AeroQ.Core.Contracts;

/// <summary>
/// Клиент для отправки задач в очередь AeroQ
/// </summary>
public interface IAeroQClient
{
    /// <summary>
    /// Отправить задачу в очередь
    /// </summary>
    /// <param name="queue">Имя очереди</param>
    /// <param name="payload">Данные задачи (будут сериализованы в JSON)</param>
    /// <param name="options">Опции (приоритет, retry, scheduled_at</param>
    /// <typeparam name="T"></typeparam>
    /// <returns>ID Созданной задачи</returns>
    Task<Guid> EnqueueAsync<T>(string queue, T payload, EnqueueOptions? options = null);

    /// <summary>
    /// Отменить задачу (если она еще не выполняется)
    /// </summary>
    /// <param name="taskId">ID Задачи</param>
    /// <returns>true если задача отменена</returns>
    Task<bool> CancelAsync(Guid taskId);

    /// <summary>
    /// Получить статус задачи
    /// </summary>
    /// <param name="taskId">ID Задачи</param>
    /// <returns>Задача или null, если не найдена</returns>
    Task<TaskItem?> GetStatusAsync(Guid taskId);
}