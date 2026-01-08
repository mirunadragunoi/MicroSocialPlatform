using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MicroSocialPlatform.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MicroSocialPlatform.Data
{
    public class SeedData
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var config = serviceProvider.GetRequiredService<IConfiguration>();

            // Verificare flag
            if (!config.GetValue<bool>("SeedDatabase")) return;

            // Asiguram crearea DB
            context.Database.EnsureCreated();

            // Daca exista deja useri, ne oprim (presupunand ca am sters baza inainte, asta va rula)
            if (await context.Users.AnyAsync()) return;

            Console.WriteLine("🌱 Starting Massive Seed...");

            // ================= 1. ROLURI =================
            string[] roleNames = { "Administrator", "RegisteredUser" };
            foreach (var roleName in roleNames)
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }

            // ================= 2. UTILIZATORI (8 buc) =================
            var users = new List<ApplicationUser>
            {
                new ApplicationUser { UserName = "admin@platform.com", Email = "admin@platform.com", FullName = "Admin Platforma", IsPublic = true, ProfilePicture = "https://ui-avatars.com/api/?name=Admin&background=000&color=fff" },
                new ApplicationUser { UserName = "alex_dev", Email = "alex@test.com", FullName = "Alexandru Popescu", Bio = "Senior Dev 💻 | C# & React", IsPublic = true, ProfilePicture = "https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?w=400&h=400&fit=crop" },
                new ApplicationUser { UserName = "maria_art", Email = "maria@test.com", FullName = "Maria Ionescu", Bio = "Digital Artist 🎨 | Nature Lover", IsPublic = true, ProfilePicture = "https://images.unsplash.com/photo-1494790108377-be9c29b29330?w=400&h=400&fit=crop" },
                new ApplicationUser { UserName = "dan_photo", Email = "dan@test.com", FullName = "Dan Stanciu", Bio = "Street Photography 📸", IsPublic = false, ProfilePicture = "https://images.unsplash.com/photo-1500648767791-00dcc994a43e?w=400&h=400&fit=crop" },
                new ApplicationUser { UserName = "elena_fit", Email = "elena@test.com", FullName = "Elena Radu", Bio = "Yoga & Wellness 🧘‍♀️", IsPublic = true, ProfilePicture = "https://images.unsplash.com/photo-1438761681033-6461ffad8d80?w=400&h=400&fit=crop" },
                new ApplicationUser { UserName = "ioana_travel", Email = "ioana@test.com", FullName = "Ioana Vasile", Bio = "Travel Vlogger ✈️ | Exploring the world", IsPublic = true, ProfilePicture = "https://images.unsplash.com/photo-1544005313-94ddf0286df2?w=400&h=400&fit=crop" },
                new ApplicationUser { UserName = "matei_tech", Email = "matei@test.com", FullName = "Matei Georgescu", Bio = "Gadget Reviewer 📱", IsPublic = true, ProfilePicture = "https://images.unsplash.com/photo-1506794778202-cad84cf45f1d?w=400&h=400&fit=crop" },
                new ApplicationUser { UserName = "user@test.com", Email = "user@test.com", FullName = "Utilizator Test", Bio = "Cont pentru testare manuală", IsPublic = true, ProfilePicture = "https://ui-avatars.com/api/?name=User+Test&background=random" }
            };

            foreach (var u in users)
            {
                u.EmailConfirmed = true;
                u.CreatedAt = DateTime.UtcNow;
                var res = await userManager.CreateAsync(u, "Parola123!");
                if (res.Succeeded)
                {
                    await userManager.AddToRoleAsync(u, u.Email.StartsWith("admin") ? "Administrator" : "RegisteredUser");
                }
            }
            // Salvam DB pentru a avea ID-uri
            await context.SaveChangesAsync();

            // Reincarcam userii cu ID-urile reale
            var admin = await userManager.FindByEmailAsync("admin@platform.com");
            var alex = await userManager.FindByEmailAsync("alex@test.com");
            var maria = await userManager.FindByEmailAsync("maria@test.com");
            var dan = await userManager.FindByEmailAsync("dan@test.com");
            var elena = await userManager.FindByEmailAsync("elena@test.com");
            var ioana = await userManager.FindByEmailAsync("ioana@test.com");
            var matei = await userManager.FindByEmailAsync("matei@test.com");
            var userTest = await userManager.FindByEmailAsync("user@test.com");

            // ================= 3. RELATII FOLLOW =================
            var follows = new List<Follow>
            {
                new Follow { FollowerId = alex.Id, FollowingId = maria.Id, Status = FollowStatus.Accepted },
                new Follow { FollowerId = alex.Id, FollowingId = dan.Id, Status = FollowStatus.Accepted },
                new Follow { FollowerId = maria.Id, FollowingId = alex.Id, Status = FollowStatus.Accepted },
                new Follow { FollowerId = maria.Id, FollowingId = elena.Id, Status = FollowStatus.Accepted },
                new Follow { FollowerId = userTest.Id, FollowingId = alex.Id, Status = FollowStatus.Accepted },
                new Follow { FollowerId = userTest.Id, FollowingId = maria.Id, Status = FollowStatus.Accepted },
                new Follow { FollowerId = userTest.Id, FollowingId = ioana.Id, Status = FollowStatus.Accepted },
                new Follow { FollowerId = ioana.Id, FollowingId = userTest.Id, Status = FollowStatus.Accepted },
                // Cerere in asteptare
                new Follow { FollowerId = matei.Id, FollowingId = dan.Id, Status = FollowStatus.Pending }
            };
            context.Follows.AddRange(follows);

            // ================= 4. GRUPURI =================
            var groupDev = new Group { Name = "Programatori .NET", Description = "Discuții despre C#, ASP.NET Core, Entity Framework și arhitectură software. Împărtășim resurse și ne ajutăm reciproc la debug.", OwnerId = alex.Id, CreatedAt = DateTime.UtcNow, GroupPicture = "https://images.unsplash.com/photo-1587620962725-abab7fe55159?w=400&h=400&fit=crop" };
            var groupPhoto = new Group { Name = "Fotografie Urbană", Description = "Grup dedicat pasionaților de fotografie stradală, arhitectură și portrete urbane. Postați cele mai bune cadre aici!", OwnerId = dan.Id, CreatedAt = DateTime.UtcNow, GroupPicture = "https://images.unsplash.com/photo-1516035069371-29a1b244cc32?w=400&h=400&fit=crop" };
            var groupTravel = new Group { Name = "Vacante și Călătorii", Description = "Recomandări de destinații, sfaturi pentru zboruri ieftine și jurnale de călătorie din toată lumea.", OwnerId = ioana.Id, CreatedAt = DateTime.UtcNow, GroupPicture = "https://images.unsplash.com/photo-1476514525535-07fb3b4ae5f1?w=400&h=400&fit=crop" };

            context.Groups.AddRange(groupDev, groupPhoto, groupTravel);
            await context.SaveChangesAsync();

            // Membri in grupuri
            var memberships = new List<GroupMember>
            {
                new GroupMember { UserId = maria.Id, GroupId = groupDev.Id },
                new GroupMember { UserId = matei.Id, GroupId = groupDev.Id },
                new GroupMember { UserId = userTest.Id, GroupId = groupDev.Id },

                new GroupMember { UserId = alex.Id, GroupId = groupPhoto.Id },
                new GroupMember { UserId = userTest.Id, GroupId = groupPhoto.Id },

                new GroupMember { UserId = maria.Id, GroupId = groupTravel.Id },
                new GroupMember { UserId = userTest.Id, GroupId = groupTravel.Id }
            };
            context.GroupMembers.AddRange(memberships);
            await context.SaveChangesAsync();

            // ================= 5. MESAJE IN GRUP =================
            // Verificam daca ai modelul GroupMessage. Daca nu, sterge sectiunea asta.
            try
            {
                var messages = new List<GroupMessage>
                {
                    new()
                    {
                        Content = "Salutare tuturor! Ce proiecte mai lucrați?",
                        UserId = alex.Id,
                        GroupId = groupDev.Id,
                        SentAt = DateTime.UtcNow.AddDays(-2)
                    },
                    new()
                    {
                        Content = "Eu lucrez la o aplicație de Social Media 🚀",
                        UserId = matei.Id,
                        GroupId = groupDev.Id,
                        SentAt = DateTime.UtcNow.AddDays(-2).AddMinutes(15)
                    },
                    new()
                    {
                        Content = "Și eu! Folosesc MVC.",
                        UserId = userTest.Id,
                        GroupId = groupDev.Id,
                        SentAt = DateTime.UtcNow.AddDays(-2).AddMinutes(20)
                    },
                    new()
                    {
                        Content = "Cine merge la photowalk sâmbătă?",
                        UserId = dan.Id,
                        GroupId = groupPhoto.Id,
                        SentAt = DateTime.UtcNow.AddDays(-1)
                    },
                    new()
                    {
                        Content = "Vin eu!",
                        UserId = alex.Id,
                        GroupId = groupPhoto.Id,
                        SentAt = DateTime.UtcNow.AddDays(-1).AddHours(1)
                    }
                };
                context.GroupMessages.AddRange(messages);
            }
            catch (Exception) { /* Ignoram daca nu ai modelul inca */ }

            // ================= 6. POSTARI & COMENTARII =================
            var posts = new List<Post>();

            // Post 1 - Alex (Imagine)
            posts.Add(new Post
            {
                UserId = alex.Id,
                Content = "Setup-ul meu de azi. Coding mode ON! ☕💻 #developer #setup",
                CreatedAt = DateTime.UtcNow.AddDays(-3),
                PostMedias = new List<PostMedia> { new PostMedia { MediaType = MediaType.Image, Url = "https://images.unsplash.com/photo-1498050108023-c5249f4df085?w=1200" } },
                Likes = new List<Like> { new Like { UserId = maria.Id, Type = LikeType.Love }, new Like { UserId = matei.Id, Type = LikeType.Like } },
                Comments = new List<Comment> { new Comment { UserId = matei.Id, Content = "Ce tastatură folosești?", CreatedAt = DateTime.UtcNow.AddDays(-3).AddHours(1) } }
            });

            // Post 2 - Ioana (Travel)
            posts.Add(new Post
            {
                UserId = ioana.Id,
                Content = "Am ajuns în Bali! Este ireal de frumos aici. 🌴☀️",
                CreatedAt = DateTime.UtcNow.AddDays(-2),
                PostMedias = new List<PostMedia> { new PostMedia { MediaType = MediaType.Image, Url = "https://images.unsplash.com/photo-1537996194471-e657df975ab4?w=1200" } },
                Likes = new List<Like> { new Like { UserId = alex.Id, Type = LikeType.Wow }, new Like { UserId = userTest.Id, Type = LikeType.Love } },
                Comments = new List<Comment> { new Comment { UserId = userTest.Id, Content = "Wow! Vacanță plăcută!", CreatedAt = DateTime.UtcNow.AddDays(-2).AddHours(2) } }
            });

            // Post 3 - Elena (Video Youtube)
            posts.Add(new Post
            {
                UserId = elena.Id,
                Content = "Rutina mea de dimineață. 10 minute de stretching fac minuni! 🧘‍♀️",
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                PostMedias = new List<PostMedia> { new PostMedia { MediaType = MediaType.Video, Url = "https://www.youtube.com/watch?v=sTANio_2E0Q" } }, // Link valid youtube
                Likes = new List<Like> { new Like { UserId = maria.Id, Type = LikeType.Like } }
            });

            // Post 4 - UserTest (Text)
            posts.Add(new Post
            {
                UserId = userTest.Id,
                Content = "Salut comunitate! Aceasta este prima mea postare de test. Îmi place mult noua platformă. 👍",
                CreatedAt = DateTime.UtcNow.AddHours(-5),
                Likes = new List<Like> { new Like { UserId = alex.Id, Type = LikeType.Like }, new Like { UserId = admin.Id, Type = LikeType.Like } },
                Comments = new List<Comment> { new Comment { UserId = alex.Id, Content = "Bine ai venit pe Agora!", CreatedAt = DateTime.UtcNow.AddHours(-4) } }
            });

            // Post 5 - Maria (Art Gallery - Multiple Images daca suporta, pun una momentan)
            posts.Add(new Post
            {
                UserId = maria.Id,
                Content = "Work in progress... aproape gata! 🎨🖌️",
                CreatedAt = DateTime.UtcNow.AddHours(-2),
                PostMedias = new List<PostMedia> { new PostMedia { MediaType = MediaType.Image, Url = "https://images.unsplash.com/photo-1513364776144-60967b0f800f?w=1200" } },
                Likes = new List<Like> { new Like { UserId = alex.Id, Type = LikeType.Love }, new Like { UserId = dan.Id, Type = LikeType.Wow } }
            });

            // Post 6 - Matei (Tech)
            posts.Add(new Post
            {
                UserId = matei.Id,
                Content = "Tocmai am testat noul iPhone. Camera este incredibilă pe timp de noapte. 🌑📱",
                CreatedAt = DateTime.UtcNow.AddMinutes(-30),
                PostMedias = new List<PostMedia> { new PostMedia { MediaType = MediaType.Image, Url = "https://images.unsplash.com/photo-1512428559087-560fa5ce7d02?w=1200" } },
                Likes = new List<Like> { new Like { UserId = userTest.Id, Type = LikeType.Like } }
            });

            context.Posts.AddRange(posts);
            await context.SaveChangesAsync();

            Console.WriteLine("✅ Massive Seed Completed Successfully!");
        }
    }
}