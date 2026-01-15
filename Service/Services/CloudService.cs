using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Service.Services
{
    public class CloudService
    {
        private readonly Cloudinary _cloudinary;

        public CloudService(IConfiguration config)
        {
            var acc = new Account(
                config["CloudinarySettings:CloudName"],
                config["CloudinarySettings:ApiKey"],
                config["CloudinarySettings:ApiSecret"]
            );
            _cloudinary = new Cloudinary(acc);
        }

        public async Task<string> UploadImageAsync(IFormFile imageFile)
        {
            if (imageFile == null || imageFile.Length == 0)
            {
                return "default.jpg";
            }
            using var stream = imageFile.OpenReadStream();
            var uploadParams = new ImageUploadParams()
            {
                File = new FileDescription(imageFile.FileName, stream),
                Transformation = new Transformation().Width(800).Height(600).Crop("limit")
            };
            var uploadResult = await _cloudinary.UploadAsync(uploadParams);            
            return uploadResult.SecureUrl.ToString();
        }
    }
}
