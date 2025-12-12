using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; 

namespace MicroSocialPlatform.Models
{
    public class PostMedia
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int PostId { get; set; }

        [ForeignKey("PostId")]
        public virtual Post? Post { get; set; }

        [Required]
        public string Url { get; set; }

        [Required]
        public MediaType MediaType { get; set; }
    }

    public enum MediaType
    {
        Image = 0,
        Video = 1
    }
}
