namespace MicroSocialPlatform.Models.ViewModels
{
    public class AdminDashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalPosts { get; set; }
        public int TotalGroups { get; set; }
        public int TotalComments { get; set; }
        public int TotalReactions { get; set; }

        public int NewUsersThisWeek { get; set; }
        public int NewPostsToday { get; set; }

        // extra stats
        public int ActiveUsersToday { get; set; }
        public int PendingFollowRequests { get; set; }
        public int PendingGroupRequests { get; set; }
    }
}