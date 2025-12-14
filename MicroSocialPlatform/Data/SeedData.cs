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

            // definirea rolurilor -> Administrator, RegisteredUser, UnregisteredVisitor
            string[] roleNames = { "Administrator", "RegisteredUser", "UnregisteredVisitor" };
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
            var administratorEmail = "administrator@platform.com";
            var administratorUser = await userManager.FindByEmailAsync(administratorEmail);
            if (administratorUser == null)
            {
                var administrator = new ApplicationUser
                {
                    UserName = administratorEmail,
                    Email = administratorEmail,
                    FullName = "Administrator Platforma",
                    EmailConfirmed = true,
                    IsPublic = true,
                    CreatedAt = DateTime.UtcNow
                };
                string administratorPassword = "Admin@12345"; // Parola puternica pentru admin
                var createAdministrator = await userManager.CreateAsync(administrator, administratorPassword);
                if (createAdministrator.Succeeded)
                {
                    // atribui rolul de Admin utilizatorului creat
                    await userManager.AddToRoleAsync(administrator, "Administrator");
                }
            }

            // creez utilizatorul de test
            var testUserEmail = "user@test.com";
            var testUser = await userManager.FindByEmailAsync(testUserEmail);
            if (testUser == null)
            {
                var user = new ApplicationUser
                {
                    UserName = testUserEmail,
                    Email = testUserEmail,
                    FullName = "Utilizator Test",
                    EmailConfirmed = true,
                    IsPublic = true,
                    CreatedAt = DateTime.UtcNow
                };
                string userPassword = "User@12345"; // Parola puternica pentru utilizatorul de test
                var createUser = await userManager.CreateAsync(user, userPassword);
                if (createUser.Succeeded)
                {
                    // atribui rolul de RegisteredUser utilizatorului creat
                    await userManager.AddToRoleAsync(user, "RegisteredUser");
                }
            }
        }
    }
}