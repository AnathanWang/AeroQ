using AeroQ.Core.Enums;
using TaskStatus = AeroQ.Core.Enums.TaskStatus;

namespace AeroQ.Core.Models;

/// <summary>
/// Задача в очереди AeroQ
/// </summary>
public class TaskItem
{
    /// <summary>
    /// Уникальный идентификатор задачи
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Имя очереди (например "emails", "payments")
    /// </summary>
    public string Queue { get; set; } = "default";

    /// <summary>
    /// Тип задачи (имя обработчика, например "SendEmailHandler")
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Текущий статус задачи
    /// </summary>
    public string Payload { get; set; } = "{}";

    /// <summary>
    /// Текущий статус задачи
    /// </summary>
    public AeroQ.Core.Enums.TaskStatus Status { get; set; } = AeroQ.Core.Enums.TaskStatus.Pending;

    /// <summary>
    /// Приоритет задачи (0-10 чем выше тем важнее)
    /// </summary>
    public int Priority { get; set; } = (int)AeroQ.Core.Enums.Priority.Normal;

    /// <summary>
    /// Сколько раз задача уже выполнялась
    /// </summary>
    public int RetryCount { get; set; } = 0;

    /// <summary>
    /// Максимальное кол-во попыток выполнения
    /// </summary>
    public int MaxRetries { get; set; } = 3;
    
    /// <summary>
    /// Текст последней ошибки(если была)
    /// </summary>
    public  string? Error { get; set; }
    
    /// <summary>
    /// Когда задача должна быть выполнена (для отложенных задач)
    /// </summary>
    public DateTime? ScheduledAt { get; set; }

    /// <summary>
    /// Когда задача была создана
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Когда задача начала выполнятся
    /// </summary>
    public DateTime? StartedAt { get; set; }
    
    /// <summary>
    /// Когда задача была завершена
    /// </summary>
    public DateTime? CompletedAt { get; set; }
    
    /// <summary>
    /// ID Воркера котоырй взял задачу (для блокировки)
    /// </summary>
    public string? LockedBy { get; set; }
    
    /// <summary>
    /// до какого времени задача заблокирована (если воркер упал, задача вернется в очередь)
    /// </summary>
    public DateTime? LockedUntil { get; set; }
    
    public string? IdempotencyKey { get; set; }
}