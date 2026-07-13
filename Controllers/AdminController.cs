using Microsoft.AspNetCore.Mvc;
using FoodHeaven.Data;
using FoodHeaven.Models;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Authorization;

namespace FoodHeaven.Controllers
{
    public class AdminController : Controller
    {
        private readonly DbHelper _db;

        public AdminController(DbHelper db)
        {
            _db = db;
        }

        // GET: Admin/Login
        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true && User.IsInRole("Admin"))
            {
                return RedirectToAction(nameof(Dashboard));
            }
            return View();
        }

        // POST: Admin/Login handled in AccountController unified login now.  Keeping this in case of direct posts here to redirect correctly.
        // Or if you kept the separate AdminLogin action here:
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password)
        {
           // Redirecting to AccountController to handle all logins uniformly.
           return RedirectToAction("Login", "Account");
        }

        // GET: Admin/Logout
        public IActionResult Logout()
        {
           return RedirectToAction("Logout", "Account");
        }

        // GET: Admin/Dashboard
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Dashboard()
        {
            var today = DateTime.Today;

            ViewBag.TotalOrders = await _db.ScalarAsync<int>("SELECT COUNT(*) FROM Orders");
            
            var orderRevenue = await _db.ScalarAsync<decimal>(
                "SELECT ISNULL(SUM(Total), 0) FROM Orders WHERE Status IN ('Completed', 'Delivered')");
            
            var premiumRevenue = await _db.ScalarAsync<decimal>(
                "SELECT ISNULL(SUM(EstimatedCost), 0) FROM Reservations WHERE Status = 'Completed'");

            ViewBag.TotalRevenue = orderRevenue + premiumRevenue;
            ViewBag.PremiumRevenue = premiumRevenue;
            
            ViewBag.TotalReservations = await _db.ScalarAsync<int>("SELECT COUNT(*) FROM Reservations");
            
            ViewBag.PendingOrders = await _db.ScalarAsync<int>(
                "SELECT COUNT(*) FROM Orders WHERE Status = 'Pending'");
                
            ViewBag.UnreadMessages = await _db.ScalarAsync<int>(
                "SELECT COUNT(*) FROM ContactMessages WHERE IsRead = 0");

            ViewBag.TodaysReservations = await _db.ScalarAsync<int>(
                "SELECT COUNT(*) FROM Reservations WHERE CAST(ReservationDate AS DATE) = @d",
                new[] { new SqlParameter("@d", today) });

            // Get recent orders
            ViewBag.RecentOrders = await _db.QueryAsync(
                "SELECT TOP 5 * FROM Orders ORDER BY OrderDate DESC",
                r => new Order
                {
                    Id = r.GetInt32(r.GetOrdinal("Id")),
                    OrderNumber = r.GetString(r.GetOrdinal("OrderNumber")),
                    CustomerName = DbHelper.ReadStr(r, "CustomerName"),
                    Total = DbHelper.ReadDecimal(r, "Total"),
                    Status = r.GetString(r.GetOrdinal("Status")),
                    OrderDate = r.GetDateTime(r.GetOrdinal("OrderDate"))
                });

            // Get recent messages
            ViewBag.RecentMessages = await _db.QueryAsync(
                "SELECT TOP 5 * FROM ContactMessages ORDER BY CreatedAt DESC",
                r => new ContactMessage
                {
                    Id = r.GetInt32(r.GetOrdinal("Id")),
                    FullName = r.GetString(r.GetOrdinal("FullName")),
                    Email = r.GetString(r.GetOrdinal("Email")),
                    Message = r.GetString(r.GetOrdinal("Message")),
                    IsRead = r.GetBoolean(r.GetOrdinal("IsRead")),
                    CreatedAt = r.GetDateTime(r.GetOrdinal("CreatedAt"))
                });

            // Get recent users
            ViewBag.RecentUsers = await _db.QueryAsync(
                "SELECT TOP 5 * FROM Users ORDER BY ISNULL(LastLoginAt, CreatedAt) DESC",
                r => new User
                {
                    Id = r.GetInt32(r.GetOrdinal("Id")),
                    Username = r.GetString(r.GetOrdinal("Username")),
                    Email = r.GetString(r.GetOrdinal("Email")),
                    CreatedAt = r.GetDateTime(r.GetOrdinal("CreatedAt")),
                    LastLoginAt = DbHelper.ReadDateTime(r, "LastLoginAt")
                });

            ViewBag.TotalUsers = await _db.ScalarAsync<int>("SELECT COUNT(*) FROM Users");
            ViewBag.TotalAdmins = await _db.ScalarAsync<int>("SELECT COUNT(*) FROM Admins");

            return View();
        }

        // GET: Admin/Reservations
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Reservations()
        {
            var reservations = await _db.QueryAsync(
                "SELECT * FROM Reservations ORDER BY ReservationDate DESC, ReservationTime",
                 r => new Reservation {
                    Id = r.GetInt32(r.GetOrdinal("Id")),
                    CustomerName = r.GetString(r.GetOrdinal("CustomerName")),
                    PhoneNumber = r.GetString(r.GetOrdinal("PhoneNumber")),
                    Email = DbHelper.ReadStr(r, "Email"),
                    ReservationDate = r.GetDateTime(r.GetOrdinal("ReservationDate")),
                    ReservationTime = DbHelper.ReadTime(r, "ReservationTime"),
                    PartySize = r.GetInt32(r.GetOrdinal("PartySize")),
                    TableNumber = r.GetString(r.GetOrdinal("TableNumber")),
                    ReservationType = r.GetString(r.GetOrdinal("ReservationType")),
                    Status = r.GetString(r.GetOrdinal("Status"))
                 });

            return View(reservations);
        }

        // ===================================
        // AJAX API ENDPOINTS FOR DASHBOARD
        // ===================================

        [HttpGet]
        public async Task<JsonResult> GetOrdersData()
        {
            var orders = await _db.QueryAsync(
                "SELECT TOP 100 Id, CustomerName, DeliveryAddress, Total, Status, OrderDate FROM Orders ORDER BY OrderDate DESC",
                r => new {
                    Id = r.GetInt32(r.GetOrdinal("Id")),
                    CustomerName = DbHelper.ReadStr(r, "CustomerName") ?? "Guest",
                    DeliveryAddress = DbHelper.ReadStr(r, "DeliveryAddress"),
                    Total = DbHelper.ReadDecimal(r, "Total"),
                    Status = r.GetString(r.GetOrdinal("Status")),
                    Date = r.GetDateTime(r.GetOrdinal("OrderDate")).ToString("g")
                });
            return Json(orders);
        }

        [HttpGet]
        public async Task<JsonResult> GetAdminsData()
        {
            var admins = await _db.QueryAsync(
                "SELECT Id, FullName, Email, CreatedAt, LastLoginAt, PlainPassword FROM Admins ORDER BY CreatedAt DESC",
                r => new {
                    Id = r.GetInt32(r.GetOrdinal("Id")),
                    FullName = r.GetString(r.GetOrdinal("FullName")),
                    Email = r.GetString(r.GetOrdinal("Email")),
                    Role = "Admin",
                    Joined = r.GetDateTime(r.GetOrdinal("CreatedAt")).ToShortDateString(),
                    LastLogin = DbHelper.ReadDateTime(r, "LastLoginAt")?.ToString("g") ?? "Never",
                    Password = DbHelper.ReadStr(r, "PlainPassword")
                });
            return Json(admins);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<JsonResult> AddAdmin([FromBody] Admin admin)
        {
            try
            {
                var exists = await _db.ScalarAsync<int>(
                    "SELECT COUNT(*) FROM Admins WHERE Username = @u OR Email = @e",
                    new[] { new SqlParameter("@u", admin.Username), new SqlParameter("@e", admin.Email) });

                if (exists > 0)
                {
                    return Json(new { success = false, message = "Username or Email already exists" });
                }

                if (string.IsNullOrEmpty(admin.PlainPassword))
                {
                    return Json(new { success = false, message = "Password is required" });
                }

                admin.PasswordHash = BCrypt.Net.BCrypt.HashPassword(admin.PlainPassword);
                admin.CreatedAt = DateTime.Now;
                admin.IsActive = true;
                if(string.IsNullOrEmpty(admin.FullName)) admin.FullName = admin.Username;

                await _db.ExecuteAsync(
                    @"INSERT INTO Admins (Username, Email, PasswordHash, PlainPassword, FullName, IsActive, CreatedAt)
                      VALUES (@u, @e, @ph, @pp, @fn, 1, @ca)",
                    new[] {
                        new SqlParameter("@u", admin.Username),
                        new SqlParameter("@e", admin.Email),
                        new SqlParameter("@ph", admin.PasswordHash),
                        new SqlParameter("@pp", admin.PlainPassword),
                        new SqlParameter("@fn", admin.FullName),
                        new SqlParameter("@ca", admin.CreatedAt)
                    });

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetReservationsData()
        {
            var reservations = await _db.QueryAsync(
                "SELECT TOP 100 Id, CustomerName, ReservationDate, ReservationTime, PartySize, Status, ReservationType FROM Reservations ORDER BY ReservationDate DESC, ReservationTime",
                r => new {
                    Id = r.GetInt32(r.GetOrdinal("Id")),
                    CustomerName = r.GetString(r.GetOrdinal("CustomerName")),
                    Date = r.GetDateTime(r.GetOrdinal("ReservationDate")).ToShortDateString(),
                    Time = DateTime.Today.Add(DbHelper.ReadTime(r, "ReservationTime")).ToString("hh:mm tt"),
                    PartySize = r.GetInt32(r.GetOrdinal("PartySize")),
                    Status = r.GetString(r.GetOrdinal("Status")),
                    ReservationType = r.GetString(r.GetOrdinal("ReservationType"))
                });
            return Json(reservations);
        }

        [HttpGet]
        public async Task<JsonResult> GetUsersData()
        {
            var users = await _db.QueryAsync(
                "SELECT Id, Username, Email, Password, CreatedAt, LastLoginAt FROM Users ORDER BY CreatedAt DESC",
                r => new {
                    Id = r.GetInt32(r.GetOrdinal("Id")),
                    FullName = r.GetString(r.GetOrdinal("Username")), 
                    Email = r.GetString(r.GetOrdinal("Email")),
                    Password = r.GetString(r.GetOrdinal("Password")),
                    Joined = r.GetDateTime(r.GetOrdinal("CreatedAt")).ToShortDateString(),
                    LastLogin = DbHelper.ReadDateTime(r, "LastLoginAt")?.ToString("g") ?? "Never"
                });
            return Json(users);
        }

        [HttpPost]
        public async Task<JsonResult> UpdateOrderStatus(int id, string status)
        {
             try
             {
                 var rows = await _db.ExecuteAsync(
                     "UPDATE Orders SET Status = @s WHERE Id = @id",
                     new[] { new SqlParameter("@s", status), new SqlParameter("@id", id) });
                 
                 if (rows == 0) return Json(new { success = false, message = "Order not found" });
                 return Json(new { success = true });
             }
             catch(Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        [HttpPost]
        public async Task<JsonResult> UpdateReservationStatus(int id, string status)
        {
             try
             {
                 var rows = await _db.ExecuteAsync(
                     "UPDATE Reservations SET Status = @s WHERE Id = @id",
                     new[] { new SqlParameter("@s", status), new SqlParameter("@id", id) });

                  if (rows == 0) return Json(new { success = false, message = "Reservation not found" });
                  return Json(new { success = true });
             }
             catch(Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        [HttpPost]
        public async Task<JsonResult> ResetUserPassword(int id)
        {
            try
            {
                string defaultPass = "123456";
                string hash = BCrypt.Net.BCrypt.HashPassword(defaultPass);

                var rows = await _db.ExecuteAsync(
                    "UPDATE Users SET Password = @p, PasswordHash = @ph WHERE Id = @id",
                    new[] { new SqlParameter("@p", defaultPass), new SqlParameter("@ph", hash), new SqlParameter("@id", id) });

                if (rows == 0) return Json(new { success = false, message = "User not found" });
                return Json(new { success = true, newPassword = defaultPass });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<JsonResult> ClearAllOrders()
        {
            try 
            {
                var today = DateTime.Today;
                var totalOrders = await _db.ScalarAsync<int>("SELECT COUNT(*) FROM Orders");
                var totalRev = await _db.ScalarAsync<decimal>("SELECT ISNULL(SUM(Total), 0) FROM Orders WHERE Status != 'Cancelled'");

                if (totalOrders > 0)
                {
                     var exists = await _db.ScalarAsync<int>("SELECT COUNT(*) FROM DailyStats WHERE CAST(Date AS DATE) = @d",
                        new[] { new SqlParameter("@d", today) });

                     if (exists == 0) {
                         await _db.ExecuteAsync(
                            "INSERT INTO DailyStats (Date, TotalOrders, TotalRevenue, TotalReservations) VALUES (@d, @o, @r, 0)",
                            new[] { new SqlParameter("@d", DateTime.Now), new SqlParameter("@o", totalOrders), new SqlParameter("@r", totalRev) });
                     } else {
                         await _db.ExecuteAsync(
                            "UPDATE DailyStats SET TotalOrders = TotalOrders + @o, TotalRevenue = TotalRevenue + @r, Date = @d WHERE CAST(Date AS DATE) = @dt",
                             new[] { new SqlParameter("@o", totalOrders), new SqlParameter("@r", totalRev), new SqlParameter("@d", DateTime.Now), new SqlParameter("@dt", today) });
                     }
                }

                await _db.ExecuteAsync("DELETE FROM OrderItems");
                await _db.ExecuteAsync("DELETE FROM Orders");
                
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<JsonResult> ClearAllReservations()
        {
            try
            {
                var today = DateTime.Today;
                var totalRes = await _db.ScalarAsync<int>("SELECT COUNT(*) FROM Reservations");
                var totalRev = await _db.ScalarAsync<decimal>("SELECT ISNULL(SUM(EstimatedCost), 0) FROM Reservations WHERE Status != 'Cancelled'");

                if(totalRes > 0) {
                     var exists = await _db.ScalarAsync<int>("SELECT COUNT(*) FROM DailyStats WHERE CAST(Date AS DATE) = @d",
                        new[] { new SqlParameter("@d", today) });

                     if (exists == 0) {
                         await _db.ExecuteAsync(
                            "INSERT INTO DailyStats (Date, TotalOrders, TotalRevenue, TotalReservations) VALUES (@d, 0, @r, @res)",
                            new[] { new SqlParameter("@d", DateTime.Now), new SqlParameter("@res", totalRes), new SqlParameter("@r", totalRev) });
                     } else {
                         await _db.ExecuteAsync(
                            "UPDATE DailyStats SET TotalReservations = TotalReservations + @res, TotalRevenue = TotalRevenue + @r, Date = @d WHERE CAST(Date AS DATE) = @dt",
                             new[] { new SqlParameter("@res", totalRes), new SqlParameter("@r", totalRev), new SqlParameter("@d", DateTime.Now), new SqlParameter("@dt", today) });
                     }
                }
                
                await _db.ExecuteAsync("DELETE FROM Reservations");
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<JsonResult> ClearAllHistory()
        {
            try {
                await _db.ExecuteAsync("DELETE FROM DailyStats");
                return Json(new { success = true });
            } catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        [HttpGet]
        public async Task<JsonResult> GetHistoryData()
        {
            var stats = await _db.QueryAsync(
                "SELECT * FROM DailyStats ORDER BY Date DESC",
                r => new {
                    Id = r.GetInt32(r.GetOrdinal("Id")),
                    Date = r.GetDateTime(r.GetOrdinal("Date")).ToString("g"),
                    TotalOrders = r.GetInt32(r.GetOrdinal("TotalOrders")),
                    TotalRevenue = DbHelper.ReadDecimal(r, "TotalRevenue"),
                    TotalReservations = r.GetInt32(r.GetOrdinal("TotalReservations"))
                });
            return Json(stats);
        }

        // POST: Admin/AddMenuItem
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<JsonResult> AddMenuItem([FromBody] MenuItem menuItem)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    int id = await _db.InsertAndGetIdAsync(
                        @"INSERT INTO MenuItems (Name, Description, Price, Category, ImageUrl, Rating, ReviewCount, IsAvailable, CreatedAt)
                          OUTPUT INSERTED.Id
                          VALUES (@n, @d, @p, @c, @iu, @r, @rc, 1, @ca)",
                          new[] {
                              new SqlParameter("@n", menuItem.Name),
                              new SqlParameter("@d", menuItem.Description),
                              new SqlParameter("@p", menuItem.Price),
                              new SqlParameter("@c", menuItem.Category),
                              new SqlParameter("@iu", menuItem.ImageUrl ?? (object)DBNull.Value),
                              new SqlParameter("@r", menuItem.Rating),
                              new SqlParameter("@rc", menuItem.ReviewCount),
                              new SqlParameter("@ca", DateTime.Now)
                          });
                    menuItem.Id = id;
                    menuItem.IsAvailable = true;
                    menuItem.CreatedAt = DateTime.Now;
                    return Json(new { success = true, item = menuItem });
                }
                return Json(new { success = false, message = "Invalid data" });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        // POST: Admin/UpdateMenuItem
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<JsonResult> UpdateMenuItem([FromBody] MenuItem menuItem)
        {
            try
            {
                 var rows = await _db.ExecuteAsync(
                     @"UPDATE MenuItems 
                       SET Name=@n, Description=@d, Price=@p, Category=@c, ImageUrl=@iu, 
                           Rating=@r, ReviewCount=@rc, IsAvailable=@ia, UpdatedAt=@ua
                       WHERE Id=@id",
                       new[] {
                            new SqlParameter("@id", menuItem.Id),
                            new SqlParameter("@n", menuItem.Name),
                            new SqlParameter("@d", menuItem.Description),
                            new SqlParameter("@p", menuItem.Price),
                            new SqlParameter("@c", menuItem.Category),
                            new SqlParameter("@iu", menuItem.ImageUrl ?? (object)DBNull.Value),
                            new SqlParameter("@r", menuItem.Rating),
                            new SqlParameter("@rc", menuItem.ReviewCount),
                            new SqlParameter("@ia", menuItem.IsAvailable),
                            new SqlParameter("@ua", DateTime.Now)
                       });

                 if(rows == 0) return Json(new { success = false, message = "Menu item not found" });
                 return Json(new { success = true, item = menuItem }); // Could fetch from DB to return exactly what's there
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: Admin/DeleteMenuItem
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<JsonResult> DeleteMenuItem(int id)
        {
            try
            {
                var rows = await _db.ExecuteAsync("DELETE FROM MenuItems WHERE Id = @id", new[] { new SqlParameter("@id", id) });
                if(rows == 0) return Json(new { success = false, message = "Menu item not found" });
                return Json(new { success = true });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        // GET: Admin/GetOrders
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<JsonResult> GetOrders(string? status)
        {
            string sql = @"
                SELECT TOP 50 o.*, oi.Id AS OI_Id, oi.MenuItemId, oi.Quantity, oi.Price AS OI_Price, oi.Customization 
                FROM Orders o 
                LEFT JOIN OrderItems oi ON o.Id = oi.OrderId 
                WHERE 1=1 ";
            
            var parameters = new List<SqlParameter>();
            if(!string.IsNullOrEmpty(status)) {
                sql += " AND o.Status = @s ";
                parameters.Add(new SqlParameter("@s", status));
            }
            sql += " ORDER BY o.OrderDate DESC";

            var rows = await _db.QueryAsync(sql, r => new {
                OrderId = r.GetInt32(r.GetOrdinal("Id")),
                OrderNumber = r.GetString(r.GetOrdinal("OrderNumber")),
                Total = DbHelper.ReadDecimal(r, "Total"),
                Status = r.GetString(r.GetOrdinal("Status")),
                OrderDate = r.GetDateTime(r.GetOrdinal("OrderDate")),
                CustomerName = DbHelper.ReadStr(r, "CustomerName"),
                OI_Id = DbHelper.ReadIntN(r, "OI_Id"),
                MenuItemId = DbHelper.ReadIntN(r, "MenuItemId"),
                Quantity = DbHelper.ReadIntN(r, "Quantity"),
                OI_Price = DbHelper.ReadDecimal(r, "OI_Price"),
                Customization = DbHelper.ReadStr(r, "Customization")
            }, parameters.ToArray());

            var grouped = rows.GroupBy(x => x.OrderId).Select(g => {
                var first = g.First();
                return new {
                    Id = first.OrderId,
                    OrderNumber = first.OrderNumber,
                    Total = first.Total,
                    Status = first.Status,
                    OrderDate = first.OrderDate,
                    CustomerName = first.CustomerName,
                    OrderItems = g.Where(x => x.OI_Id.HasValue).Select(x => new {
                         Id = x.OI_Id,
                         MenuItemId = x.MenuItemId,
                         Quantity = x.Quantity,
                         Price = x.OI_Price,
                         Customization = x.Customization
                    }).ToList()
                };
            }).ToList();

            return Json(grouped);
        }

        // GET: Admin/GetMessages
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<JsonResult> GetMessages(bool? unreadOnly)
        {
            string sql = "SELECT TOP 50 * FROM ContactMessages WHERE 1=1 ";
            if (unreadOnly == true) sql += " AND IsRead = 0 ";
            sql += " ORDER BY CreatedAt DESC";

            var msgs = await _db.QueryAsync(sql, r => new {
                Id = r.GetInt32(r.GetOrdinal("Id")),
                FullName = r.GetString(r.GetOrdinal("FullName")),
                Email = r.GetString(r.GetOrdinal("Email")),
                PhoneNumber = r.GetString(r.GetOrdinal("PhoneNumber")),
                Message = r.GetString(r.GetOrdinal("Message")),
                IsRead = r.GetBoolean(r.GetOrdinal("IsRead")),
                CreatedAt = r.GetDateTime(r.GetOrdinal("CreatedAt"))
            });
            return Json(msgs);
        }

        // POST: Admin/MarkMessageRead
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<JsonResult> MarkMessageRead(int id)
        {
            try {
                var rows = await _db.ExecuteAsync("UPDATE ContactMessages SET IsRead = 1 WHERE Id = @id", new[] { new SqlParameter("@id", id) });
                if(rows == 0) return Json(new { success = false, message = "Message not found" });
                return Json(new { success = true });
            } catch(Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        // POST: Admin/CleanReservation
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<JsonResult> CleanReservation(int id)
        {
            try {
                var rows = await _db.ExecuteAsync("DELETE FROM Reservations WHERE Id = @id", new[] { new SqlParameter("@id", id) });
                if(rows == 0) return Json(new { success = false, message = "Reservation not found" });
                return Json(new { success = true });
            } catch(Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<JsonResult> ClearAllMessages()
        {
             try {
                await _db.ExecuteAsync("DELETE FROM ContactMessages");
                return Json(new { success = true });
            } catch(Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }
    }
}
