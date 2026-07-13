using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using FoodHeaven.Data;
using FoodHeaven.Models;
using Microsoft.Data.SqlClient;

namespace FoodHeaven.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {
        private readonly DbHelper _db;

        public OrderController(DbHelper db)
        {
            _db = db;
        }

        // GET: Order/Cart
        public IActionResult Cart() => View();

        // GET: Order/Checkout
        public IActionResult Checkout() => View();

        // POST: Order/Create
        [HttpPost]
        public async Task<JsonResult> Create([FromBody] Order order)
        {
            try
            {
                // Validate menu items exist
                var menuItemIds = order.OrderItems.Select(oi => oi.MenuItemId).Distinct().ToList();
                var inClause   = string.Join(",", menuItemIds);
                var existing   = await _db.QueryAsync(
                    $"SELECT Id FROM MenuItems WHERE Id IN ({inClause})",
                    r => r.GetInt32(0));

                var invalidIds = menuItemIds.Except(existing).ToList();
                if (invalidIds.Any())
                    return Json(new { success = false, message = $"Invalid menu items: {string.Join(", ", invalidIds)}. Please refresh and try again." });

                // Fill order metadata
                order.OrderNumber = $"ORD-{DateTime.Now:yyyyMMdd}-{new Random().Next(1000, 9999)}";
                order.OrderDate   = DateTime.Now;
                order.Status      = "Pending";

                if (User.Identity?.IsAuthenticated == true)
                {
                    var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
                    if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
                        order.UserId = userId;

                    if (string.IsNullOrEmpty(order.CustomerName))
                        order.CustomerName = User.Identity.Name;
                    if (string.IsNullOrEmpty(order.CustomerEmail))
                        order.CustomerEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
                }

                // Insert Order, get new Id
                int orderId = await _db.InsertAndGetIdAsync(
                    @"INSERT INTO Orders
                        (OrderNumber, CustomerName, CustomerEmail, CustomerPhone,
                         Subtotal, DeliveryFee, Tax, Total, Status, PromoCode,
                         Discount, SpecialInstructions, OrderDate, DeliveryAddress,
                         PaymentMethod, UserId)
                      OUTPUT INSERTED.Id
                      VALUES
                        (@on, @cn, @ce, @cp, @sub, @df, @tax, @tot, @st, @pc,
                         @disc, @si, @od, @da, @pm, @uid)",
                    new[]
                    {
                        new SqlParameter("@on",   order.OrderNumber),
                        new SqlParameter("@cn",   order.CustomerName      ?? (object)DBNull.Value),
                        new SqlParameter("@ce",   order.CustomerEmail     ?? (object)DBNull.Value),
                        new SqlParameter("@cp",   order.CustomerPhone     ?? (object)DBNull.Value),
                        new SqlParameter("@sub",  order.Subtotal),
                        new SqlParameter("@df",   order.DeliveryFee),
                        new SqlParameter("@tax",  order.Tax),
                        new SqlParameter("@tot",  order.Total),
                        new SqlParameter("@st",   order.Status),
                        new SqlParameter("@pc",   order.PromoCode         ?? (object)DBNull.Value),
                        new SqlParameter("@disc", order.Discount),
                        new SqlParameter("@si",   order.SpecialInstructions ?? (object)DBNull.Value),
                        new SqlParameter("@od",   order.OrderDate),
                        new SqlParameter("@da",   order.DeliveryAddress   ?? (object)DBNull.Value),
                        new SqlParameter("@pm",   order.PaymentMethod     ?? (object)DBNull.Value),
                        new SqlParameter("@uid",  order.UserId.HasValue ? (object)order.UserId.Value : DBNull.Value)
                    });

                // Insert OrderItems
                foreach (var item in order.OrderItems)
                {
                    await _db.ExecuteAsync(
                        @"INSERT INTO OrderItems (OrderId, MenuItemId, Quantity, Price, Customization)
                          VALUES (@oid, @mid, @qty, @pr, @cust)",
                        new[]
                        {
                            new SqlParameter("@oid",  orderId),
                            new SqlParameter("@mid",  item.MenuItemId),
                            new SqlParameter("@qty",  item.Quantity),
                            new SqlParameter("@pr",   item.Price),
                            new SqlParameter("@cust", item.Customization ?? (object)DBNull.Value)
                        });
                }

                return Json(new { success = true, orderNumber = order.OrderNumber, orderId });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message + (ex.InnerException != null ? " | " + ex.InnerException.Message : "") });
            }
        }

        // GET: Order/MyOrders
        public async Task<IActionResult> MyOrders()
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                return View(new List<Order>());

            var orders = await _db.QueryAsync(
                @"SELECT o.*, oi.Id AS OI_Id, oi.MenuItemId, oi.Quantity, oi.Price AS OI_Price,
                         oi.Customization, m.Name AS MenuItemName, m.ImageUrl
                  FROM Orders o
                  LEFT JOIN OrderItems oi ON oi.OrderId = o.Id
                  LEFT JOIN MenuItems  m  ON m.Id = oi.MenuItemId
                  WHERE o.UserId = @uid
                  ORDER BY o.OrderDate DESC",
                r =>
                {
                    // We build a flat record; grouping is done below
                    return new
                    {
                        OrderId          = r.GetInt32(r.GetOrdinal("Id")),
                        OrderNumber      = r.GetString(r.GetOrdinal("OrderNumber")),
                        CustomerName     = DbHelper.ReadStr(r, "CustomerName"),
                        Total            = DbHelper.ReadDecimal(r, "Total"),
                        Status           = r.GetString(r.GetOrdinal("Status")),
                        OrderDate        = r.GetDateTime(r.GetOrdinal("OrderDate")),
                        OI_Id            = DbHelper.ReadIntN(r, "OI_Id"),
                        MenuItemId       = DbHelper.ReadIntN(r, "MenuItemId"),
                        Qty              = DbHelper.ReadIntN(r, "Quantity"),
                        OI_Price         = DbHelper.ReadDecimal(r, "OI_Price"),
                        Customization    = DbHelper.ReadStr(r, "Customization"),
                        MenuItemName     = DbHelper.ReadStr(r, "MenuItemName"),
                        ImageUrl         = DbHelper.ReadStr(r, "ImageUrl")
                    };
                },
                new[] { new SqlParameter("@uid", userId) });

            // Group flat rows into Order objects
            var grouped = orders
                .GroupBy(o => o.OrderId)
                .Select(g =>
                {
                    var first = g.First();
                    var ord = new Order
                    {
                        Id          = first.OrderId,
                        OrderNumber = first.OrderNumber,
                        CustomerName = first.CustomerName,
                        Total       = first.Total,
                        Status      = first.Status,
                        OrderDate   = first.OrderDate,
                        OrderItems  = g.Where(x => x.OI_Id.HasValue).Select(x => new OrderItem
                        {
                            Id         = x.OI_Id!.Value,
                            MenuItemId = x.MenuItemId!.Value,
                            Quantity   = x.Qty!.Value,
                            Price      = x.OI_Price,
                            Customization = x.Customization,
                            MenuItem   = new MenuItem { Id = x.MenuItemId!.Value, Name = x.MenuItemName ?? "", ImageUrl = x.ImageUrl }
                        }).ToList()
                    };
                    return ord;
                }).ToList();

            // Additional logic to also retrieve reservations for the profile page
            var userEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? "";
            var username = User.Identity?.Name ?? "";
            var reservations = await _db.QueryAsync(
                @"SELECT Id, CustomerName, ReservationDate, ReservationTime, PartySize, TableNumber, Status, ReservationType 
                  FROM Reservations 
                  WHERE Email = @e OR CustomerName = @n 
                  ORDER BY ReservationDate DESC",
                r => new Reservation {
                    Id = r.GetInt32(r.GetOrdinal("Id")),
                    CustomerName = DbHelper.ReadStr(r, "CustomerName") ?? "",
                    ReservationDate = r.GetDateTime(r.GetOrdinal("ReservationDate")),
                    ReservationTime = DbHelper.ReadTime(r, "ReservationTime"),
                    PartySize = r.GetInt32(r.GetOrdinal("PartySize")),
                    TableNumber = r.GetString(r.GetOrdinal("TableNumber")),
                    Status = r.GetString(r.GetOrdinal("Status")),
                    ReservationType = r.GetString(r.GetOrdinal("ReservationType"))
                },
                new[] { new SqlParameter("@e", userEmail), new SqlParameter("@n", username) }
            );

            ViewBag.UserId    = userId;
            ViewBag.Username  = username;
            ViewBag.OrderCount = grouped.Count;
            ViewBag.Reservations = reservations;
            return View(grouped);
        }

        // GET: Order/Confirmation/{id}
        public async Task<IActionResult> Confirmation(int? id)
        {
            if (id == null) return NotFound();

            var rows = await _db.QueryAsync(
                @"SELECT o.*, oi.Id AS OI_Id, oi.MenuItemId, oi.Quantity, oi.Price AS OI_Price,
                         oi.Customization, m.Name AS MenuItemName
                  FROM Orders o
                  LEFT JOIN OrderItems oi ON oi.OrderId = o.Id
                  LEFT JOIN MenuItems  m  ON m.Id = oi.MenuItemId
                  WHERE o.Id = @id",
                r => new
                {
                    OrderId       = r.GetInt32(r.GetOrdinal("Id")),
                    OrderNumber   = r.GetString(r.GetOrdinal("OrderNumber")),
                    CustomerName  = DbHelper.ReadStr(r, "CustomerName"),
                    CustomerEmail = DbHelper.ReadStr(r, "CustomerEmail"),
                    Total         = DbHelper.ReadDecimal(r, "Total"),
                    Subtotal      = DbHelper.ReadDecimal(r, "Subtotal"),
                    DeliveryFee   = DbHelper.ReadDecimal(r, "DeliveryFee"),
                    Tax           = DbHelper.ReadDecimal(r, "Tax"),
                    Status        = r.GetString(r.GetOrdinal("Status")),
                    OrderDate     = r.GetDateTime(r.GetOrdinal("OrderDate")),
                    OI_Id         = DbHelper.ReadIntN(r, "OI_Id"),
                    MenuItemId    = DbHelper.ReadIntN(r, "MenuItemId"),
                    Qty           = DbHelper.ReadIntN(r, "Quantity"),
                    OI_Price      = DbHelper.ReadDecimal(r, "OI_Price"),
                    Customization = DbHelper.ReadStr(r, "Customization"),
                    MenuItemName  = DbHelper.ReadStr(r, "MenuItemName")
                },
                new[] { new SqlParameter("@id", id.Value) });

            if (!rows.Any()) return NotFound();

            var firstRow = rows.First();
            var order = new Order
            {
                Id           = firstRow.OrderId,
                OrderNumber  = firstRow.OrderNumber,
                CustomerName = firstRow.CustomerName,
                CustomerEmail = firstRow.CustomerEmail,
                Subtotal     = firstRow.Subtotal,
                DeliveryFee  = firstRow.DeliveryFee,
                Tax          = firstRow.Tax,
                Total        = firstRow.Total,
                Status       = firstRow.Status,
                OrderDate    = firstRow.OrderDate,
                OrderItems   = rows.Where(x => x.OI_Id.HasValue).Select(x => new OrderItem
                {
                    Id         = x.OI_Id!.Value,
                    MenuItemId = x.MenuItemId!.Value,
                    Quantity   = x.Qty!.Value,
                    Price      = x.OI_Price,
                    Customization = x.Customization,
                    MenuItem   = new MenuItem { Id = x.MenuItemId!.Value, Name = x.MenuItemName ?? "" }
                }).ToList()
            };

            return View(order);
        }

        // POST: Order/UpdateStatus
        [HttpPost]
        public async Task<JsonResult> UpdateStatus(int orderId, string status)
        {
            try
            {
                var completedDate = (status == "Delivered" || status == "Completed")
                    ? (object)DateTime.Now : DBNull.Value;

                var rows = await _db.ExecuteAsync(
                    "UPDATE Orders SET Status = @s, CompletedDate = @cd WHERE Id = @id",
                    new[]
                    {
                        new SqlParameter("@s",  status),
                        new SqlParameter("@cd", completedDate),
                        new SqlParameter("@id", orderId)
                    });

                if (rows == 0) return Json(new { success = false, message = "Order not found" });
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: Order/ApplyPromoCode
        [HttpGet]
        public JsonResult ApplyPromoCode(string promoCode, decimal subtotal)
        {
            var validCodes = new Dictionary<string, decimal>
            {
                { "SAVE10",  0.10m },
                { "SAVE20",  0.20m },
                { "FIRST15", 0.15m }
            };

            if (validCodes.TryGetValue(promoCode.ToUpper(), out decimal pct))
            {
                var discount = subtotal * pct;
                return Json(new { success = true, discount, message = "Promo code applied!" });
            }
            return Json(new { success = false, message = "Invalid promo code" });
        }
    }
}
