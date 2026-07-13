using FoodHeaven.Data;
using FoodHeaven.Models;
using Microsoft.Data.SqlClient;

namespace FoodHeaven.Data
{
    public static class SeedAdmins
    {
        public static async Task Initialize(DbHelper db)
        {
            var admins = new[]
            {
                new Admin
                {
                    Username     = "admin",
                    Email        = "admin@foodheaven.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin@123"),
                    PlainPassword = "admin@123",
                    FullName     = "Admin User",
                    IsActive     = true,
                    CreatedAt    = DateTime.Now
                },
                new Admin
                {
                    Username     = "Muhammad Asad",
                    Email        = "asad.arshad@foodheaven.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("asad.arshad"),
                    PlainPassword = "asad.arshad",
                    FullName     = "Muhammad Asad",
                    IsActive     = true,
                    CreatedAt    = DateTime.Now
                }
            };

            foreach (var admin in admins)
            {
                // Check if already exists
                var existingId = await db.ScalarAsync<int?>(
                    "SELECT Id FROM Admins WHERE Username = @u",
                    new[] { new SqlParameter("@u", admin.Username) });

                if (existingId == null)
                {
                    // Insert new admin
                    await db.ExecuteAsync(
                        @"INSERT INTO Admins (Username, Email, PasswordHash, PlainPassword, FullName, IsActive, CreatedAt)
                          VALUES (@u, @e, @ph, @pp, @fn, @ia, @ca)",
                        new[]
                        {
                            new SqlParameter("@u",  admin.Username),
                            new SqlParameter("@e",  admin.Email),
                            new SqlParameter("@ph", admin.PasswordHash),
                            new SqlParameter("@pp", admin.PlainPassword ?? (object)DBNull.Value),
                            new SqlParameter("@fn", admin.FullName),
                            new SqlParameter("@ia", admin.IsActive),
                            new SqlParameter("@ca", admin.CreatedAt)
                        });
                }
                else
                {
                    // Update password hash
                    await db.ExecuteAsync(
                        @"UPDATE Admins SET PasswordHash = @ph, PlainPassword = @pp, FullName = @fn
                          WHERE Username = @u",
                        new[]
                        {
                            new SqlParameter("@ph", admin.PasswordHash),
                            new SqlParameter("@pp", admin.PlainPassword ?? (object)DBNull.Value),
                            new SqlParameter("@fn", admin.FullName),
                            new SqlParameter("@u",  admin.Username)
                        });
                }
            }
        }
    }
}
