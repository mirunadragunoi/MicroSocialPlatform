using System.ComponentModel.DataAnnotations;

namespace MicroSocialPlatform.Models
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Numele este obligatoriu!")]
        [StringLength(100, ErrorMessage = "Numele nu poate depăși 100 de caractere!")]
        public string FullName { get; set; } 

        [Required(ErrorMessage = "Email-ul este obligatoriu!")]
        [EmailAddress(ErrorMessage = "Adresa de email nu este validă!")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Parola este obligatorie!")]
        [DataType(DataType.Password)]
        [StringLength(100, ErrorMessage = "Parola trebuie să aibă cel puțin {2} caractere!", MinimumLength = 6)]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirma parola")]
        [Compare("Password", ErrorMessage = "Parolele nu se potrivesc!")]
        public string ConfirmPassword { get; set; }
    }
}
