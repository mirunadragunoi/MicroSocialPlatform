using System.ComponentModel.DataAnnotations;

namespace MicroSocialPlatform.Models
{
    public class EditProfileViewModel
    {
        // nume
        [Required(ErrorMessage = "Numele complet este obligatoriu!")]
        [StringLength(100, ErrorMessage = "Numele complet nu poate depăși 100 de caractere!")]
        [Display(Name = "Nume complet")]
        public string FullName { get; set; }

        // username
        [StringLength(30, ErrorMessage = "Username-ul nu poate depasi 30 de caractere!")]
        [RegularExpression(@"^[a-zA-Z0-9._]+$", ErrorMessage = "Username-ul poate conține doar litere, cifre, punct și underscore!")]
        [Display(Name = "Username")]
        public string? CustomUsername { get; set; }

        // biografie
        [StringLength(500, ErrorMessage = "Biografia nu poate depăși 500 de caractere!")]
        [Display(Name = "Biografie")]
        [DataType(DataType.MultilineText)]
        public string? Bio { get; set; }

        // status
        [StringLength(100, ErrorMessage = "Status-ul nu poate depăși 100 de caractere!")]
        [Display(Name = "Status")]
        public string? Status { get; set; }

        // emoji status
        [StringLength(10)]
        [Display(Name = "Emoji Status")]
        public string? StatusEmoji { get; set; }

        // link catre website
        [StringLength(200)]
        [Display(Name = "Website")]
        [DataType(DataType.Url)]
        public string? Website { get; set; }

        // locatie
        [StringLength(100)]
        [Display(Name = "Locație")]
        public string? Location { get; set; }

        // data nasterii
        [Display(Name = "Data nașterii")]
        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        // profil public sau private
        [Display(Name = "Profil public")]
        public bool IsPublic { get; set; } = true;

        // pt upload poza de profil
        [Display(Name = "Poză de profil")]
        public IFormFile? ProfilePictureFile { get; set; }

        // pt upload poza de cover
        [Display(Name = "Poză de cover")]
        public IFormFile? CoverPhotoFile { get; set; }

        // caile actuale ale pozelor (pentru preview)
        public string? CurrentProfilePicture { get; set; }
        public string? CurrentCoverPhoto { get; set; }
    }
}
