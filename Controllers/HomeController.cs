using Microsoft.AspNetCore.Mvc;
using FoodHeaven.Data;
using FoodHeaven.Models;
using Microsoft.Data.SqlClient;

namespace FoodHeaven.Controllers
{
    public class HomeController : Controller
    {
        private readonly DbHelper _db;

        public HomeController(DbHelper db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var featuredItems = await _db.QueryAsync(
                @"SELECT TOP 5 * FROM MenuItems 
                  WHERE IsAvailable = 1 
                  ORDER BY Rating DESC",
                r => new MenuItem
                {
                    Id = r.GetInt32(r.GetOrdinal("Id")),
                    Name = r.GetString(r.GetOrdinal("Name")),
                    Description = r.GetString(r.GetOrdinal("Description")),
                    Price = DbHelper.ReadDecimal(r, "Price"),
                    Category = r.GetString(r.GetOrdinal("Category")),
                    ImageUrl = DbHelper.ReadStr(r, "ImageUrl"),
                    Rating = r.GetDouble(r.GetOrdinal("Rating")),
                    ReviewCount = r.GetInt32(r.GetOrdinal("ReviewCount")),
                    IsAvailable = r.GetBoolean(r.GetOrdinal("IsAvailable"))
                });

            ViewBag.FeaturedItems = featuredItems;
            return View();
        }

        public IActionResult About()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Subscribe([FromBody] string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return Json(new { success = false, message = "Email address is required." });
            }

            if (!new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(email))
            {
                 return Json(new { success = false, message = "Please enter a valid email address." });
            }

            // Check if already subscribed
            var count = await _db.ScalarAsync<int>(
                "SELECT COUNT(*) FROM Subscribers WHERE Email = @e",
                new[] { new SqlParameter("@e", email) });

            if (count > 0)
            {
                return Json(new { success = false, message = "You are already subscribed!" });
            }

            await _db.ExecuteAsync(
                "INSERT INTO Subscribers (Email, SubscribedAt) VALUES (@e, @d)",
                new[] { 
                    new SqlParameter("@e", email),
                    new SqlParameter("@d", DateTime.Now)
                });

            return Json(new { success = true, message = "Welcome to the FoodHeaven Family!" });
        }
    }
}
