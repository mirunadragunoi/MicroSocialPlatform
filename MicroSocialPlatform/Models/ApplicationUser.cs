using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace MicroSocialPlatform.Models
{
    public class ApplicationUser : IdentityUser
    {
        // numele complet - obligatoriu
        [Required(ErrorMessage = "Numele complet este obligatoriu!")]
        [StringLength(100, ErrorMessage = "Numele complet nu poate depasi 100 de caractere!")]
        public string? FullName { get; set; }

        // descriere = bio 
        [StringLength(500, ErrorMessage = "Descrierea nu poate depasi 500 de caractere!")]
        public string? Description { get; set; }

        // poza de profil - optional -> calea catre URL
        public string? ProfilePicture { get; set; }

        // vizibilitatea profilului - public/privat
        public bool IsPublic { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
