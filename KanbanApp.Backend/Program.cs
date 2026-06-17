using KanbanApp.Backend.Data;
using KanbanApp.Backend.Extensions;
using KanbanApp.Backend.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDatabase(builder.Configuration, builder.Environment);
builder.Services.AddAuth(builder.Configuration);
builder.Services.AddAppServices();
builder.Services.AddSignalRServices();
builder.Services.AddSwagger();

var configuredCorsOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .GetChildren()
    .Select(origin => origin.Value)
    .Where(origin => !string.IsNullOrWhiteSpace(origin))
    .Cast<string>();

var configuredCorsOriginsFromValue = (builder.Configuration["Cors:AllowedOrigins"] ?? string.Empty)
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

var corsOrigins = new[]
    {
        "http://localhost:5173",
        "https://shellty-kanban.netlify.app",
        "https://shellty-kanban.vercel.app"
    }
    .Concat(configuredCorsOrigins)
    .Concat(configuredCorsOriginsFromValue)
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
        policy.WithOrigins(corsOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
});

var app = builder.Build();

app.RunMigrations();
app.UseStaticFiles();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");

var rateLimitingDisabled = builder.Configuration["RateLimiting:Disabled"] == "true";
if (!rateLimitingDisabled)
    app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapAuthEndpoints();
app.MapUserEndpoints();
app.MapProjectEndpoints();
app.MapBoardEndpoints();
app.MapColumnEndpoints();
app.MapCardEndpoints();
app.MapNotificationEndpoints();
app.MapPushNotificationEndpoints();
app.MapHealthEndpoints();
app.MapSignalREndpoints();

app.Run();

public partial class Program { }
