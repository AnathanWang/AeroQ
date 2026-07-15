using AeroQ.Core.Contracts;
using AeroQ.Server.Data;
using AeroQ.Server.Options;
using AeroQ.Server.Repositories;
using AeroQ.Server.Services;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

// 2. Используем Serilog как логгер
builder.Host.UseSerilog();

builder.Services.AddDbContext<AeroQDbContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("AeroQDb")));

builder.Services.AddScoped<ITaskRepository, TaskRepository>();

builder.Services.AddGrpc();

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5050, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http2;
    });
});

var app = builder.Build();

app.MapGet("/", ()=> "AeroQ Server is running! Use a gRPC client to connect.");

// TODO: Расскоментируй эту строку когда сделаем класс AeroQServectImpl
app.MapGrpcService<AeroQServiceImpl>();

Log.Information("Запуск AeroQ Server...");
app.Run();