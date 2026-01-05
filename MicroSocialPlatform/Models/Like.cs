using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MicroSocialPlatform.Models
{
    public class Like
    {
        [Key]
        public int Id { get; set; }

        // data la care s a dat like
        public DateTime LikedAt { get; set; } = DateTime.UtcNow;

        // foreign keys
        [Required]
        public int PostId { get; set; }

        [Required]
        public string UserId { get; set; }

        public LikeType Type { get; set; }

        // navigation properties
        [ForeignKey("PostId")]
        public virtual Post? Post { get; set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }
    }

    public enum LikeType 
    {
        Like = 1,
        Love = 2,
        Haha = 3,
        Wow = 4,
        Sad = 5,
        Angry = 6
    }
}
