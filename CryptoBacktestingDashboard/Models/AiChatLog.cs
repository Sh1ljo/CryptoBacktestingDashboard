using System;
using System.ComponentModel.DataAnnotations;

namespace CryptoBacktestingDashboard.Models
{
    public class AiChatLog
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// "user" or "assistant"
        /// </summary>
        [Required]
        [MaxLength(20)]
        public string Role { get; set; } = string.Empty;

        /// <summary>
        /// Message content
        /// </summary>
        [Required]
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// When this message was sent/received
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// The date (UTC date portion) this message counts toward the daily limit
        /// </summary>
        public DateTime DateKey { get; set; } = DateTime.UtcNow.Date;
    }
}
