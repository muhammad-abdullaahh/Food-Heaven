using Microsoft.AspNetCore.Mvc;
using FoodHeaven.Data;
using FoodHeaven.Models;
using Microsoft.Data.SqlClient;

namespace FoodHeaven.Controllers
{
    public class LoyaltyController : Controller
    {
        private readonly DbHelper _db;

        public LoyaltyController(DbHelper db)
        {
            _db = db;
        }

        // GET: Loyalty/Index/{customerId}
        public async Task<IActionResult> Index(string? customerId)
        {
            customerId ??= "CUST-001";

            var accounts = await _db.QueryAsync(
                "SELECT * FROM LoyaltyAccounts WHERE CustomerId = @cid",
                r => new LoyaltyAccount
                {
                    Id               = r.GetInt32(r.GetOrdinal("Id")),
                    CustomerId       = r.GetString(r.GetOrdinal("CustomerId")),
                    CustomerName     = r.GetString(r.GetOrdinal("CustomerName")),
                    Email            = DbHelper.ReadStr(r, "Email"),
                    PhoneNumber      = DbHelper.ReadStr(r, "PhoneNumber"),
                    Points           = r.GetInt32(r.GetOrdinal("Points")),
                    MembershipTier   = r.GetString(r.GetOrdinal("MembershipTier")),
                    LunchPunchCount  = r.GetInt32(r.GetOrdinal("LunchPunchCount")),
                    CreatedAt        = r.GetDateTime(r.GetOrdinal("CreatedAt")),
                    LastActivityDate = DbHelper.ReadDateTime(r, "LastActivityDate")
                },
                new[] { new SqlParameter("@cid", customerId) });

            LoyaltyAccount? loyaltyAccount = accounts.FirstOrDefault();

            if (loyaltyAccount == null)
            {
                // Create demo account
                int newId = await _db.InsertAndGetIdAsync(
                    @"INSERT INTO LoyaltyAccounts (CustomerId, CustomerName, Points, MembershipTier, LunchPunchCount, CreatedAt, LastActivityDate)
                      OUTPUT INSERTED.Id
                      VALUES (@cid, @cn, @pts, @tier, @lpc, @ca, @la)",
                    new[]
                    {
                        new SqlParameter("@cid",  customerId),
                        new SqlParameter("@cn",   "Demo Customer"),
                        new SqlParameter("@pts",  1250),
                        new SqlParameter("@tier", "Gold"),
                        new SqlParameter("@lpc",  6),
                        new SqlParameter("@ca",   DateTime.Now),
                        new SqlParameter("@la",   DateTime.Now)
                    });

                loyaltyAccount = new LoyaltyAccount
                {
                    Id             = newId,
                    CustomerId     = customerId,
                    CustomerName   = "Demo Customer",
                    Points         = 1250,
                    MembershipTier = "Gold",
                    LunchPunchCount = 6,
                    CreatedAt      = DateTime.Now,
                    LastActivityDate = DateTime.Now
                };
            }

            // Load recent transactions
            loyaltyAccount.Transactions = await _db.QueryAsync(
                @"SELECT TOP 10 * FROM LoyaltyTransactions
                  WHERE LoyaltyAccountId = @aid
                  ORDER BY TransactionDate DESC",
                r => new LoyaltyTransaction
                {
                    Id               = r.GetInt32(r.GetOrdinal("Id")),
                    LoyaltyAccountId = r.GetInt32(r.GetOrdinal("LoyaltyAccountId")),
                    TransactionType  = r.GetString(r.GetOrdinal("TransactionType")),
                    Points           = r.GetInt32(r.GetOrdinal("Points")),
                    Description      = DbHelper.ReadStr(r, "Description"),
                    OrderId          = DbHelper.ReadIntN(r, "OrderId"),
                    TransactionDate  = r.GetDateTime(r.GetOrdinal("TransactionDate"))
                },
                new[] { new SqlParameter("@aid", loyaltyAccount.Id) });

            return View(loyaltyAccount);
        }

