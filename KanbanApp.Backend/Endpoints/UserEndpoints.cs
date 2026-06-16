using System.Security.Claims;
using KanbanApp.Backend.DTOs;
using KanbanApp.Backend.Services;

namespace KanbanApp.Backend.Endpoints;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this WebApplication app)
    {
        app.MapGet("/api/users/me", async (ClaimsPrincipal user, IUserService userService) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            var profile = await userService.GetUserProfileAsync(userId!);
            if (profile == null) return Results.NotFound();
            return Results.Ok(profile);
        }).RequireAuthorization();

        app.MapPut("/api/users/me", async (UpdateProfileDto dto, ClaimsPrincipal user, IUserService userService) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            var profile = await userService.UpdateUserProfileAsync(userId!, dto.Bio, null);
            if (profile == null) return Results.NotFound();
            return Results.Ok(profile);
        }).RequireAuthorization();

        // S5: validate file type and size; C2: clean up old avatar file
        app.MapPost("/api/users/me/avatar", async (IFormFile file, ClaimsPrincipal user,
            IUserService userService, IWebHostEnvironment env) =>
        {
            var validationError = CoverImageHelper.ValidateImage(file);
            if (validationError != null) return Results.BadRequest(validationError);

            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);

            var currentProfile = await userService.GetUserProfileAsync(userId!);
            var oldAvatarUrl = currentProfile?.ProfilePictureUrl;

            var uploadsFolder = Path.Combine(CoverImageHelper.GetWebRoot(env), "avatars");
            Directory.CreateDirectory(uploadsFolder);
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var fileName = $"{userId}{ext}";
            var filePath = Path.Combine(uploadsFolder, fileName);
            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var url = $"/avatars/{fileName}";
            var profile = await userService.UpdateUserProfileAsync(userId!, null, url);
            if (profile == null)
            {
                File.Delete(filePath);
                return Results.NotFound();
            }

            if (!string.IsNullOrEmpty(oldAvatarUrl) && oldAvatarUrl != url)
                CoverImageHelper.DeleteLocalImage(env, oldAvatarUrl);

            return Results.Ok(profile);
        }).DisableAntiforgery().RequireAuthorization();
    }
}
