using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MicroSocialPlatform.Models;

namespace MicroSocialPlatform.ViewComponents
{
    public class UserTypeViewComponent : ViewComponent
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public UserTypeViewComponent(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            ApplicationUser? currentUser = null;
            string userType = "Visitor"; // default

            if (_signInManager.IsSignedIn(UserClaimsPrincipal))
            {
                currentUser = await _userManager.GetUserAsync(UserClaimsPrincipal);
                userType = "User"; // daca e logat, implicit este "User"
                if (currentUser != null)
                {
                    // determinam daca e Admin
                    if (await _userManager.IsInRoleAsync(currentUser, "Admin"))
                    {
                        userType = "Admin";
                    }
                }
            }

            var model = new UserTypeViewModel
            {
                User = currentUser,
                UserType = userType
            };

            return View(model);
        }
    }

    public class UserTypeViewModel
    {
        public ApplicationUser? User { get; set; }
        public string UserType { get; set; } = "Visitor";
    }
}

