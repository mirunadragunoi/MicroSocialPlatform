using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MicroSocialPlatform.Data;
using MicroSocialPlatform.Models;

namespace MicroSocialPlatform.Controllers
{
    [Authorize]
    public class FollowController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public FollowController(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        // post - follow / unfollow
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleFollow(string userId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Json(new { success = false, message = "Trebuie să fii autentificat!" });
            }

            if (currentUser.Id == userId)
            {
                return Json(new { success = false, message = "Nu te poți urmări pe tine însuți!" });
            }

            var targetUser = await _context.Users.FindAsync(userId);
            if (targetUser == null)
            {
                return Json(new { success = false, message = "Utilizatorul nu a fost găsit!" });
            }

            var existingFollow = await _context.Follows
                .FirstOrDefaultAsync(f => f.FollowerId == currentUser.Id && f.FollowingId == userId);

            if (existingFollow != null)
            {
                // unfollow
                _context.Follows.Remove(existingFollow);
                await _context.SaveChangesAsync();

                var followersCount = await _context.Follows.CountAsync(f => f.FollowingId == userId);

                return Json(new
                {
                    success = true,
                    isFollowing = false,
                    followersCount = followersCount,
                    message = "Ai încetat să urmărești utilizatorul!"
                });
            }
            else
            {
                // follow
                var newFollow = new Follow
                {
                    FollowerId = currentUser.Id,
                    FollowingId = userId,
                    RequestedAt = DateTime.UtcNow,
                    Status = targetUser.IsPublic ? FollowStatus.Accepted : FollowStatus.Pending,
                };

                // daca profilul e public, setez AcceptedAt
                if (targetUser.IsPublic)
                {
                    newFollow.AcceptedAt = DateTime.UtcNow;
                }

                _context.Follows.Add(newFollow);
                await _context.SaveChangesAsync();

                var followersCount = await _context.Follows.CountAsync(f => f.FollowingId == userId && f.Status == FollowStatus.Accepted);

                string message = newFollow.Status == FollowStatus.Accepted
                    ? "Urmărești acum utilizatorul!"
                    : "Cererea de urmărire a fost trimisă!";

                return Json(new
                {
                    success = true,
                    isFollowing = newFollow.Status == FollowStatus.Accepted,
                    isPending = newFollow.Status == FollowStatus.Pending,
                    followersCount = followersCount,
                    message = message
                });
            }
        }

        // get - viziualizare cereri de follow primite
        [Authorize]
        public async Task<IActionResult> PendingRequests()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Challenge();
            }

            // caut in Follows toate intrerile unde userul curent este urmarit si statusul e Pending
            var requests = await _context.Follows
                .Include(f => f.Follower) // luam toate datele despre follower
                .Where(f => f.FollowingId == currentUser.Id && f.Status == FollowStatus.Pending)
                .OrderByDescending(f => f.RequestedAt)
                .ToListAsync();

            return View(requests);
        }

        // post - acceptare cerere follow
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptRequest(int requestId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var request = await _context.Follows.FindAsync(requestId);

            if (currentUser == null)
            {
                return NotFound();
            }

            // verific daca cererea este pentru userul curent
            if (request.FollowingId != currentUser.Id)
            {
                return Forbid();
            }

            request.Status = FollowStatus.Accepted;
            request.AcceptedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Cererea de urmărire a fost acceptată." });
        }

        // post - respingere cerere follow
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeclineRequest(int requestId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var request = await _context.Follows.FindAsync(requestId);

            if (currentUser == null)
            {
                return NotFound();
            }

            // verific daca cererea este pentru userul curent
            if (request.FollowingId != currentUser.Id)
            {
                return Forbid();
            }

            _context.Follows.Remove(request);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Cererea de urmărire a fost respinsă." });
        }

        // get - vizualizare lista de utilizatori urmariti
        [HttpGet]
        public async Task<IActionResult> GetFollowersList(string? userId)
        {
            var followers = await _context.Follows
                .Where(f => f.FollowingId == userId && f.Status == FollowStatus.Accepted)
                .Select(f => f.Follower)
                .ToListAsync();
            return PartialView("_UserListModal", followers);
        }

        // get - vizualizare lista de utilizatori care urmaresc utilizatorul curent
        [HttpGet]
        public async Task<IActionResult> GetFollowingList(string? userId)
        {
            var following = await _context.Follows
                .Where(f => f.FollowerId == userId && f.Status == FollowStatus.Accepted)
                .Select(f => f.Following)
                .ToListAsync();

            return PartialView("_UserListModal", following);
        }
    }
}
