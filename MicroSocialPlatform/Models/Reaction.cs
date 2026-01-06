using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MicroSocialPlatform.Models
{
    public class Reaction
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }
        public ApplicationUser? User { get; set; }

        [Required]
        public int PostId { get; set; }
        public Post? Post { get; set; }

        public ReactionType Type { get; set; } = ReactionType.Like;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
