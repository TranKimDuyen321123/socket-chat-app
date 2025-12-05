using System.ComponentModel.DataAnnotations;

namespace ChatServer.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public string Username { get; set; } = string.Empty;
        
        [Required]
        public string Password { get; set; } = string.Empty; // Lưu ý: Nên hash password trong thực tế
    }
}
