using MicroSocialPlatform.Data;
using MicroSocialPlatform.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// modific ca sa integrez rolurile
builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false; // n-am nevoie momentan de confirmarea de email
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 6;
    options.User.RequireUniqueEmail = true;
})
    .AddRoles<IdentityRole>() // suport pentru roluri
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddControllersWithViews();

// Configurare autorizare pentru diferite tipuri de utilizatori
builder.Services.AddAuthorization(options =>
{
    // Politica pentru administratori
    options.AddPolicy("RequireAdministrator", policy => 
        policy.RequireRole("Administrator"));
    
    // Politica pentru utilizatori înregistrați (User sau Administrator)
    options.AddPolicy("RequireRegisteredUser", policy => 
        policy.RequireRole("User", "Administrator"));
    
    // Politica pentru vizitatori (permite și utilizatori neautentificați)
    // Nu este nevoie de o politică specială, deoarece implicit toate rutele sunt accesibile
});

var app = builder.Build();

// seed roles si admin user
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        await SeedRolesAndAdmin(services);
    }
    catch (Exception ex) 
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "A aparut o eroare la seed-area rolurilor si a adminului!!!");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages()
   .WithStaticAssets();

app.Run();

// metoda pentru roluri
async Task SeedRolesAndAdmin(IServiceProvider serviceProvider)
{
    var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    // creez cele 2 roluri principale
    string[] roleNames = { "Administrator", "User" };

    foreach (var roleName in roleNames)
    {
        // verific daca rolul exista deja, altfel il creez
        var roleExists = await roleManager.RoleExistsAsync(roleName);
        if (!roleExists)
        {
            await roleManager.CreateAsync(new IdentityRole(roleName));
        }
    }

    // creez utilizatorul administrator implicit
    var adminEmail = "admin@microsocial.com";
    var adminUser = await userManager.FindByEmailAsync(adminEmail);

    if (adminUser == null)
    {
        // creez adminul
        adminUser = new ApplicationUser
        {
            UserName = adminEmail, // folosesc email-ul ca username pentru consistență
            Email = adminEmail,
            FullName = "Administrator",
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow
        };

        // creez userul cu parola
        var createResult = await userManager.CreateAsync(adminUser, "Admin@123");

        if (createResult.Succeeded)
        {
            // adaug rolul de administrator
            await userManager.AddToRoleAsync(adminUser, "Administrator");
        }
    }
    else
    {
        // daca administratorul exista deja, actualizez username-ul la email pentru consistenta
        if (adminUser.UserName != adminEmail)
        {
            adminUser.UserName = adminEmail;
            await userManager.UpdateAsync(adminUser);
        }
        
        // ma asigur ca are rolul de administrator
        var isInRole = await userManager.IsInRoleAsync(adminUser, "Administrator");
        if (!isInRole)
        {
            await userManager.AddToRoleAsync(adminUser, "Administrator");
        }
    }
}