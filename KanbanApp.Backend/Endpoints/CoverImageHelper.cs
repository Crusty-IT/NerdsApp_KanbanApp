namespace KanbanApp.Backend.Endpoints;

public static class CoverImageHelper
{
    public static string? ValidateImage(IFormFile file)
    {
        if (file.Length == 0) return "Image cannot be empty.";
        if (file.Length > 5 * 1024 * 1024) return "Image cannot be bigger than 5 MB.";

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (!allowedExtensions.Contains(ext))
            return "Only JPG, PNG and WEBP images are allowed.";

        var header = new byte[12];
        using var stream = file.OpenReadStream();
        var bytesRead = stream.Read(header, 0, 12);

        if (!HasValidMagicBytes(header, bytesRead, ext))
            return "File content does not match the declared image type.";

        return null;
    }

    public static bool IsValidObjectPosition(string? position)
    {
        if (string.IsNullOrWhiteSpace(position)) return false;
        return position.Length <= 50
            && System.Text.RegularExpressions.Regex.IsMatch(position, @"^[\w\s.%]+$");
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

    public static void DeleteLocalImage(IWebHostEnvironment env, string? url)
    {
        if (string.IsNullOrEmpty(url) || url.StartsWith("http")) return;
        var filePath = Path.Combine(GetWebRoot(env), url.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(filePath)) File.Delete(filePath);
    }

    public static string GetWebRoot(IWebHostEnvironment env)
        => env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");

    private static bool HasValidMagicBytes(byte[] header, int bytesRead, string ext) => ext switch
    {
        ".jpg" or ".jpeg" => bytesRead >= 3
            && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
        ".png" => bytesRead >= 4
            && header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47,
        ".webp" => bytesRead >= 12
            && header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46
            && header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50,
        _ => false
    };
}
