using System;
using System.ComponentModel.DataAnnotations;

namespace ChatServer.Models
{
    public enum MessageType
    {
        Text = 0,
        Image = 1,
        File = 2
    }

    public class Message
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public string SenderName { get; set; } = string.Empty;
        
        public string? ReceiverName { get; set; }
        
        [Required]
        public string Content { get; set; } = string.Empty; // Chứa Text hoặc URL file
        
        public MessageType Type { get; set; } = MessageType.Text;

        public string? AttachmentName { get; set; } // Tên gốc của file (nếu có)

        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}
