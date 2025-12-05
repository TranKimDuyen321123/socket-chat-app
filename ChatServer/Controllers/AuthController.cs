using ChatServer.Data;
using ChatServer.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace ChatServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ChatContext _context;
        private readonly ILogger<AuthController> _logger;

        public AuthController(ChatContext context, ILogger<AuthController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public record AuthRequest(string Username, string Password);

        [HttpPost("register")]
        public IActionResult Register([FromBody] AuthRequest request)
        {
            if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
                return BadRequest(new { Message = "Thông tin không hợp lệ." });

            if (_context.Users.Any(u => u.Username == request.Username))
                return Conflict(new { Message = "Tên đăng nhập đã tồn tại." });

            var newUser = new User { Username = request.Username, Password = request.Password };
            _context.Users.Add(newUser);
            _context.SaveChanges();

            _logger.LogInformation("User registered successfully: {Username}", request.Username);

            return Ok(new { Message = "Đăng ký thành công!" });
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] AuthRequest request)
        {
            _logger.LogInformation("Login attempt for username: '{Username}'", request.Username);

            var user = _context.Users.FirstOrDefault(u => u.Username == request.Username && u.Password == request.Password);
            
            if (user != null)
            {
                _logger.LogInformation("Login successful for username: '{Username}'", request.Username);
                return Ok(new { Message = "Đăng nhập thành công!", Username = user.Username });
            }

            _logger.LogWarning("Login failed for username: '{Username}'. User not found or password incorrect.", request.Username);
            return Unauthorized(new { Message = "Sai tên đăng nhập hoặc mật khẩu." });
        }
    }
}
