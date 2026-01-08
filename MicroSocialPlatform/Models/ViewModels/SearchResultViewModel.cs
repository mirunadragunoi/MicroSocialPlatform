namespace MicroSocialPlatform.Models.ViewModels
{
    public class SearchResultViewModel
    {
        public string UserId { get; set; }
        public string FullName { get; set; }
        public string Username { get; set; }
        public string Bio { get; set; }
        public string ProfilePicture { get; set; }
        public bool IsPublic { get; set; }

        public bool IsFollowing { get; set; } // il urmaresc?
        public bool IsPending { get; set; } // cererea de urmarire este in asteptare?
        public bool IsCurrentUser { get; set; } // este utilizatorul curent?
    }
}
