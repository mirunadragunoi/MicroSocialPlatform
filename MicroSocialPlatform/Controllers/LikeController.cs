using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MicroSocialPlatform.Data;
using MicroSocialPlatform.Models;

namespace MicroSocialPlatform.Controllers
{
    [Authorize]
    public class ReactionController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReactionController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> React(int postId, int reactionType)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Json(new { success = false, message = "Nu ești autentificat" });
                }

                var post = await _context.Posts
                    .Include(p => p.User) 
                    .FirstOrDefaultAsync(p => p.Id == postId);

                if (post == null)
                {
                    return Json(new { success = false, message = "Postarea nu există" });
                }

                // Cauta like-ul existent
                var existingLike = await _context.Likes
                    .FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == user.Id);

                if (existingLike != null)
                {
                    // Daca e același tip de reactie -> sterge-l (toggle off)
                    if ((int)existingLike.Type == reactionType)
                    {
                        _context.Likes.Remove(existingLike);
                        post.LikesCount = Math.Max(0, post.LikesCount - 1);

                        // Sterg notificarea asociata
                        var notificationToDelete = await _context.Notifications
                            .FirstOrDefaultAsync(n => n.SenderId == user.Id &&
                                            n.RecipientId == post.UserId &&
                                            n.Type == NotificationType.Like &&
                                            n.RelatedEntityId == postId);
                        if (notificationToDelete != null)
                        {
                            _context.Notifications.Remove(notificationToDelete);
                        }
                    }
                    else
                    {
                        // Schimba tipul de reactie
                        existingLike.Type = (LikeType)reactionType;
                        existingLike.LikedAt = DateTime.UtcNow;
                    }
                }
                else
                {
                    // Creeaza like nou
                    var newLike = new Like
                    {
                        PostId = postId,
                        UserId = user.Id,
                        Type = (LikeType)reactionType,
                        LikedAt = DateTime.UtcNow
                    };
                    _context.Likes.Add(newLike);
                    post.LikesCount++;

                    // Creez notificarea
                    if (post.UserId != user.Id)
                    {
                        var notification = new Notification
                        {
                            RecipientId = post.UserId,
                            SenderId = user.Id,
                            Type = NotificationType.Like,
                            Content = $"{user.UserName} a reacționat la postarea ta",
                            RelatedUrl = $"/Post/Details/{postId}",
                            RelatedEntityId = postId,
                            IsRead = false,
                            CreatedAt = DateTime.UtcNow
                        };
                        _context.Notifications.Add(notification);
                    }
                }

                await _context.SaveChangesAsync();

                // Returneaza contoarele actualizate
                var counts = await _context.Likes
                    .Where(l => l.PostId == postId)
                    .GroupBy(l => l.Type)
                    .Select(g => new { Type = g.Key.ToString(), Count = g.Count() })
                    .ToDictionaryAsync(x => x.Type, x => x.Count);

                return Json(new
                {
                    success = true,
                    totalCount = post.LikesCount,
                    counts = counts,
                    hasReaction = existingLike == null || (int)existingLike.Type != reactionType,
                    currentReaction = existingLike != null && (int)existingLike.Type != reactionType ? (int)existingLike.Type : reactionType
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetUserReaction(int postId)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Json(new { hasReaction = false, reactionType = 0 });
                }

                var like = await _context.Likes
                    .FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == user.Id);

                return Json(new
                {
                    hasReaction = like != null,
                    reactionType = like != null ? (int)like.Type : 0
                });
            }
            catch
            {
                return Json(new { hasReaction = false, reactionType = 0 });
            }
        }
    }
}