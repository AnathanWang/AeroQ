using System;
using System.Threading.Tasks;
using AeroQ.Client;
using AeroQ.Core.Models;

// Разрешаем незашифрованный HTTP/2
AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

Console.WriteLine("=== AeroQ Sample Worker ===\n");

// Явное указание типов помогает компилятору
AeroQClientOptions options = new AeroQClientOptions
{
    ServerUrl = "http://localhost:5050"
};

AeroQClient client = new AeroQClient(options);

string workerId = "worker-" + Guid.NewGuid().ToString("N");
Console.WriteLine("My Worker ID: " + workerId);
Console.WriteLine("Polling queue 'emails'...\n");

while (true)
{
    try
    {
        // 1. Запрашиваем задачу
        TaskItem? task = await client.DequeueAsync("emails", workerId);

        if (task == null)
        {
            Console.WriteLine("Queue is empty. Waiting 3 seconds...");
            await Task.Delay(3000);
            continue;
        }

        // 2. Явный вывод без интерполяции, чтобы избежать Ambiguous invocation
        string logMsg = "Got task: " + task.Id.ToString() + 
                        " | Queue: " + task.Queue + 
                        " | Type: " + task.Type;
        Console.WriteLine(logMsg);
        Console.WriteLine("Payload: " + task.Payload);

        // 3. Имитация работы
        Console.WriteLine("Simulating work (2 sec)...");
        await Task.Delay(2000);

        // 4. Сообщаем об успехе
        Console.WriteLine("Task " + task.Id.ToString() + " completed successfully!\n");
        await client.CompleteAsync(task.Id);

        Console.WriteLine("Continuing to poll...\n");
    }
    catch (Exception ex)
    {
        Console.WriteLine("Error: " + ex.Message);
        await Task.Delay(3000);
    }
}