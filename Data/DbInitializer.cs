using IntelligentAttendanceSystem.Models;
using System;
using System.Linq;

namespace IntelligentAttendanceSystem.Data
{
    public static class DbInitializer
    {
        public static void Initialize(ApplicationDbContext context)
        {
            context.Database.EnsureCreated();

            // Look for any users.
            if (context.Users.Any())
            {
                return;   // DB has been seeded
            }

            var users = new User[]
            {
                new User { Name = "Admin User", Email = "admin@example.com", Role = "Admin", Department = "IT", FaceIdentifier = "admin-face-id" },
                new User { Name = "Vinay Kumar", Email = "vinay@example.com", Role = "Student", Department = "Computer Science", FaceIdentifier = "vinay-face-id" },
                new User { Name = "John Smith", Email = "john@example.com", Role = "Student", Department = "BCA", FaceIdentifier = "john-face-id" }
            };

            foreach (User u in users)
            {
                context.Users.Add(u);
            }
            context.SaveChanges();
        }
    }
}
