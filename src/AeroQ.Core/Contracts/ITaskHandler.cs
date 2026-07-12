namespace AeroQ.Core.Contracts;

/// <summary>
/// Интерфейс для обработчика задач определнного типа
/// </summary>
/// <typeparam name="TPayload">Тип данных задачи</typeparam>
public interface ITaskHandler <TPayload>
{
    /// <summary>
    /// Обработать задачу
    /// </summary>
    /// <param name="payload">Данные задачи</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Task для async/await</returns>
    Task HandleAsync(TPayload payload, CancellationToken cancellationToken);
}