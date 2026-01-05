using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MicroSocialPlatform.Models
{
    public class Notification
    {
        [Key]
        public int Id { get; set; }

        // 
        [Required]
        public string RecipientId { get; set; }

        // Cine a generat notificarea (poate fi null pentru notificări de sistem)
        public string? SenderId { get; set; }

        // Tipul notificării
        [Required]
        public NotificationType Type { get; set; }

        // Conținutul notificării
        [Required]
        [StringLength(500)]
        public string Content { get; set; }

        // Link către resursa relevantă (postare, profil, etc.)
        public string? RelatedUrl { get; set; }

        // ID-ul entității asociate (postare, comentariu, etc.)
        public int? RelatedEntityId { get; set; }

        // A fost citită?
        public bool IsRead { get; set; } = false;

        // Data creării
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("RecipientId")]
        public virtual ApplicationUser? Recipient { get; set; }

        [ForeignKey("SenderId")]
        public virtual ApplicationUser? Sender { get; set; }
    }

    public enum NotificationType
    {
        Like = 1,           // Cineva a dat like la postarea ta
        Comment = 2,        // Cineva a comentat la postarea ta
        Follow = 3,         // Cineva te-a început să te urmărească
        FollowRequest = 4,  // Cerere de urmărire (cont privat)
        FollowAccepted = 5, // Cererea ta de urmărire a fost acceptată
        NewPost = 6,        // Un utilizator pe care îl urmărești a postat
        GroupMessage = 7,   // Mesaj nou în grup
    }
}