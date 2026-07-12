namespace AeroQ.Core.Exceptions;

/// <summary>
/// Задача не найдена
/// </summary>
public class TaskNotFoundException : AeroQException
{
    public Guid TaskId { get; }

    public TaskNotFoundException(Guid taskID) : base($"Task with ID {taskID} not found")
    {
        TaskId = taskID;
    }
}