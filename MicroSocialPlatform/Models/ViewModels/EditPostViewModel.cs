using System.ComponentModel.DataAnnotations;

namespace MicroSocialPlatform.Models.ViewModels
{
    public class EditPostViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Postarea trebuie sa aiba un text!")]
        [StringLength(1000, ErrorMessage = "Textul nu poate depasi 1000 de caractere.")]
        [Display(Name = "Editeaza textul")]
        public string Content { get; set; }

        // lista pentru afisare
        public List<PostMedia>? ExistingMedia { get; set; } = new List<PostMedia>();

        // lista pentru upload nou
        [Display(Name = "Adauga imagini noi")]
        public List<IFormFile>? NewImages { get; set; }

        [Display(Name = "Adauga video-uri noi")]
        public List<IFormFile>? NewVideos { get; set; }

        // lista pentru stergere
        public List<int>? MediaIdsToDelete { get; set; } = new List<int>();
    }
}
