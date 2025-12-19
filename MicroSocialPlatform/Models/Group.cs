using System.ComponentModel.DataAnnotations;

namespace MicroSocialPlatform.Models
{
    public class Group
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Numele grupului este obligatoriu!")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Numele grupului trebuie sa aiba intre 3 si 100 caractere.")]
        public string Name { get; set; }

        [StringLength(500, ErrorMessage = "Descrierea grupului nu poate depasi 500 de caractere.")]
        public string Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // creatorul grupului (owner)
        public string OwnerId { get; set; }
        public virtual ApplicationUser Owner { get; set; }

        // relația cu membrii grupului (M - N)
        public virtual ICollection<GroupMember> Members { get; set; } = new List<GroupMember>();

        // relația cu mesajele din grup (1 - M)
        public virtual ICollection<GroupMessage> Messages { get; set; } = new List<GroupMessage>();

        // postarile din grup (1 - M)
        public virtual ICollection<Post> Posts { get; set; } = new List<Post>();

        // poza grupului
        public string? GroupPicture { get; set; }
    }
}
