using AeroQ.Core.Enums;
using AeroQ.Core.Models;
using AeroQ.Core.Protos;
using Google.Protobuf.WellKnownTypes;
using TaskStatus = AeroQ.Core.Enums.TaskStatus;

namespace AeroQ.Client.Extentions;

/// <summary>
/// Extentions методы для конвертации между proto и моделями
/// </summary>
public static class ProtoExtentions
{
    /// <summary>
    /// Конвертировать DateTime в TimeStamp (для отправки на сервер)
    /// </summary>
    public static Timestamp ToTimeStamp(this DateTime dateTime)
    {
        return Timestamp.FromDateTime(dateTime.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(dateTime, DateTimeKind.Utc)
            : dateTime.ToUniversalTime());
    }

    /// <summary>
    /// Конвертировать TaskItemProto в TaskItem (для получения ответа от сервера)
    /// </summary>
    public static TaskItem ToTaskItem(this TaskItemProto proto)
    {
        return new TaskItem
        {
            Id = Guid.Parse(proto.Id),
            Queue = proto.Queue,
            Type = proto.Type,
            Payload = proto.Payload,
            Status = (TaskStatus)proto.Status,
            Priority = proto.Priority,
            RetryCount = proto.RetryCount,
            MaxRetries = proto.MaxRetries,
            Error = string.IsNullOrEmpty(proto.Error) ? null : proto.Error,
            //ToDateTime() встроенный методв Google.Protobuf не нужно писать свой
            ScheduledAt = proto.ScheduledAt == null ? null : proto.ScheduledAt.ToDateTime(),
            CreatedAt = proto.CreatedAt.ToDateTime(),
            StartedAt = proto.StartedAt == null ? null : proto.StartedAt.ToDateTime(),
            CompletedAt = proto.CompletedAt == null ? null : proto.CompletedAt.ToDateTime(),
            LockedBy = string.IsNullOrEmpty(proto.LockedBy) ? null : proto.LockedBy,
            LockedUntil = proto.LockedUntil == null ? null : proto.LockedUntil.ToDateTime()
        };
    }
    public static TaskItemProto ToProto(this TaskItem task)
    {
        var proto = new TaskItemProto
        {
            Id = task.Id.ToString(),
            Queue = task.Queue,
            Type = task.Type,
            Payload = task.Payload,
            Status = (int)task.Status,
            Priority = task.Priority,
            RetryCount = task.RetryCount,
            MaxRetries = task.MaxRetries,
            Error = task.Error ?? string.Empty,
            CreatedAt = task.CreatedAt.ToTimestamp(),
            LockedBy = task.LockedBy ?? string.Empty
        };

        if (task.ScheduledAt.HasValue) proto.ScheduledAt = task.ScheduledAt.Value.ToTimestamp();
        if (task.StartedAt.HasValue) proto.StartedAt = task.StartedAt.Value.ToTimestamp();
        if (task.CompletedAt.HasValue) proto.CompletedAt = task.CompletedAt.Value.ToTimestamp();
        if (task.LockedUntil.HasValue) proto.LockedUntil = task.LockedUntil.Value.ToTimestamp();

        return proto;
    }
}