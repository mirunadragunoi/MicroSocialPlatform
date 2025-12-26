using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MicroSocialPlatform.Models;
using MicroSocialPlatform.Data;

namespace MicroSocialPlatform.ViewComponents
{
    public class PendingRequestsCountViewComponent : ViewComponent
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public PendingRequestsCountViewComponent(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            // daca userul nu e logat => nu afisam nimic
            if (!User.Identity.IsAuthenticated)
            {
                return Content(string.Empty);
            }

            var currentUser = _userManager.GetUserId(HttpContext.User);

            // numaram cererile de follow in asteptare pentru userul curent
            var pendingRequestsCount = await _context.Follows
                .CountAsync(f => f.FollowingId == currentUser && f.Status == FollowStatus.Pending);

            return View(pendingRequestsCount);
        }
    }
}
