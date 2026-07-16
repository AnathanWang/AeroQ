using AeroQ.Dashboard.Data;
using AeroQ.Dashboard.Services;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Добавляем Razor Components и интерактивный серверный режим
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// 2. Добавляем MudBlazor
builder.Services.AddMudServices();

// 3. Подключаем БД
builder.Services.AddDbContext<DashboardDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("AeroQDb")));

// 4. Регистрируем наш сервис
builder.Services.AddScoped<DashboardService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

// 🚨 ВАЖНО: Правильный маппинг для .NET 8/9 Blazor Web App
app.MapRazorComponents<AeroQ.Dashboard.Components.App>()
    .AddInteractiveServerRenderMode();

app.Run();