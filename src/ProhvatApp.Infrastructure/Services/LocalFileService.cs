using System;
using System.IO;
using System.Threading.Tasks;
using ProhvatApp.Application.Common.Interfaces;

namespace ProhvatApp.Infrastructure.Services;

public class LocalFileService : IFileService
{
    public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType)
    {
        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
        
        if (!Directory.Exists(uploadsFolder))
            Directory.CreateDirectory(uploadsFolder);
            
        var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
        var filePath = Path.Combine(uploadsFolder, uniqueFileName);
        
        using var file = new FileStream(filePath, FileMode.Create);
        await fileStream.CopyToAsync(file);
        
        return $"/uploads/{uniqueFileName}";
    }
}