        // POST: Loyalty/RedeemReward
        [HttpPost]
        public async Task<JsonResult> RedeemReward(string customerId, int points, string rewardName)
        {
            try
            {
                var accounts = await _db.QueryAsync(
                    "SELECT Id, Points FROM LoyaltyAccounts WHERE CustomerId = @cid",
                    r => new { Id = r.GetInt32(0), Points = r.GetInt32(1) },
                    new[] { new SqlParameter("@cid", customerId) });

                var account = accounts.FirstOrDefault();
                if (account == null) return Json(new { success = false, message = "Account not found" });
                if (account.Points < points) return Json(new { success = false, message = "Insufficient points" });

                int remaining = account.Points - points;
                await _db.ExecuteAsync(
                    "UPDATE LoyaltyAccounts SET Points = @pts, LastActivityDate = @la WHERE Id = @id",
                    new[] { new SqlParameter("@pts", remaining), new SqlParameter("@la", DateTime.Now), new SqlParameter("@id", account.Id) });

                await _db.ExecuteAsync(
                    @"INSERT INTO LoyaltyTransactions (LoyaltyAccountId, TransactionType, Points, Description, TransactionDate)
                      VALUES (@aid, 'Redeem', @neg, @desc, @td)",
                    new[]
                    {
                        new SqlParameter("@aid",  account.Id),
                        new SqlParameter("@neg",  -points),
                        new SqlParameter("@desc", $"Redeemed: {rewardName}"),
                        new SqlParameter("@td",   DateTime.Now)
                    });

                return Json(new { success = true, remainingPoints = remaining, message = $"Successfully redeemed {rewardName}!" });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        // POST: Loyalty/AddPoints
        [HttpPost]
        public async Task<JsonResult> AddPoints(string customerId, int points, int? orderId)
        {
            try
            {
                var accounts = await _db.QueryAsync(
                    "SELECT Id, Points FROM LoyaltyAccounts WHERE CustomerId = @cid",
                    r => new { Id = r.GetInt32(0), Points = r.GetInt32(1) },
                    new[] { new SqlParameter("@cid", customerId) });

                var account = accounts.FirstOrDefault();
                if (account == null) return Json(new { success = false, message = "Account not found" });

                int newPoints = account.Points + points;
                string tier   = newPoints >= 2000 ? "Platinum" : newPoints >= 1000 ? "Gold" : newPoints >= 500 ? "Silver" : "Bronze";

                await _db.ExecuteAsync(
                    "UPDATE LoyaltyAccounts SET Points = @pts, MembershipTier = @tier, LastActivityDate = @la WHERE Id = @id",
                    new[]
                    {
                        new SqlParameter("@pts",  newPoints),
                        new SqlParameter("@tier", tier),
                        new SqlParameter("@la",   DateTime.Now),
                        new SqlParameter("@id",   account.Id)
                    });

                await _db.ExecuteAsync(
                    @"INSERT INTO LoyaltyTransactions (LoyaltyAccountId, TransactionType, Points, Description, OrderId, TransactionDate)
                      VALUES (@aid, 'Earn', @pts, @desc, @oid, @td)",
                    new[]
                    {
                        new SqlParameter("@aid",  account.Id),
                        new SqlParameter("@pts",  points),
                        new SqlParameter("@desc", orderId.HasValue ? $"Points earned from Order #{orderId}" : "Points earned"),
                        new SqlParameter("@oid",  orderId.HasValue ? (object)orderId.Value : DBNull.Value),
                        new SqlParameter("@td",   DateTime.Now)
                    });

                return Json(new { success = true, totalPoints = newPoints, tier });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        // GET: Loyalty/GetHistory
        [HttpGet]
        public async Task<JsonResult> GetHistory(string customerId, int take = 20)
        {
            var accounts = await _db.QueryAsync(
                "SELECT Id FROM LoyaltyAccounts WHERE CustomerId = @cid",
                r => r.GetInt32(0),
                new[] { new SqlParameter("@cid", customerId) });

            if (!accounts.Any()) return Json(new { success = false, message = "Account not found" });

            var transactions = await _db.QueryAsync(
                $"SELECT TOP {take} * FROM LoyaltyTransactions WHERE LoyaltyAccountId = @aid ORDER BY TransactionDate DESC",
                r => new
                {
                    Id              = r.GetInt32(r.GetOrdinal("Id")),
                    TransactionType = r.GetString(r.GetOrdinal("TransactionType")),
                    Points          = r.GetInt32(r.GetOrdinal("Points")),
                    Description     = DbHelper.ReadStr(r, "Description"),
                    TransactionDate = r.GetDateTime(r.GetOrdinal("TransactionDate")).ToString("g")
                },
                new[] { new SqlParameter("@aid", accounts.First()) });

            return Json(new { success = true, transactions });
        }
    }
}
