using Microsoft.AspNetCore.Identity;
using MicroSocialPlatform.Models;

namespace MicroSocialPlatform.Data
{
    public class SeedData
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            // creez datele folosind RoleManager si UserManager
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            // definirea rolurilor -> Admin, RegisteredUser, UnregisteredVisitor
            string[] roleNames = { "Admin", "RegisteredUser", "UnregisteredVisitor" };
            foreach (var roleName in roleNames)
            {
                // verific daca rolul exista deja
                var roleExist = await roleManager.RoleExistsAsync(roleName);
                if (!roleExist)
                {
                    // creez rolul
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // creez administratorul
            var adminEmail = "admin@platform.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                var admin = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "Administrator Platforma",
                    EmailConfirmed = true,
                    IsPublic = true,
                    CreatedAt = DateTime.UtcNow
                };
                string adminPassword = "Admin@12345"; // Parola puternica pentru admin
                var createAdmin = await userManager.CreateAsync(admin, adminPassword);
                if (createAdmin.Succeeded)
                {
                    // atribui rolul de Admin utilizatorului creat
                    await userManager.AddToRoleAsync(admin, "Admin");
                }
            }
        }
    }
}