using System.ComponentModel.DataAnnotations;

namespace MicroSocialPlatform.Models.ViewModels
{
    public class CreatePostViewModel
    {
        [Required(ErrorMessage = "Postarea trebuie sa contina un text!")]
        [StringLength(1000, ErrorMessage = "Postarea nu poate depasi 1000 de caractere.")]
        [Display(Name = "Ce ai vrea sa impartasesti?")]
        public string Content { get; set; } 

        [Display(Name = "Incarca imagine (optional)")]
        public List<IFormFile>? Images { get; set; }

        [Display(Name = "Incarca videoclip (optional)")]
        public List<IFormFile>? Videos { get; set; }
    }
}
