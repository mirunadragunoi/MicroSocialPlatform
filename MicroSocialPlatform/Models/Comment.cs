using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MicroSocialPlatform.Models
{
    public class Comment
    {
        [Key]
        public int Id { get; set; }

        // continutul comentariului
        [Required]
        [StringLength(500, ErrorMessage = "Comentariul nu poate depasi 500 de caractere!")]
        public string Content { get; set; }

        // data crearii
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // data ultimei modificari
        public DateTime? UpdatedAt { get; set; }

        // foreign keys
        [Required]
        public int PostId { get; set; }

        [Required]
        public string UserId { get; set; }

        // navigation properties
        [ForeignKey("PostId")]
        public virtual Post? Post { get; set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }
    }
}
