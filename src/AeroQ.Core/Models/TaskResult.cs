namespace AeroQ.Core.Models;

/// <summary>
/// Результат dequeue операции (получения задачи из очереди)
/// </summary>
public class TaskResult
{
    /// <summary>
    /// Задача (null, если очередь пуста)
    /// </summary>
    public TaskItem? Task { get; set; }

    /// <summary>
    /// Успешно ли получена задача
    /// </summary>
    public bool Success => Task != null;
}