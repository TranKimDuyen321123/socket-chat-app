using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChatServer.Models
{
    public class Friendship
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string RequesterName { get; set; } = string.Empty; // Người gửi lời mời

        [Required]
        public string ReceiverName { get; set; } = string.Empty; // Người nhận

        public bool IsAccepted { get; set; } = false; // false: Chờ đồng ý, true: Đã là bạn
    }
}
