using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace MicroSocialPlatform.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [StringLength(100)]
        public string FullName { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
