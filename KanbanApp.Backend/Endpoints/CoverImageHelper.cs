namespace KanbanApp.Backend.Endpoints;

public static class CoverImageHelper
{
    public static string? ValidateImage(IFormFile file)
    {
        if (file.Length == 0) return "Image cannot be empty.";
        if (file.Length > 5 * 1024 * 1024) return "Image cannot be bigger than 5 MB.";

        var allowedContentTypes = new[] { "image/jpeg", "image/png", "image/webp" };
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (!allowedContentTypes.Contains(file.ContentType) || !allowedExtensions.Contains(ext))
            return "Only JPG, PNG and WEBP images are allowed.";

        return null;
    }

    public static async Task<string> SaveImageAsync(IWebHostEnvironment env, string subfolder, IFormFile file)
    {
        var uploadsFolder = Path.Combine(GetWebRoot(env), subfolder);
        Directory.CreateDirectory(uploadsFolder);

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var fileName = $"{Guid.NewGuid():N}{ext}";
        var filePath = Path.Combine(uploadsFolder, fileName);

        await using var stream = new FileStream(filePath, FileMode.Create);
        await file.CopyToAsync(stream);

        return $"/{subfolder}/{fileName}";
    }

    public static void DeleteLocalImage(IWebHostEnvironment env, string url)
    {
        if (string.IsNullOrEmpty(url) || url.StartsWith("http")) return;
        var filePath = Path.Combine(GetWebRoot(env), url.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(filePath)) File.Delete(filePath);
    }

    private static string GetWebRoot(IWebHostEnvironment env)
    {
        return env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");
    }
}
