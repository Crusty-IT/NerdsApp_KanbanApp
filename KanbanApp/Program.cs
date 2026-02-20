using Microsoft.EntityFrameworkCore;
using KanbanApp.Data;
using KanbanApp.Models;
using KanbanApp.Services;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentityApiEndpoints<ApplicationUser>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<IBoardService, BoardService>();
builder.Services.AddAuthorization();
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapIdentityApi<ApplicationUser>();

app.MapGet("/api/boards", async (IBoardService service) =>
{
    var boards = await service.GetAllByUserAsync("test-user-id");
    return Results.Ok(boards);
});

app.MapGet("/api/users/me", async (ClaimsPrincipal user, ApplicationDbContext db) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    
    var appUser = await db.Users.FindAsync(userId);
    
    return TypedResults.Ok(new { appUser!.Id, appUser.UserName, appUser.Email });

}).RequireAuthorization();

app.Run();

public partial class Program { }

