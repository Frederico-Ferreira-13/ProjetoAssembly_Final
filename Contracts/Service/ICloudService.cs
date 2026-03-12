using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace Contracts.Service
{
    public interface ICloudService
    {
        Task<string> UploadImageAsync(IFormFile? imageFile);
    }
}
