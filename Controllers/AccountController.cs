using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using FoodHeaven.Data;
using FoodHeaven.Models;
using Microsoft.Data.SqlClient;
using System.Security.Claims;

namespace FoodHeaven.Controllers
{
    public class AccountController : Controller
    {
        private readonly DbHelper _db;

        public AccountController(DbHelper db)
        {
            _db = db;
        }

        // GET: Account/Login
        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                if (User.IsInRole("Admin"))
                    return RedirectToAction("Dashboard", "Admin");
                return RedirectToAction("Index", "Menu");
            }
            return View();
        }

        // POST: Account/Login (Unified)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password)
        {
            // 1. Check Admin
            var admins = await _db.QueryAsync(
                "SELECT * FROM Admins WHERE (Username = @u OR Email = @u) AND IsActive = 1",
                r => new Admin
                {
                    Id           = r.GetInt32(r.GetOrdinal("Id")),
                    Username     = r.GetString(r.GetOrdinal("Username")),
                    Email        = r.GetString(r.GetOrdinal("Email")),
                    PasswordHash = r.GetString(r.GetOrdinal("PasswordHash")),
                    FullName     = r.GetString(r.GetOrdinal("FullName")),
                    IsActive     = r.GetBoolean(r.GetOrdinal("IsActive"))
                },
                new[] { new SqlParameter("@u", username) });

            var admin = admins.FirstOrDefault(a => BCrypt.Net.BCrypt.Verify(password, a.PasswordHash));
            if (admin != null)
            {
                await _db.ExecuteAsync(
                    "UPDATE Admins SET LastLoginAt = @d WHERE Id = @id",
                    new[] { new SqlParameter("@d", DateTime.Now), new SqlParameter("@id", admin.Id) });

                await SignInUser(admin.Id.ToString(), admin.Username, admin.Email, "Admin",
                    new[] { new Claim("FullName", admin.FullName) });

                return RedirectToAction("Dashboard", "Admin");
            }

            // 2. Check regular User
            var users = await _db.QueryAsync(
                "SELECT * FROM Users WHERE Email = @u OR Username = @u",
                r => new User
                {
                    Id           = r.GetInt32(r.GetOrdinal("Id")),
                    Username     = r.GetString(r.GetOrdinal("Username")),
                    Email        = r.GetString(r.GetOrdinal("Email")),
                    PasswordHash = r.GetString(r.GetOrdinal("PasswordHash"))
                },
                new[] { new SqlParameter("@u", username) });

            var user = users.FirstOrDefault(u => BCrypt.Net.BCrypt.Verify(password, u.PasswordHash));
            if (user != null)
            {
                await _db.ExecuteAsync(
                    "UPDATE Users SET LastLoginAt = @d WHERE Id = @id",
                    new[] { new SqlParameter("@d", DateTime.Now), new SqlParameter("@id", user.Id) });

                await SignInUser(user.Id.ToString(), user.Username, user.Email, "User");
                return RedirectToAction("Index", "Menu");
            }

            TempData["ErrorMessage"] = "Invalid username or password";
            return View();
        }

        // POST: Account/AdminLogin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdminLogin(string username, string password)
        {
            var admins = await _db.QueryAsync(
                "SELECT * FROM Admins WHERE Username = @u AND IsActive = 1",
                r => new Admin
                {
                    Id           = r.GetInt32(r.GetOrdinal("Id")),
                    Username     = r.GetString(r.GetOrdinal("Username")),
                    Email        = r.GetString(r.GetOrdinal("Email")),
                    PasswordHash = r.GetString(r.GetOrdinal("PasswordHash")),
                    FullName     = r.GetString(r.GetOrdinal("FullName"))
                },
                new[] { new SqlParameter("@u", username) });

            var admin = admins.FirstOrDefault(a => BCrypt.Net.BCrypt.Verify(password, a.PasswordHash));
            if (admin != null)
            {
                await _db.ExecuteAsync(
                    "UPDATE Admins SET LastLoginAt = @d WHERE Id = @id",
                    new[] { new SqlParameter("@d", DateTime.Now), new SqlParameter("@id", admin.Id) });

                await SignInUser(admin.Id.ToString(), admin.Username, admin.Email, "Admin",
                    new[] { new Claim("FullName", admin.FullName) });

                return RedirectToAction("Dashboard", "Admin");
            }

            TempData["ErrorMessage"] = "Invalid admin credentials";
            return View("Login");
        }

        // GET: Account/Register
        public IActionResult Register() => View();

        // POST: Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(string username, string email, string password)
        {
            var exists = await _db.ScalarAsync<int>(
                "SELECT COUNT(*) FROM Users WHERE Email = @e",
                new[] { new SqlParameter("@e", email) });

            if (exists > 0)
            {
                TempData["ErrorMessage"] = "Email is already registered.";
                return View();
            }

            await _db.ExecuteAsync(
                @"INSERT INTO Users (Username, Email, PasswordHash, Password, CreatedAt)
                  VALUES (@u, @e, @ph, @pw, @ca)",
                new[]
                {
                    new SqlParameter("@u",  username),
                    new SqlParameter("@e",  email),
                    new SqlParameter("@ph", BCrypt.Net.BCrypt.HashPassword(password)),
                    new SqlParameter("@pw", password),
                    new SqlParameter("@ca", DateTime.Now)
                });

            TempData["SuccessMessage"] = "Registration successful! Please login.";
            return RedirectToAction(nameof(Login));
        }

        // GET: Account/Logout
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return View();
        }

        // ── Private helper ─────────────────────────────────────────────
        private async Task SignInUser(string id, string name, string email, string role,
            IEnumerable<Claim>? extraClaims = null)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, id),
                new Claim(ClaimTypes.Name,  name),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Role,  role)
            };
            if (extraClaims != null) claims.AddRange(extraClaims);

            var identity   = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var properties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc   = DateTimeOffset.UtcNow.AddHours(8)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity),
                properties);
        }
    }
}
