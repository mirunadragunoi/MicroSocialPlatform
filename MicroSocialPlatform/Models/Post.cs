using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MicroSocialPlatform.Models
{
    public class Post
    {
        [Key]
        public int Id { get; set; }

        // continutul postarii
        [StringLength(2000, ErrorMessage = "Postarea nu poate depăși 2000 de caractere!")]
        public string? Content { get; set; }

        // data crearii postarii
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // data ultimei modificari a postarii
        public DateTime? UpdatedAt { get; set; }

        // numar like uri
        [NotMapped]
        public int LikesCount => Reactions?.Count(r => r.Type == ReactionType.Like) ?? 0;

        // numar de comentarii
        public int CommentsCount { get; set; } = 0;

        // vizibilitatea postarii (public / private)
        public PostVisibility Visibility { get; set; } = PostVisibility.Public;

        // foreign key catre user
        [Required]
        public string UserId { get; set; }

        // navigation property catre User
        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        // comentariile de la postare
        public virtual ICollection<Comment>? Comments { get; set; }

        // colectia de fisiere media atasate postarii (0, 1 sau 10 poze)
        public virtual ICollection<PostMedia>? PostMedias { get; set; } = new List<PostMedia>();

        // reactiile de la postare
        public virtual ICollection<Reaction>? Reactions { get; set; } = new List<Reaction>();
    }

    public enum PostVisibility
    {
        Public = 0,
        Private = 1
    }
}
