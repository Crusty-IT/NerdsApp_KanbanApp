using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using KanbanApp.Backend.Data;
using KanbanApp.Backend.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace KanbanApp.Backend.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        app.MapPost("/register", async (
            [FromBody] RegisterRequest request,
            UserManager<ApplicationUser> userManager) =>
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                return Results.BadRequest(new { message = "Email and password are required." });

            var userName = string.IsNullOrWhiteSpace(request.UserName)
                ? request.Email.Split('@')[0]
                : request.UserName;

            var user = new ApplicationUser { UserName = userName, Email = request.Email };
            var result = await userManager.CreateAsync(user, request.Password);

            if (!result.Succeeded)
                return Results.BadRequest(new { message = string.Join(", ", result.Errors.Select(e => e.Description)) });

            return Results.Ok(new { message = "User created successfully." });
        }).RequireRateLimiting("auth");

        app.MapPost("/login", async (
            [FromBody] LoginRequest request,
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext db,
            IConfiguration configuration) =>
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                return Results.BadRequest(new { message = "Email and password are required." });

            var user = await userManager.FindByEmailAsync(request.Email);
            if (user == null)
                return Results.Unauthorized();

            var result = await signInManager.CheckPasswordSignInAsync(user, request.Password, false);
            if (!result.Succeeded)
                return Results.Unauthorized();

            var key = configuration["Jwt:Key"]!;
            var accessToken = GenerateAccessToken(user, key, configuration["Jwt:Issuer"], configuration["Jwt:Audience"]);
            var refreshToken = GenerateRefreshToken();

            db.RefreshTokens.Add(new RefreshToken
            {
                Token = refreshToken,
                UserId = user.Id,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            });
            await db.SaveChangesAsync();

            return Results.Ok(new { accessToken, refreshToken });
        }).RequireRateLimiting("auth");

        app.MapPost("/api/auth/refresh", async (
            [FromBody] RefreshRequest request,
            ApplicationDbContext db,
            IConfiguration configuration) =>
        {
            // Atomic revoke: only one concurrent request with the same token can succeed
            var revoked = await db.RefreshTokens
                .Where(rt => rt.Token == request.RefreshToken && !rt.IsRevoked && rt.ExpiresAt > DateTime.UtcNow)
                .ExecuteUpdateAsync(setters => setters.SetProperty(rt => rt.IsRevoked, true));

            if (revoked == 0)
                return Results.Unauthorized();

            var stored = await db.RefreshTokens
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken);

            var newRefreshToken = GenerateRefreshToken();
            db.RefreshTokens.Add(new RefreshToken
            {
                Token = newRefreshToken,
                UserId = stored!.UserId,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            });
            await db.SaveChangesAsync();

            var key = configuration["Jwt:Key"]!;
            var accessToken = GenerateAccessToken(stored.User, key, configuration["Jwt:Issuer"], configuration["Jwt:Audience"]);
            return Results.Ok(new { accessToken, refreshToken = newRefreshToken });
        });

        app.MapPost("/api/auth/logout", async (
            [FromBody] RefreshRequest request,
            ApplicationDbContext db) =>
        {
            var stored = await db.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken);

            if (stored != null)
            {
                stored.IsRevoked = true;
                await db.SaveChangesAsync();
            }

            return Results.Ok(new { message = "Logged out." });
        }).RequireAuthorization();
    }

    private static string GenerateAccessToken(ApplicationUser user, string key, string? issuer, string? audience)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenKey = Encoding.UTF8.GetBytes(key);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.UserName ?? ""),
                new Claim(ClaimTypes.Email, user.Email ?? "")
            }),
            Expires = DateTime.UtcNow.AddMinutes(15),
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(tokenKey),
                SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    private static string GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }
}

public record RegisterRequest(string Email, string Password, string? UserName = null);
public record LoginRequest(string Email, string Password);
public record RefreshRequest(string RefreshToken);
