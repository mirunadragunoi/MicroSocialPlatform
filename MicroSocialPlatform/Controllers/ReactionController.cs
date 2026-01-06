using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MicroSocialPlatform.Data;
using MicroSocialPlatform.Models;
using Microsoft.Identity.Client;

namespace MicroSocialPlatform.Controllers
{
    [Authorize]
    public class ReactionController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public ReactionController(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleLike(int postId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Unauthorized();
            }

            // verific daca exista o reactie pentru postarea si utilizatorul curent
            var existingReaction = await _context.Reactions
                .FirstOrDefaultAsync(r => r.PostId == postId && 
                                          r.UserId == currentUser.Id &&
                                          r.Type == ReactionType.Like);

            bool isLiked;
            if (existingReaction != null)
            {
                // unlike: sterge reactia existenta
                _context.Reactions.Remove(existingReaction);
                isLiked = false;
            }
            else
            {
                // like: adauga o noua reactie
                var newReaction = new Reaction
                {
                    PostId = postId,
                    UserId = currentUser.Id,
                    Type = ReactionType.Like,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Reactions.Add(newReaction);
                isLiked = true;
            }
            
            await _context.SaveChangesAsync();

            // calculez numarul total de like-uri pentru postare
            var totalLikes = await _context.Reactions
                .CountAsync(r => r.PostId == postId && r.Type == ReactionType.Like);

            return Json(new
            {
                success = true,
                isLiked = isLiked,
                likesCount = totalLikes
            });
        }
    }
}
