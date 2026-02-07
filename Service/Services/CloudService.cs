using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Service.Services
{
    public class CloudService
    {
        private readonly Cloudinary _cloudinary;
        private readonly ILogger<CloudService> _logger;

        public CloudService(IConfiguration config, ILogger<CloudService> logger)
        {
            var acc = new Account(
                config["CloudinarySettings:CloudName"],
                config["CloudinarySettings:ApiKey"],
                config["CloudinarySettings:ApiSecret"]
            );
            _cloudinary = new Cloudinary(acc);
            _logger = logger;
        }

        public async Task<string> UploadImageAsync(IFormFile imageFile)
        {
            if (imageFile == null || imageFile.Length == 0)
            {
                return "default.jpg";
            }

            try
            {
                using var stream = imageFile.OpenReadStream();
                var uploadParams = new ImageUploadParams()
                {
                    File = new FileDescription(imageFile.FileName, stream),
                    Transformation = new Transformation().Width(800).Height(600).Crop("limit")
                };
                var uploadResult = await _cloudinary.UploadAsync(uploadParams);

                if(uploadResult.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    return uploadResult.SecureUrl?.ToString() ?? "default.jpg";
                }

                _logger?.LogWarning("Upload para Claudinary falhou: {Error}", uploadResult.Error?.Message);

                return "default.jpg";
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Erro ao fazer upload da imagem {FileName}", imageFile.FileName);
                return "default.jpg";
            }
        }
    }
}
