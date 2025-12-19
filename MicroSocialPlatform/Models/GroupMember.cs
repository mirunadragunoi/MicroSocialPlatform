using System.ComponentModel.DataAnnotations;

namespace MicroSocialPlatform.Models
{
    public class GroupMember
    {
        public int Id { get; set; }

        // relatia cu grupul (M - 1)
        public int GroupId { get; set; }
        public virtual Group Group { get; set; }

        // relația cu utilizatorul (M - 1)
        public string UserId { get; set; }
        public virtual ApplicationUser User { get; set; }

        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

        // rol in grup: 0 = member; 1 = moderator; 2 = admin
        public GroupRole Role { get; set; } = GroupRole.Member;
    }

    public enum GroupRole
    {
        Member = 0,
        Moderator = 1,
        Admin = 2
    }
}
