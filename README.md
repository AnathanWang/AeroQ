# 🚀 AeroQ — Распределенная очередь задач

Высокопроизводительная система очередей на .NET 9 с gRPC и PostgreSQL. Аналог RabbitMQ/Celery с фокусом на надежность.

## Возможности

- ✅ **Гарантированная доставка** — задачи не теряются при падении воркера
- 🔄 **Retry механизм** — автоматические повторные попытки с Dead Letter Queue
- ⚡ **Приоритеты** — важные задачи выполняются первыми
-  **Отложенные задачи** — планирование на определенное время
- 🔒 **Атомарные операции** — `FOR UPDATE SKIP LOCKED` для безопасного конкурентного доступа
- 📊 **Веб-дашборд** — мониторинг в реальном времени на Blazor + MudBlazor
- ️ **Автовосстановление** — фоновый сервис возвращает "зависшие" задачи в очередь

## Архитектура

```
Producer → gRPC → AeroQ Server → PostgreSQL
                                    ↓
Worker ← gRPC ← Task Repository ← Background Service
                                    ↓
Dashboard ← HTTP ← Blazor UI
```

## Быстрый старт

```bash
# 1. Запуск PostgreSQL
docker-compose up -d

# 2. Миграции БД
cd src/AeroQ.Server
dotnet ef database update

# 3. Запуск сервера (порт 5050)
dotnet run

# 4. Запуск Dashboard (порт 5001)
cd ../AeroQ.Dashboard
dotnet run

# 5. Тестовая задача
cd ../../samples/SampleProducer
dotnet run

# 6. Обработка задачи
cd ../SampleWorker
dotnet run
```

## Пример использования

### Producer
```csharp
var client = new AeroQClient(new AeroQClientOptions
{
    ServerUrl = "http://localhost:5050"
});

var taskId = await client.EnqueueAsync("emails", new EmailPayload
{
    To = "user@example.com",
    Subject = "Привет!"
}, new EnqueueOptions { Priority = Priority.High, MaxRetries = 3 });
```

### Worker
```csharp
var task = await client.DequeueAsync("emails", workerId);

try
{
    await ProcessTask(task);
    await client.CompleteAsync(task.Id);
}
catch (Exception ex)
{
    await client.FailAsync(task.Id, ex.Message);
}
```

## Технологии

- **.NET 9** — платформа
- **gRPC** — коммуникация
- **PostgreSQL 17** — база данных
- **EF Core 9** — ORM
- **Blazor + MudBlazor** — веб-дашборд
- **Docker** — контейнеризация

## Структура проекта

```
AeroQ/
├── src/
│   ├── AeroQ.Core/          # Контракты, модели, proto
│   ├── AeroQ.Server/        # gRPC сервер + БД
│   ├── AeroQ.Client/        # Клиентский SDK
│   └── AeroQ.Dashboard/     # Веб-интерфейс
├── samples/
│   ├── SampleProducer/      # Пример отправки
│   └── SampleWorker/        # Пример обработки
└── docker-compose.yml
```

## Как это работает

**Атомарное извлечение задач:**
```sql
UPDATE tasks SET status = 2, locked_by = 'worker-1'
WHERE id = (
    SELECT id FROM tasks WHERE queue = 'emails' AND status = 0
    ORDER BY priority DESC, created_at ASC
    FOR UPDATE SKIP LOCKED LIMIT 1
)
```

**Retry логика:**
1. Worker берет задачу → статус `Processing`
2. При ошибке → `FailAsync`
3. Если `RetryCount < MaxRetries` → возврат в `Pending`
4. Иначе → перемещение в `dead_letters`

**Автовосстановление:**
Фоновый сервис каждые 30 сек проверяет задачи с `locked_until < NOW()` и возвращает их в очередь.

## Roadmap

- [ ] Prometheus + Grafana метрики
- [ ] Rate limiting на очереди
- [ ] Batch operations
- [ ] Redis кэширование
- [ ] Kubernetes деплой

---

**Автор:** Nathan Karma  
**Лицензия:** MIT

<p align="center">Сделано с ❤️ на .NET 9</p>
