using KanbanApp.Backend.Data;
using KanbanApp.Backend.Extensions;
using KanbanApp.Backend.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDatabase(builder.Configuration, builder.Environment);
builder.Services.AddAuth(builder.Configuration);
builder.Services.AddAppServices();
builder.Services.AddSwagger();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
        policy.WithOrigins(
                "http://localhost:5173",
                "https://shellty-kanban.netlify.app"
            )
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
app.MapHealthEndpoints();

app.Run();

public partial class Program { }