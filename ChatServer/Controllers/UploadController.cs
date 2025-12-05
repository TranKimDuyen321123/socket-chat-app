using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Threading.Tasks;

namespace ChatServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UploadController : ControllerBase
    {
        [HttpPost]
        [RequestSizeLimit(524288000)] // Giới hạn 500MB cho Action này
        [RequestFormLimits(MultipartBodyLengthLimit = 524288000)] // Giới hạn độ dài Form Body
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Không có file được chọn.");

            // Tạo thư mục lưu trữ nếu chưa có
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            // Tạo tên file duy nhất để tránh trùng
            // Giữ nguyên phần mở rộng
            var ext = Path.GetExtension(file.FileName);
            var uniqueFileName = Guid.NewGuid().ToString() + ext;
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Trả về URL để truy cập file
            var fileUrl = $"/uploads/{uniqueFileName}";
            
            return Ok(new { Url = fileUrl, OriginalName = file.FileName });
        }
    }
}
