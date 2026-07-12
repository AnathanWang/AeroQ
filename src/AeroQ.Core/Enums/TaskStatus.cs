namespace AeroQ.Core.Enums;

/// <summary>
/// Статус задачи в очереди
/// </summary>
public enum TaskStatus
{
    /// <summary>
    /// Задача создана и ожидает выполнения
    /// </summary>
    Pending = 0,
    
    /// <summary>
    /// Задача запланирована на будущее (scheduled_at > now)
    /// </summary>
    Scheduled = 1,
    
    /// <summary>
    /// Задача сейчас выполняется воркером
    /// </summary>
    Processing = 2,
    
    /// <summary>
    /// Задача успешно завершена
    /// </summary>
    Completed = 3,
    
    /// <summary>
    /// Задача провалена после всех попыток retry
    /// </summary>
    Failed = 4,
    
    /// <summary>
    /// Задача перемещена в dead letter queue (не может быть выполнена)
    /// </summary>
    DeadLetter = 5
}