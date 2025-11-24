using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MicroSocialPlatform.Models
{
    public class Follow
    {
        [Key]
        public int Id { get; set; }

        // id ul utiliazatorului care urmareste
        [Required]
        public string FollowerId { get; set; }

        // id ul utilizatorului care este urmarit
        [Required]
        public string FollowingId { get; set; }

        // data la care a inceput urmarirea
        public DateTime FollowedAt { get; set; } = DateTime.UtcNow;

        // navigation proprieties
        [ForeignKey("FollowerId")]
        public virtual ApplicationUser? Follower { get; set; }

        [ForeignKey("FollowingId")]
        public virtual ApplicationUser? Following { get; set; }
    }
}
