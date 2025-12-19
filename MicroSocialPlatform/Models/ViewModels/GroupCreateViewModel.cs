using System.ComponentModel.DataAnnotations;

namespace MicroSocialPlatform.Models.ViewModels
{
    public class GroupCreateViewModel
    {
        [Required(ErrorMessage = "Numele grupului este obligatoriu!")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Numele grupului trebuie sa aiba intre 3 si 100 caractere.")]
        public string Name { get; set; }

        [StringLength(500, ErrorMessage = "Descrierea grupului nu poate depasi 500 de caractere.")]
        public string Description { get; set; }

        public IFormFile? GroupPicture { get; set; }
    }
}
