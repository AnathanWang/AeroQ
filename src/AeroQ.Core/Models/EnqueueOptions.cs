using AeroQ.Core.Enums;

namespace AeroQ.Core.Models;

/// <summary>
/// Опции для отправки задачи в очередь
/// </summary>
public class EnqueueOptions
{
    /// <summary>
    /// Приоритет задачи (по умолчанию Normal)
    /// </summary>
    public Priority Priority { get; set; } = Priority.Normal;

    /// <summary>
    /// Максимальное кол-во попыток по умолчанию
    /// </summary>
    public int MaxRetries { get; set; } = 3;
    
    /// <summary>
    /// Когда задача должна быть выполнена (для отложенных задач)
    /// </summary>
    public DateTime? ScheduledAt { get; set; }
    
    /// <summary>
    /// Уникальный ключ для идемпотентности (если задача уже есть не создаем дубликат)
    /// </summary>
    public string? IdempotencyKey { get; set; }
}