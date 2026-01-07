using MicroSocialPlatform.Data;
using MicroSocialPlatform.Models;
using MicroSocialPlatform.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MicroSocialPlatform.Models.ViewModels;

namespace MicroSocialPlatform.Controllers
{
    [Authorize(Roles = "Administrator")]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public AdminController(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        // Lista utilizatori
        public async Task<IActionResult> Users()
        {
            var users = await _userManager.Users.ToListAsync();
            return View(users);
        }

        // Ștergere utilizator
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (user.Id == currentUser.Id)
            {
                TempData["ErrorMessage"] = "Nu te poți șterge pe tine însuți!";
                return RedirectToAction(nameof(Users));
            }

            // ✅ IMPORTANT: Șterge în ORDINEA CORECTĂ pentru a evita conflicte FK!

            // 1. Șterge postări (cu cascade la media, likes, comments)
            var posts = await _context.Posts.Where(p => p.UserId == userId).ToListAsync();
            _context.Posts.RemoveRange(posts);

            // 2. Șterge comentarii
            var comments = await _context.Comments.Where(c => c.UserId == userId).ToListAsync();
            _context.Comments.RemoveRange(comments);

            // 3. Șterge like-uri
            var likes = await _context.Likes.Where(l => l.UserId == userId).ToListAsync();
            _context.Likes.RemoveRange(likes);

            // 4. Șterge relații follow
            var follows = await _context.Follows
                .Where(f => f.FollowerId == userId || f.FollowingId == userId)
                .ToListAsync();
            _context.Follows.RemoveRange(follows);

            // 5. ✅ Șterge cereri de join în grupuri (LIPSEA ASTA!)
            var groupJoinRequests = await _context.GroupJoinRequests
                .Where(r => r.UserId == userId)
                .ToListAsync();
            _context.GroupJoinRequests.RemoveRange(groupJoinRequests);

            // 6. Șterge notificări
            var notifications = await _context.Notifications
                .Where(n => n.RecipientId == userId || n.SenderId == userId)
                .ToListAsync();
            _context.Notifications.RemoveRange(notifications);

            // 7. Gestionare grupuri deținute
            var ownedGroups = await _context.Groups
                .Include(g => g.Members)
                .Where(g => g.OwnerId == userId)
                .ToListAsync();

            foreach (var group in ownedGroups)
            {
                // Găsește succesor sau șterge grupul
                var successor = group.Members
                    .Where(m => m.UserId != userId)
                    .OrderByDescending(m => m.Role == GroupRole.Moderator)
                    .ThenBy(m => m.JoinedAt)
                    .FirstOrDefault();

                if (successor != null)
                {
                    // Transferă ownership
                    group.OwnerId = successor.UserId;
                    successor.Role = GroupRole.Admin;
                    _context.Update(group);
                    _context.Update(successor);
                }
                else
                {
                    // Nu există urmaș -> șterge grupul
                    _context.Groups.Remove(group);
                }
            }

            // 8. Șterge memberships în grupuri
            var memberships = await _context.GroupMembers
                .Where(m => m.UserId == userId)
                .ToListAsync();
            _context.GroupMembers.RemoveRange(memberships);

            // 9. Șterge mesaje în grupuri
            var groupMessages = await _context.GroupMessages
                .Where(m => m.UserId == userId)
                .ToListAsync();
            _context.GroupMessages.RemoveRange(groupMessages);

            // Salvează toate modificările
            await _context.SaveChangesAsync();

            // 10. Acum șterge userul (nu mai are dependențe FK)
            var result = await _userManager.DeleteAsync(user);

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = $"Utilizatorul {user.UserName} a fost șters cu succes!";
            }
            else
            {
                TempData["ErrorMessage"] = "Eroare la ștergerea utilizatorului: " + string.Join(", ", result.Errors.Select(e => e.Description));
            }

            return RedirectToAction(nameof(Users));
        }

        // Dashboard pentru administrator
        public async Task<IActionResult> Index()
        {
            var today = DateTime.UtcNow.Date;
            var weekAgo = DateTime.UtcNow.AddDays(-7);

            var model = new AdminDashboardViewModel
            {
                // Statistici generale
                TotalUsers = await _context.Users.CountAsync(),
                TotalPosts = await _context.Posts.CountAsync(),
                TotalGroups = await _context.Groups.CountAsync(),
                TotalComments = await _context.Comments.CountAsync(),
                TotalReactions = await _context.Likes.CountAsync(),

                // Statistici recente
                NewUsersThisWeek = await _context.Users
                    .Where(u => u.CreatedAt >= weekAgo)
                    .CountAsync(),

                NewPostsToday = await _context.Posts
                    .Where(p => p.CreatedAt >= today)
                    .CountAsync(),

                // Bonus: statistici extra
                ActiveUsersToday = await _context.Posts
                    .Where(p => p.CreatedAt >= today)
                    .Select(p => p.UserId)
                    .Distinct()
                    .CountAsync() +
                    await _context.Comments
                    .Where(c => c.CreatedAt >= today)
                    .Select(c => c.UserId)
                    .Distinct()
                    .CountAsync(),

                PendingFollowRequests = await _context.Follows
                    .Where(f => f.Status == FollowStatus.Pending)
                    .CountAsync(),

                PendingGroupRequests = await _context.GroupJoinRequests
                    .Where(r => r.Status == GroupJoinRequestStatus.Pending)
                    .CountAsync()
            };

            return View(model);
        }
    }
}