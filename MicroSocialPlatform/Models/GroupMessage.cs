using System.ComponentModel.DataAnnotations;    

namespace MicroSocialPlatform.Models
{
    public class GroupMessage
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Mesajul nu poate fi gol!")]
        [StringLength(1000, ErrorMessage = "Mesajul nu poate depasi 1000 de caractere.")]
        public string Content { get; set; }

        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        // userul care posteaza mesajul
        public string UserId { get; set; }
        public virtual ApplicationUser User { get; set; }

        // grupul in care se posteaza mesajul
        public int GroupId { get; set; }
        public virtual Group Group { get; set; }
    }
}
