using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Mvc;
using ProjectApi.Models;

namespace ProjectApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UploadController : ControllerBase
    {
        private readonly Cloudinary _cloudinary;

        public UploadController(IConfiguration config)
        {
            var settings = config.GetSection("CloudinarySettings").Get<CloudinarySettings>();
            var account = new Account(settings.CloudName, settings.ApiKey, settings.ApiSecret);
            _cloudinary = new Cloudinary(account);
        }

        [HttpPost("file")]
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Không có file nào được tải lên.");

            var extension = Path.GetExtension(file.FileName).ToLower();
            dynamic uploadResult;

            using var stream = file.OpenReadStream();

            if (extension == ".jpg" || extension == ".jpeg" || extension == ".png" || extension == ".webp")
            {
                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(file.FileName, stream),
                    Folder = "uploads/images"
                };
                uploadResult = await _cloudinary.UploadAsync(uploadParams);
            }
            else if (extension == ".mp4" || extension == ".mov" || extension == ".avi")
            {
                var uploadParams = new VideoUploadParams
                {
                    File = new FileDescription(file.FileName, stream),
                    Folder = "uploads/videos"
                };
                uploadResult = await _cloudinary.UploadAsync(uploadParams);
            }
            else
            {
                // Dùng cho file .glb, .gltf, .zip, v.v.
                var uploadParams = new RawUploadParams
                {
                    File = new FileDescription(file.FileName, stream),
                    Folder = "uploads/files"
                };
                uploadResult = await _cloudinary.UploadAsync(uploadParams);
            }

            return Ok(new
            {
                url = uploadResult.SecureUrl?.ToString(),
                publicId = uploadResult.PublicId,
                resourceType = uploadResult.ResourceType
            });
        }
    }
}
