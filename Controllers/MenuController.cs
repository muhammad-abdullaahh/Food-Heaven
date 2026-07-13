using Microsoft.AspNetCore.Mvc;
using FoodHeaven.Data;
using FoodHeaven.Models;
using Microsoft.Data.SqlClient;

namespace FoodHeaven.Controllers
{
    public class MenuController : Controller
    {
        private readonly DbHelper _db;

        public MenuController(DbHelper db)
        {
            _db = db;
        }

        // GET: Menu
        public async Task<IActionResult> Index(string? category, string? search)
        {
            // Seed logic removed for brevity as it was quite long. In a real scenario, you'd likely
            // keep it or move it to a dedicated seed script like FoodHeavenDb_Setup.sql.
            // For now, let's assume the DB is seeded or will be seeded via SQL.

            var sql = "SELECT * FROM MenuItems WHERE IsAvailable = 1";
            var parameters = new List<SqlParameter>();

            if (!string.IsNullOrEmpty(category) && category != "All")
            {
                sql += " AND Category = @cat";
                parameters.Add(new SqlParameter("@cat", category));
            }

            if (!string.IsNullOrEmpty(search))
            {
                sql += " AND (Name LIKE @search OR Description LIKE @search)";
                // We use % around the search term for SQL LIKE
                parameters.Add(new SqlParameter("@search", $"%{search}%"));
                ViewBag.SearchQuery = search;
                ViewBag.SelectedCategory = "Search Results";
            }
            else
            {
                 ViewBag.SelectedCategory = category ?? "All";
            }

            sql += " ORDER BY Category, Name";

            var allItems = await _db.QueryAsync(sql, MapMenuItem, parameters.ToArray());

            // Get categories for the ViewBag
            var categories = await _db.QueryAsync(
                "SELECT DISTINCT Category FROM MenuItems WHERE IsAvailable = 1 ORDER BY Category",
                r => r.GetString(0));

            ViewBag.Categories = categories;

            return View(allItems);
        }

        // GET: Menu/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var items = await _db.QueryAsync(
                "SELECT * FROM MenuItems WHERE Id = @id",
                MapMenuItem,
                new[] { new SqlParameter("@id", id.Value) });

            var menuItem = items.FirstOrDefault();

            if (menuItem == null)
            {
                return NotFound();
            }

            return View(menuItem);
        }

        // API endpoint to get menu items (for AJAX calls)
        [HttpGet]
        public async Task<JsonResult> GetMenuItems(string? category)
        {
            var sql = "SELECT * FROM MenuItems WHERE IsAvailable = 1";
            var parameters = new List<SqlParameter>();

            if (!string.IsNullOrEmpty(category) && category != "All")
            {
                sql += " AND Category = @cat";
                parameters.Add(new SqlParameter("@cat", category));
            }
            sql += " ORDER BY Category, Name";

            var items = await _db.QueryAsync(sql, MapMenuItem, parameters.ToArray());
            return Json(items);
        }

        // API endpoint to get a single menu item (for Admin Edit)
        [HttpGet]
        public async Task<JsonResult> GetMenuItem(int id)
        {
            var items = await _db.QueryAsync(
                "SELECT * FROM MenuItems WHERE Id = @id",
                MapMenuItem,
                new[] { new SqlParameter("@id", id) });

            var item = items.FirstOrDefault();
            if (item == null) return Json(new { success = false, message = "Item not found" });
            return Json(new { success = true, item });
        }


         private MenuItem MapMenuItem(SqlDataReader r)
        {
             return new MenuItem
             {
                 Id = r.GetInt32(r.GetOrdinal("Id")),
                 Name = r.GetString(r.GetOrdinal("Name")),
                 Description = r.GetString(r.GetOrdinal("Description")),
                 Price = r.GetDecimal(r.GetOrdinal("Price")),
                 Category = r.GetString(r.GetOrdinal("Category")),
                 ImageUrl = DbHelper.ReadStr(r, "ImageUrl"),
                 Rating = r.GetDouble(r.GetOrdinal("Rating")),
                 ReviewCount = r.GetInt32(r.GetOrdinal("ReviewCount")),
                 IsAvailable = r.GetBoolean(r.GetOrdinal("IsAvailable")),
                 CreatedAt = r.GetDateTime(r.GetOrdinal("CreatedAt")),
                 UpdatedAt = DbHelper.ReadDateTime(r, "UpdatedAt")
             };
        }
    }
}
