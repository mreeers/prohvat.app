using System.IO;
using System.Threading.Tasks;

namespace ProhvatApp.Application.Common.Interfaces;

public interface IFileService
{
    Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType);
}
