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

            // Daca exista deja useri, ne oprim 
            if (await context.Users.AnyAsync()) return;

            Console.WriteLine("🌱 Starting Massive Seed...");

            // ROLURI
            string[] roleNames = { "Administrator", "RegisteredUser" };
            foreach (var roleName in roleNames)
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }

            // UTILIZATORI
            var users = new List<ApplicationUser>
            {
                new ApplicationUser { UserName = "admin@platform.com", Email = "admin@platform.com", FullName = "Admin Platforma", IsPublic = true },
                new ApplicationUser { UserName = "alex_dev", Email = "alex@test.com", FullName = "Alexandru Popescu", Bio = "Senior Dev 💻 | C# & React", IsPublic = true, ProfilePicture = "/images/profiles/alex.jpeg" },
                new ApplicationUser { UserName = "maria_art", Email = "maria@test.com", FullName = "Maria Ionescu", Bio = "Digital Artist 🎨 | Nature Lover", IsPublic = true, ProfilePicture = "/images/profiles/maria.jpeg" },
                new ApplicationUser { UserName = "dan_photo", Email = "dan@test.com", FullName = "Dan Stanciu", Bio = "Street Photography 📸", IsPublic = false, ProfilePicture = "/images/profiles/dan.jpeg" },
                new ApplicationUser { UserName = "elena_fit", Email = "elena@test.com", FullName = "Elena Radu", Bio = "Yoga & Wellness 🧘‍♀️", IsPublic = true, ProfilePicture = "/images/profiles/elena.jpeg" },
                new ApplicationUser { UserName = "ioana_travel", Email = "ioana@test.com", FullName = "Ioana Vasile", Bio = "Travel Vlogger ✈️ | Exploring the world", IsPublic = true, ProfilePicture = "/images/profiles/ioana.jpeg" },
                new ApplicationUser { UserName = "matei_tech", Email = "matei@test.com", FullName = "Matei Georgescu", Bio = "Gadget Reviewer 📱", IsPublic = true, ProfilePicture = "/images/profiles/matei.jpeg" },
                new ApplicationUser { UserName = "user@test.com", Email = "user@test.com", FullName = "Utilizator Test", Bio = "Cont pentru testare manuală", IsPublic = true },
                new ApplicationUser { UserName = "hector_fort", Email = "hector@test.com", FullName = "Hector Fort", Bio = "Visca el Barça! 🔵🔴 | Footballer FC Barcelona", IsPublic = true, ProfilePicture = "/images/profiles/hector.jpeg", CoverPhoto = "/images/profiles/barca3.jpeg" }
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
            var hector = await userManager.FindByEmailAsync("hector@test.com");

            // RELATII DE FOLLOW
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

            // GRUPURI
            var groupDev = new Group { Name = "Programatori .NET", Description = "Discuții despre C#, ASP.NET Core, Entity Framework și arhitectură software. Împărtășim resurse și ne ajutăm reciproc la debug.", OwnerId = alex.Id, CreatedAt = DateTime.UtcNow, GroupPicture = "/images/groups/dev.jpeg" };
            var groupPhoto = new Group { Name = "Fotografie Urbană", Description = "Grup dedicat pasionaților de fotografie stradală, arhitectură și portrete urbane. Postați cele mai bune cadre aici!", OwnerId = dan.Id, CreatedAt = DateTime.UtcNow, GroupPicture = "/images/groups/photo.jpeg" };
            var groupTravel = new Group { Name = "Vacante și Călătorii", Description = "Recomandări de destinații, sfaturi pentru zboruri ieftine și jurnale de călătorie din toată lumea.", OwnerId = ioana.Id, CreatedAt = DateTime.UtcNow, GroupPicture = "/images/groups/travel.jpeg" };

            context.Groups.AddRange(groupDev, groupPhoto, groupTravel);
            await context.SaveChangesAsync();

            // MEMBRI IN GRUPURI
            var memberships = new List<GroupMember>
            {
                // adaugam adminii
                new GroupMember {UserId = alex.Id, GroupId = groupDev.Id, Role = GroupRole.Admin},
                new GroupMember {UserId = dan.Id, GroupId = groupPhoto.Id, Role = GroupRole.Admin},
                new GroupMember {UserId = ioana.Id, GroupId = groupTravel.Id, Role = GroupRole.Admin},

                // adaugam membrii normali
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

            // MESAJE IN GRUPURI
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
            catch (Exception) {  }

            // POSTARI SI COMENTARII
            var posts = new List<Post>();

            // Post 1 - Alex (Setup)
            posts.Add(new Post
            {
                UserId = alex.Id,
                Content = "Setup-ul meu de azi. Coding mode ON! ☕💻 #developer #setup",
                CreatedAt = DateTime.UtcNow.AddDays(-3),
                PostMedias = new List<PostMedia> { new PostMedia { MediaType = MediaType.Image, Url = "/images/posts/setup.jpeg" } },
                Likes = new List<Like> { new Like { UserId = maria.Id, Type = LikeType.Love }, new Like { UserId = matei.Id, Type = LikeType.Like } },
                Comments = new List<Comment> { new Comment { UserId = matei.Id, Content = "Ce tastatură folosești?", CreatedAt = DateTime.UtcNow.AddDays(-3).AddHours(1) } }
            });

            // Post 2 - Ioana (Travel)
            posts.Add(new Post
            {
                UserId = ioana.Id,
                Content = "Am ajuns în Bali! Este ireal de frumos aici. 🌴☀️",
                CreatedAt = DateTime.UtcNow.AddDays(-2),
                PostMedias = new List<PostMedia> { new PostMedia { MediaType = MediaType.Image, Url = "/images/posts/bali.jpeg" } },
                Likes = new List<Like> { new Like { UserId = alex.Id, Type = LikeType.Wow }, new Like { UserId = userTest.Id, Type = LikeType.Love } },
                Comments = new List<Comment> { new Comment { UserId = userTest.Id, Content = "Wow! Vacanță plăcută!", CreatedAt = DateTime.UtcNow.AddDays(-2).AddHours(2) } }
            });

            // Post 3 - Elena (Video)
            posts.Add(new Post
            {
                UserId = elena.Id,
                Content = "Rutina mea de dimineață. 10 minute de stretching fac minuni! 🧘‍♀️",
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                PostMedias = new List<PostMedia> { new PostMedia { MediaType = MediaType.Video, Url = "/videos/demo.mp4" } }, // Link valid youtube
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

            // Post 5 - Maria (Arta)
            posts.Add(new Post
            {
                UserId = maria.Id,
                Content = "Work in progress... aproape gata! 🎨🖌️",
                CreatedAt = DateTime.UtcNow.AddHours(-2),
                PostMedias = new List<PostMedia> { new PostMedia { MediaType = MediaType.Image, Url = "/images/posts/art.jpeg" } },
                Likes = new List<Like> { new Like { UserId = alex.Id, Type = LikeType.Love }, new Like { UserId = dan.Id, Type = LikeType.Wow } }
            });

            // Post 6 - Matei (Tech)
            posts.Add(new Post
            {
                UserId = matei.Id,
                Content = "Tocmai am testat noul iPhone. Camera este incredibilă pe timp de noapte. 🌑📱",
                CreatedAt = DateTime.UtcNow.AddMinutes(-30),
                PostMedias = new List<PostMedia> { new PostMedia { MediaType = MediaType.Image, Url = "/images/posts/iphone.jpeg" } },
                Likes = new List<Like> { new Like { UserId = userTest.Id, Type = LikeType.Like } }
            });

            // Post 7 - Hector (Meci)
            posts.Add(new Post
            {
                UserId = hector.Id,
                Content = "Victorie importantă aseară pe Camp Nou! Mulțumim fanilor pentru susținere! ⚽🔥 #ForçaBarça",
                CreatedAt = DateTime.UtcNow.AddDays(-1).AddHours(-5),
                PostMedias = new List<PostMedia> { new PostMedia { MediaType = MediaType.Image, Url = "/images/posts/barca2.jpeg" } },
                Likes = new List<Like> { new Like { UserId = userTest.Id, Type = LikeType.Love }, new Like { UserId = alex.Id, Type = LikeType.Like } },
                Comments = new List<Comment> { new Comment { UserId = userTest.Id, Content = "Cel mai bun! 💪", CreatedAt = DateTime.UtcNow.AddDays(-1).AddHours(-4) } }
            });

            // Post 8 - Hector (Antrenament)
            posts.Add(new Post
            {
                UserId = hector.Id,
                Content = "Muncă grea la antrenament. Pregătiți pentru următorul meci. 💪🔵🔴",
                CreatedAt = DateTime.UtcNow.AddHours(-3),
                PostMedias = new List<PostMedia> { new PostMedia { MediaType = MediaType.Image, Url = "/images/posts/barca1.jpeg" } },
                Likes = new List<Like> { new Like { UserId = dan.Id, Type = LikeType.Like } }
            });

            context.Posts.AddRange(posts);
            await context.SaveChangesAsync();

            Console.WriteLine("Massive Seed Completed Successfully!");
        }
    }
}