using AeroQ.Client;
using AeroQ.Core.Models;

AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

Console.WriteLine("=== 🚀 AeroQ Sample Producer ===\n");

var client = new AeroQClient(new AeroQClientOptions
{
    ServerUrl = "http://localhost:5050"
});

var payload = new EmailPayload
{
    To = "user@example.com",
    Subject = "Привет из AeroQ!",
    Body = "Это тестовая задача, которая должна попасть в PostgreSQl"
};

Console.WriteLine("📤 Отправляю задачу в очередь 'emails'...");

try
{
    var taskId = await client.EnqueueAsync("emails", payload, new EnqueueOptions
    {
        Priority = AeroQ.Core.Enums.Priority.High,
        MaxRetries = 3
    });

    Console.WriteLine($"✅ Задача успешно отправлена!");
    Console.WriteLine($"   🆔 ID задачи: {taskId}");
    Console.WriteLine($"   📥 Очередь: emails");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Ошибка: {ex.Message}");
    Console.WriteLine("\n💡 Подсказка: Убедись, что AeroQ.Server запущен (dotnet run) и PostgreSQL работает в Docker.");
}

// Простой класс для сериализации в JSON
public class EmailPayload
{
    public string To { get; set; } = "";
    public string Subject { get; set; } = "";
    public string Body { get; set; } = "";
}