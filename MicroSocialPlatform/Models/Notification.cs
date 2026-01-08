using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MicroSocialPlatform.Models
{
    public class Notification
    {
        [Key]
        public int Id { get; set; }

        // cine primeste notificarea
        [Required]
        public string RecipientId { get; set; }

        // cine a generat notificarea
        public string? SenderId { get; set; }

        // tipul notificarii
        [Required]
        public NotificationType Type { get; set; }

        // continutul notificarii
        [Required]
        [StringLength(500)]
        public string Content { get; set; }

        // link catre resursa relevanta (postare, profil, etc.)
        public string? RelatedUrl { get; set; }

        // ID ul entitatii asociate (postare, comentariu, etc.)
        public int? RelatedEntityId { get; set; }

        // daca a fost citita
        public bool IsRead { get; set; } = false;

        // data crearii
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // navigation properties
        [ForeignKey("RecipientId")]
        public virtual ApplicationUser? Recipient { get; set; }

        [ForeignKey("SenderId")]
        public virtual ApplicationUser? Sender { get; set; }
    }

    public enum NotificationType
    {
        Like = 1,               // cineva a dat like la postarea ta
        Comment = 2,            // cineva a comentat la postarea ta
        Follow = 3,             // cineva a inceput sa te urmareasca
        FollowRequest = 4,      // cerere de urmarire (cont privat)
        FollowAccepted = 5,     // cererea ta de urmarire a fost acceptata
        NewPost = 6,            // un utilizator pe care il urmaresti a postat
        GroupMessage = 7,       // mesaj nou in grup
        Mention = 8,            // ai fost mentionat undeva??? maybe
        GroupJoinRequest = 9,   // cerere de join pt un grup
        GroupJoinAccepted = 10, // cererea de join pt un grup a fost acceptata
        PostDeleted = 11,       // daca o postare a mea a fost stearsa
        CommentDeleted = 12,    // daca un comentariu lasat de mine a fost sters
        GroupDeleted = 13,      // daca grupul din care fac parte a fost sters
    }
}