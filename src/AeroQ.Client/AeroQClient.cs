using System.Text.Json;
using AeroQ.Client.Extentions;
using AeroQ.Core.Contracts;
using AeroQ.Core.Enums;
using AeroQ.Core.Models;
using AeroQ.Core.Exceptions;
using AeroQ.Core.Protos;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;

namespace AeroQ.Client;

/// <summary>
/// Клиента для отправки задач в очередь AeroQ
/// </summary>
public class AeroQClient : IAeroQClient, IDisposable
{
    private readonly GrpcChannel _channel;
    private readonly AeroQService.AeroQServiceClient _grpcClient;
    private readonly AeroQClientOptions _options;

    /// <summary>
    /// Создать новый экземпляр клиента
    /// </summary>
    public AeroQClient(AeroQClientOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));

        // Создаем grpc канал
        _channel = GrpcChannel.ForAddress(options.ServerUrl);

        // Создаем grpc клиент
        _grpcClient = new AeroQService.AeroQServiceClient(_channel);
    }

    public async Task<Guid> EnqueueAsync<T>(string queue, T payload, EnqueueOptions? options = null)
    {
        options ??= new EnqueueOptions();
        
        //Сериалиазуем payload в Json
        var payloadJson = JsonSerializer.Serialize(payload);

        //Создаем grpc запрос
        var request = new EnqueueRequest
        {
            Queue = queue,
            Type = typeof(T).Name,
            Payload = payloadJson,
            Priority = (int)options.Priority,
            MaxRetries = options.MaxRetries,
            IdempotencyKey = options.IdempotencyKey ?? string.Empty
        };
        
        //Если указана отложенная задача
        if (options.ScheduledAt.HasValue)
        {
            request.ScheduledAt = options.ScheduledAt.Value.ToTimestamp();
        }
        
        //отправляем запрос на сервер
        var response = await _grpcClient.EnqueueAsync(request);
        
        //проверяем ответ
        if (!response.Success)
        {
            throw new AeroQException($"Failed to enqueue task: {response.ErrorMessage}");
        }

        return Guid.Parse(response.TaskId);
    }

    /// <summary>
    /// Отменить задачу
    /// </summary>
    public async Task<bool> CancelAsync(Guid taskId)
    {
        var request = new CancelRequest
        {
            TaskId = taskId.ToString()
        };

        var response = await _grpcClient.CancelAsync(request);
        return response.Success;
    }

    /// <summary>
    /// Получить статус задачи
    /// </summary>
    public async Task<TaskItem?> GetStatusAsync(Guid taskId)
    {
        var request = new GetStatusRequest
        {
            TaskId = taskId.ToString()
        };

        var response = await _grpcClient.GetStatusAsync(request);

        if (!response.Found || response.Task == null)
        {
            return null;
        }

        return response.Task.ToTaskItem();
    }

    public void Dispose()
    {
        _channel?.Dispose();
    }
}