using System.ComponentModel.DataAnnotations;

namespace MicroSocialPlatform.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Email-ul este obligatoriu!")]
        [EmailAddress(ErrorMessage = "Adresa de email nu este validă!")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Parola este obligatorie!")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Display(Name = "Ține-mă minte")]
        public bool RememberMe { get; set; }

        public string? ReturnUrl { get; set; } // stochez URL-ul de redirecționare
    }
}
