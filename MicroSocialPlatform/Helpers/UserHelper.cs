using Microsoft.AspNetCore.Identity;
using MicroSocialPlatform.Models;

namespace MicroSocialPlatform.Helpers
{
    public static class UserHelper
    {
        /// <summary>
        /// Verifică dacă utilizatorul este administrator
        /// </summary>
        public static async Task<bool> IsAdministratorAsync(UserManager<ApplicationUser> userManager, ApplicationUser? user)
        {
            if (user == null) return false;
            return await userManager.IsInRoleAsync(user, "Administrator");
        }

        /// <summary>
        /// Verifică dacă utilizatorul este user înregistrat (nu administrator)
        /// </summary>
        public static async Task<bool> IsRegisteredUserAsync(UserManager<ApplicationUser> userManager, ApplicationUser? user)
        {
            if (user == null) return false;
            return await userManager.IsInRoleAsync(user, "User") && 
                   !await userManager.IsInRoleAsync(user, "Administrator");
        }

        /// <summary>
        /// Verifică dacă utilizatorul este vizitator neînregistrat (neautentificat)
        /// </summary>
        public static bool IsVisitor(System.Security.Claims.ClaimsPrincipal? user)
        {
            return (user == null | !user.Identity?.IsAuthenticated) ?? true;
        }

        /// <summary>
        /// Obține tipul de utilizator ca string
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

