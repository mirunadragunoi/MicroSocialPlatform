using System;
using System.ComponentModel.DataAnnotations;

namespace MicroSocialPlatform.Models
{
    public class SavedPost
    {
        [Key]
        public int Id { get; set; }

        public string UserId { get; set; }
        public virtual ApplicationUser User { get; set; }

        public int PostId { get; set; }
        public virtual Post Post { get; set; }

        public DateTime SavedAt { get; set; } = DateTime.UtcNow;
    }
}
