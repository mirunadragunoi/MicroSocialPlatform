using Microsoft.AspNetCore.Identity;
using MicroSocialPlatform.Models;

namespace MicroSocialPlatform.Helpers
{
    public static class UserHelper
    {
        /// <summary>
        /// verific daca utilizatorul este administrator
        /// </summary>
        public static async Task<bool> IsAdministratorAsync(UserManager<ApplicationUser> userManager, ApplicationUser? user)
        {
            if (user == null) return false;
            return await userManager.IsInRoleAsync(user, "Administrator");
        }

        /// <summary>
        /// verific daca utilizatorul este user inregistrat (nu administrator)
        /// </summary>
        public static async Task<bool> IsRegisteredUserAsync(UserManager<ApplicationUser> userManager, ApplicationUser? user)
        {
            if (user == null) return false;
            return await userManager.IsInRoleAsync(user, "User") && 
                   !await userManager.IsInRoleAsync(user, "Administrator");
        }

        /// <summary>
        /// verific daca utilizatorul este vizitator neinregistrat (neautentificat)
        /// </summary>
        public static bool IsVisitor(System.Security.Claims.ClaimsPrincipal? user)
        {
            return user == null || !(user.Identity?.IsAuthenticated ?? false);
        }

        /// <summary>
        /// obtine tipul de utilizator ca string
        /// </summary>
        public static async Task<string> GetUserTypeAsync(UserManager<ApplicationUser> userManager, ApplicationUser? user, System.Security.Claims.ClaimsPrincipal? claimsPrincipal)
        {
            if (IsVisitor(claimsPrincipal))
                return "Visitor";

            if (user == null)
                return "Visitor";

            if (await IsAdministratorAsync(userManager, user))
                return "Administrator";

            if (await IsRegisteredUserAsync(userManager, user))
                return "User";

            return "Visitor";
        }
    }
}

