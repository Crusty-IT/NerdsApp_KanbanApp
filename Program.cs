using Microsoft.EntityFrameworkCore;
using KanbanApp.Data;
using KanbanApp.Models;
using KanbanApp.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentityApiEndpoints<ApplicationUser>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<IBoardService, BoardService>();
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/api/boards", async (IBoardService service) =>
{
    var boards = await service.GetAllByUserAsync("test-user-id");
    return Results.Ok(boards);
});

app.Run();


/*
DEBUGGING WITH AI - COMPLETION REPORT

What Was Done:
- Bug Introduced: Changed AddScoped<IBoardService, BoardService>() to AddScoped<BoardService>();
- Error: InvalidOperationException: Body was inferred but the method does not allow inferred body parameters;
- AI Diagnosis: Used Rider AI with structured prompt. AI identified missing interface mapping in DI;
- Fix: Restored builder.Services.AddScoped<IBoardService, BoardService>();
- Verified: Endpoint works - returns 200 OK;
- Understanding: DI needs interface→implementation mapping;

*/