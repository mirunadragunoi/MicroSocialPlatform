namespace MicroSocialPlatform.Models
{
    public class GroupJoinRequest
    {
        public int Id { get; set; }

        // id ul grupului pt care s a trimis cererea de join
        public int GroupId { get; set; }
        public virtual Group Group { get; set; }

        // id uk userului ce a trimis cererea de join
        public string UserId { get; set; }
        public virtual ApplicationUser User { get; set; }

        // statusul cererii
        public GroupJoinRequestStatus Status { get; set; } = GroupJoinRequestStatus.Pending;

        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

        public DateTime? RespondedAt { get; set; }
    }

    public enum GroupJoinRequestStatus
    {
        Pending = 0,
        Accepted = 1,
        Rejected = 2
    }
}
