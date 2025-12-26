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

        // statusul cererii
        public FollowStatus Status { get; set; } = FollowStatus.Pending;

        // daca cand a fost trimisa cererea
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

        // data cand a fost acceptata cererea
        public DateTime? AcceptedAt { get; set; }

        // navigation proprieties
        [ForeignKey("FollowerId")]
        public virtual ApplicationUser? Follower { get; set; }

        [ForeignKey("FollowingId")]
        public virtual ApplicationUser? Following { get; set; }
    }
}
