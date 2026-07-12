namespace AeroQ.Client;

/// <summary>
/// Опции конфигурации клиента AeroQ
/// </summary>
public class AeroQClientOptions
{
    /// <summary>
    /// URL сервера AeroQ (например, "http://localhost:5000")
    /// </summary>
    public string ServerUrl { get; set; } = "http://localhost:5000";
    
    /// <summary>
    /// API ключ для аутентификации (опциально)
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Таймаут запросов в секундах
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;
}
