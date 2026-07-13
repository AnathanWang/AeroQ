namespace AeroQ.Server.Options;

/// <summary>
/// Опции для конфигурации AeroQ Server
/// </summary>
public class AeroQOptions
{
    public const string SectionName = "AeroQ";

    /// <summary>
    /// На сколько секунд блокируется задача при выполнении воркером
    /// </summary>
    public int TaskLockTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Через сколько часов удалять завершенные задачи
    /// </summary>
    public int MaxCleanupAgeHours { get; set; } = 24;
}