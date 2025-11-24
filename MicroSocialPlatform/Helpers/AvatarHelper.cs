using Microsoft.AspNetCore.Mvc.Rendering;

namespace MicroSocialPlatform.Helpers
{
    public static class AvatarHelper
    {
        public static string GetAvatarUrl(string? profilePictureUrl, string? fullName, int size = 40)
        {
            if (!string.IsNullOrEmpty(profilePictureUrl))
            {
                return profilePictureUrl;
            }

            // daca nu are poza, returneaza un avatar default cu initiala
            return null;
        }

        public static string GetInitials(string? fullName)
        {
            if (string.IsNullOrEmpty(fullName))
                return "?";

            var parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 1)
                return parts[0].Substring(0, 1).ToUpper();

            return (parts[0].Substring(0, 1) + parts[^1].Substring(0, 1)).ToUpper();
        }
    }
}