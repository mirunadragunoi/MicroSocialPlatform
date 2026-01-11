using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace MicroSocialPlatform.Models
{
    public class ApplicationUser : IdentityUser
    {
        // numele complet - obligatoriu
        [Required(ErrorMessage = "Numele complet este obligatoriu!")]
        [StringLength(100, ErrorMessage = "Numele complet nu poate depăși 100 de caractere!")]
        public string? FullName { get; set; }

        // username personalizat !!!diferit de username ul din Identity care e email
        [StringLength(30, ErrorMessage = "Username-ul nu poate depăși 30 de caractere!")]
        [RegularExpression(@"^[a-zA-Z0-9._]+$", ErrorMessage = "Username-ul poate conține doar litere, cifre, punct și underscore!")]
        public string? CustomUsername { get; set; }

        // descriere = bio 
        [StringLength(500, ErrorMessage = "Biografia nu poate depăși 500 de caractere!")]
        public string? Bio { get; set; }

        // status cu text
        [StringLength(60)]
        public string? Status { get; set; }

        // status cu emoji
        [StringLength(10)]
        public string? StatusEmoji { get; set; }

        // poza de profil - optional -> calea catre URL
        public string? ProfilePicture { get; set; }

        // poza de cover / banner
        public string? CoverPhoto { get; set; }

        // website / link personal
        [StringLength(200)]
        public string? Website { get; set; }

        // locatie 
        [StringLength(100)]
        public string? Location { get; set; }

        // data nasterii
        public DateTime? DateOfBirth { get; set; }

        // vizibilitatea profilului - public/privat
        public bool IsPublic { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // RELATII
        // postari
        public virtual ICollection<Post>? Posts { get; set; }

        // urmaritori
        public virtual ICollection<Follow>? Followers { get; set; }

        // urmariti
        public virtual ICollection<Follow>? Following {  get; set; }
    }
}
