using AeroQ.Core.Contracts;
using AeroQ.Core.Enums;
using AeroQ.Core.Models;
using AeroQ.Core.Protos;
using AeroQ.Server.Options;
using Grpc.Core;
using Microsoft.Extensions.Options;
using Serilog;
using ILogger = Serilog.ILogger;
using TaskStatus = AeroQ.Core.Enums.TaskStatus;
using System.Threading;

namespace AeroQ.Server.Services;

/// <summary>
/// Реализация gRPC сервиса AeroQ
/// </summary>
public class AeroQServiceImpl : AeroQService.AeroQServiceBase
{
    private readonly ITaskRepository _repository;
    private readonly AeroQOptions _options;
    private readonly ILogger<AeroQServiceImpl> _logger;

    public AeroQServiceImpl(ITaskRepository repository, IOptions<AeroQOptions> options,
        ILogger<AeroQServiceImpl> logger)
    {
        _repository = repository;
        _options = options.Value;
        _logger = logger;
    }

    public override async Task<EnqueueResponse> Enqueue(EnqueueRequest request, ServerCallContext context)
    {
        try
        {
            var task = new TaskItem
            {
                Queue = request.Queue,
                Type = request.Type,
                Payload = request.Payload,
                Priority = request.Priority,
                MaxRetries = request.MaxRetries,
                IdempotencyKey = string.IsNullOrEmpty(request.IdempotencyKey) ? null : request.IdempotencyKey,
                ScheduledAt = request.ScheduledAt?.ToDateTime(),
                Status = request.ScheduledAt != null ? TaskStatus.Scheduled : TaskStatus.Pending
            };

            await _repository.SaveAsync(task, context.CancellationToken);

            _logger.LogInformation("Задача {TaskId} успешно поставлена в очередь '{Queue}'", task.Id, task.Queue);

            return new EnqueueResponse
            {
                TaskId = task.Id.ToString(),
                Success = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при постановке задачи в очередь");
            return new EnqueueResponse
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public override async Task<DequeueResponse> Dequeue(DequeueRequest request, ServerCallContext context)
    {
        try
        {
            var task = await _repository.DequeueAsync(request.Queue, request.WorkerId, _options.TaskLockTimeoutSeconds,
                context.CancellationToken);

            if (task == null)
            {
                return new DequeueResponse { Success = false };
            }

            return new DequeueResponse
            {
                Success = true,
                Task = MapToProto(task)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении задачи из очереди '{Queue}'", request.Queue);
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    public override async Task<Google.Protobuf.WellKnownTypes.Empty> Complete(CompleteRequest request,
        ServerCallContext context)
    {
        try
        {
            if (!Guid.TryParse(request.TaskId, out var taskId))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid TaskId"));

            await _repository.UpdateStatusAsync(taskId, TaskStatus.Completed,
                ct: context.CancellationToken);
            _logger.LogInformation("Задача {TaskId} успешно завершена", taskId);

            return new Google.Protobuf.WellKnownTypes.Empty();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка в завершении задачи {TaskId}", request.TaskId);
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    public override async Task<Google.Protobuf.WellKnownTypes.Empty> Fail(FailRequest request,
        ServerCallContext context)
    {
        try
        {
            if (!Guid.TryParse(request.TaskId, out var taskId))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid TaskId"));

            var task = await _repository.GetByIdAsync(taskId, context.CancellationToken);
            if (task == null)
                throw new RpcException(new Status(StatusCode.NotFound, "Task not found"));

            if (task.RetryCount < task.MaxRetries)
            {
                await
                    _repository.IncrementRetryCountAsync(taskId, request.Error, context.CancellationToken);
                _logger.LogWarning("Задача {TaskId} провалилась. Повторная попытка #{RetryCount}/{MaxRetries}", taskId,
                    task.RetryCount + 1, task.MaxRetries);

            }
            else
            {
                await
                    _repository.MoveToDeadLetterAsync(taskId, request.Error, context.CancellationToken);
                _logger.LogError("Задача {TaskId} исчерпала все попытки и перемещена в Dead Letter Queue", taskId);
            }

            return new Google.Protobuf.WellKnownTypes.Empty();
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при обработке fail для задачи {TaskID}", request.TaskId);
            throw new RpcException(new Status(StatusCode.Internal, "internal server error"));
        }
    }

    public override async Task<CancelResponse> Cancel(CancelRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.TaskId, out var taskId))
            return new CancelResponse { Success = false };

        await _repository.UpdateStatusAsync(taskId, TaskStatus.Failed, "Cancceled by user", context.CancellationToken);
        return new CancelResponse { Success = true };
    }

    public override async Task<GetStatusResponse> GetStatus(GetStatusRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.TaskId, out var taskId))
            return new GetStatusResponse { Found = false };

        var task = await _repository.GetByIdAsync(taskId, context.CancellationToken);

        if (task == null)
            return new GetStatusResponse { Found = false };

        return new GetStatusResponse
        {
            Found = true,
            Task = MapToProto(task)
        };
    }

    private static TaskItemProto MapToProto(TaskItem task)
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
            CreatedAt = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(task.CreatedAt.ToUniversalTime()),
            LockedBy = task.LockedBy ?? string.Empty
        };

        if (task.ScheduledAt.HasValue)
            proto.ScheduledAt =
                Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(task.ScheduledAt.Value.ToUniversalTime());
        
        if (task.StartedAt.HasValue)
            proto.StartedAt =
                Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(task.StartedAt.Value.ToUniversalTime());
        
        if (task.CompletedAt.HasValue)
            proto.CompletedAt =
                Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(task.CompletedAt.Value.ToUniversalTime());
        
        if (task.LockedUntil.HasValue)
            proto.LockedUntil =
                Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(task.LockedUntil.Value.ToUniversalTime());

        return proto;
    }
}