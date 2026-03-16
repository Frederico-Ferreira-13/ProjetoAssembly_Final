using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Contracts.Service;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Service.Services
{
    public class CloudService : ICloudService
    {
        private readonly Cloudinary _cloudinary;
        private readonly ILogger<CloudService> _logger;
        private readonly string _defaultImageUrl;

        public CloudService(IConfiguration config, ILogger<CloudService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            var cloudName = config["CloudinarySettings:CloudName"] ?? config["CLOUDINARY_CLOUD_NAME"];
            var apiKey = config["CloudinarySettings:ApiKey"] ?? config["CLOUDINARY_API_KEY"];
            var apiSecret = config["CloudinarySettings:ApiSecret"] ?? config["CLOUDINARY_API_SECRET"];
            
            _defaultImageUrl = config["CloudinarySettings:DefaultImageUrl"] ?? config["CLOUDINARY_DEFAULT_IMAGE_URL"]
                ?? "http://res.cloudinary.com/demo/image/upload/v1/default/jpg";

            if(string.IsNullOrWhiteSpace(cloudName) || 
                string.IsNullOrWhiteSpace(apiKey) ||
                string.IsNullOrWhiteSpace(apiSecret))
            {
                throw new InvalidOperationException("Configurações do Cloudinary não encontradas. Verifica o .env ou appsettings.");
            }

           var account = new Account(cloudName, apiKey, apiSecret);
            _cloudinary = new Cloudinary(account);
            _cloudinary.Api.Secure = true; // Para forçar sempre URLs com https
            
        }

        public async Task<string> UploadImageAsync(IFormFile imageFile)
        {
            if (imageFile == null || imageFile.Length == 0)
            {
                return _defaultImageUrl;
            }

            try
            {
                await using var stream = imageFile.OpenReadStream();

                var uploadParams = new ImageUploadParams()
                {
                    File = new FileDescription(imageFile.FileName, stream),
                    Transformation = new Transformation()
                        .Width(800)
                        .Height(600)
                        .Crop("limit")
                        .Quality("auto")
                        .FetchFormat("auto"),
                    UseFilename = true,
                    UniqueFilename = true,
                    Overwrite = false
                };

                var uploadResult = await _cloudinary.UploadAsync(uploadParams);

                if(uploadResult.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    return uploadResult.SecureUrl?.ToString() ?? _defaultImageUrl;
                }

                _logger?.LogWarning(
                    "Falha no upload para Cloudinary. Status: {StatusCode}, Erro: {ErrorMessage}",
                    uploadResult.StatusCode,
                    uploadResult.Error?.Message);

                return _defaultImageUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Erro ao fazer upload da imagem {FileName}. Tamanho: {Length} bytes",
                    imageFile?.FileName, imageFile?.Length);

                return _defaultImageUrl;
            }
        }
    }
}
