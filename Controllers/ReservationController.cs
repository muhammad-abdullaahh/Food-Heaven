using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using FoodHeaven.Data;
using FoodHeaven.Models;
using Microsoft.Data.SqlClient;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;
using FoodHeaven.Services;

namespace FoodHeaven.Controllers
{
    [Authorize]
    public class ReservationController : Controller
    {
        private readonly DbHelper _db;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;

        public ReservationController(DbHelper db, IConfiguration configuration, IEmailService emailService)
        {
            _db            = db;
            _configuration = configuration;
            _emailService  = emailService;
        }

        // GET: Reservation/Create
        public IActionResult Create() => View();

        // POST: Reservation/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Reservation reservation)
        {
            if (ModelState.IsValid)
            {
                reservation.CreatedAt = DateTime.Now;
                reservation.Status    = "Confirmed";

                if (string.IsNullOrEmpty(reservation.ReservationType))
                    reservation.ReservationType = "Standard";

                if (reservation.ReservationType == "Premium")
                {
                    reservation.IsPremiumTable  = true;
                    reservation.EstimatedCost   = reservation.PartySize * 15.00m;
                }
                else if (reservation.ReservationType == "PrivateParty")
                {
                    reservation.IsPremiumTable = false;
                    reservation.EstimatedCost  = 0;
                }
                else
                {
                    reservation.IsPremiumTable  = false;
                    reservation.EstimatedCost   = 0;
                    reservation.ReservationType = "Standard";
                }

                // Insert and get new Id
                int newId = await _db.InsertAndGetIdAsync(
                    @"INSERT INTO Reservations
                        (CustomerName, PhoneNumber, Email, ReservationDate, ReservationTime,
                         PartySize, TableNumber, ReservationType, SpecialRequests, Status,
                         CreatedAt, IsPremiumTable, EstimatedCost)
                      OUTPUT INSERTED.Id
                      VALUES
                        (@cn, @ph, @em, @rd, @rt, @ps, @tn, @rtype, @sr, @st, @ca, @ipt, @ec)",
                    new[]
                    {
                        new SqlParameter("@cn",    reservation.CustomerName),
                        new SqlParameter("@ph",    reservation.PhoneNumber),
                        new SqlParameter("@em",    reservation.Email       ?? (object)DBNull.Value),
                        new SqlParameter("@rd",    reservation.ReservationDate),
                        new SqlParameter("@rt",    reservation.ReservationTime),
                        new SqlParameter("@ps",    reservation.PartySize),
                        new SqlParameter("@tn",    reservation.TableNumber),
                        new SqlParameter("@rtype", reservation.ReservationType),
                        new SqlParameter("@sr",    reservation.SpecialRequests ?? (object)DBNull.Value),
                        new SqlParameter("@st",    reservation.Status),
                        new SqlParameter("@ca",    reservation.CreatedAt),
                        new SqlParameter("@ipt",   reservation.IsPremiumTable),
                        new SqlParameter("@ec",    reservation.EstimatedCost)
                    });

                reservation.Id = newId;

                // Send SMS (silent failure)
                try { SendSmsConfirmation(reservation.PhoneNumber, reservation.CustomerName, reservation.ReservationDate, reservation.ReservationTime); }
                catch (Exception ex) { Console.WriteLine("SMS Failed: " + ex.Message); }

                // Send Email
                if (!string.IsNullOrEmpty(reservation.Email))
                {
                    try
                    {
                        string displayTableType = reservation.ReservationType switch
                        {
                            "PrivateParty" => "Private Party",
                            "Premium"      => "Premium",
                            _              => "Standard"
                        };
                        string subject = $"Reservation Confirmation - {displayTableType} Table";
                        string body = $@"
                            <div style='font-family:Arial,sans-serif;max-width:600px;margin:0 auto;color:#333;'>
                              <div style='background:#000;padding:20px;text-align:center;border-radius:10px 10px 0 0;'>
                                <h1 style='color:#fff;margin:0;'>FoodHeaven</h1>
                              </div>
                              <div style='padding:20px;border:1px solid #ddd;border-top:none;border-radius:0 0 10px 10px;'>
                                <h2 style='color:#d4a017;'>Reservation Confirmed!</h2>
                                <p>Dear {reservation.CustomerName},</p>
                                <p>We are delighted to confirm your <strong>{displayTableType}</strong> reservation at FoodHeaven.</p>
                                <table style='width:100%;border-collapse:collapse;margin:20px 0;'>
                                  <tr><td style='padding:10px;border-bottom:1px solid #eee;'><strong>📅 Date:</strong></td><td style='padding:10px;border-bottom:1px solid #eee;'>{reservation.ReservationDate.ToShortDateString()}</td></tr>
                                  <tr><td style='padding:10px;border-bottom:1px solid #eee;'><strong>⏰ Time:</strong></td><td style='padding:10px;border-bottom:1px solid #eee;'>{DateTime.Today.Add(reservation.ReservationTime):hh:mm tt}</td></tr>
                                  <tr><td style='padding:10px;border-bottom:1px solid #eee;'><strong>👥 Guests:</strong></td><td style='padding:10px;border-bottom:1px solid #eee;'>{reservation.PartySize}</td></tr>
                                  <tr><td style='padding:10px;border-bottom:1px solid #eee;'><strong>🍽️ Table Type:</strong></td><td style='padding:10px;border-bottom:1px solid #eee;'>{displayTableType}</td></tr>
                                  {(reservation.ReservationType == "Premium" ? $"<tr><td style='padding:10px;'><strong>💰 Estimated Cost:</strong></td><td style='padding:10px;'>${reservation.EstimatedCost}</td></tr>" : "")}
                                </table>
                                <p>We look forward to serving you an unforgettable dining experience.</p>
                                <p>Best Regards,<br/><strong>The FoodHeaven Team</strong></p>
                              </div>
                            </div>";
                        await _emailService.SendEmailAsync(reservation.Email, subject, body);
                    }
                    catch (Exception ex) { Console.WriteLine("Email Failed: " + ex.Message); }
                }

                string typeMsg = reservation.ReservationType == "Premium" ? "Premium" :
                                 (reservation.ReservationType == "PrivateParty" ? "Private Party" : "Standard");
                TempData["SuccessMessage"] = $"{typeMsg} Reservation Confirmed! A confirmation email has been sent to {reservation.Email}.";
                return RedirectToAction(nameof(Confirmation), new { id = reservation.Id });
            }
            return View(reservation);
        }

        // GET: Reservation/Confirmation/{id}
        public async Task<IActionResult> Confirmation(int? id)
        {
            if (id == null) return NotFound();

            var list = await _db.QueryAsync(
                "SELECT * FROM Reservations WHERE Id = @id",
                r => MapReservation(r),
                new[] { new SqlParameter("@id", id.Value) });

            if (!list.Any()) return NotFound();
            return View(list.First());
        }

        // POST: Reservation/Cancel
        [HttpPost]
        public async Task<JsonResult> Cancel(int id)
        {
            try
            {
                var rows = await _db.ExecuteAsync(
                    "UPDATE Reservations SET Status = 'Cancelled', UpdatedAt = @upd WHERE Id = @id",
                    new[] { new SqlParameter("@upd", DateTime.Now), new SqlParameter("@id", id) });

                if (rows == 0) return Json(new { success = false, message = "Reservation not found" });
                return Json(new { success = true, message = "Reservation cancelled successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: Reservation/CheckAvailability
        [HttpGet]
        public async Task<JsonResult> CheckAvailability(DateTime date, string time, string tableNumber)
        {
            var ts = TimeSpan.Parse(time);
            var count = await _db.ScalarAsync<int>(
                @"SELECT COUNT(*) FROM Reservations
                  WHERE CAST(ReservationDate AS DATE) = @d
                    AND ReservationTime = @t
                    AND TableNumber     = @tn
                    AND Status          = 'Confirmed'",
                new[]
                {
                    new SqlParameter("@d",  date.Date),
                    new SqlParameter("@t",  ts),
                    new SqlParameter("@tn", tableNumber)
                });

            return Json(new { available = count == 0 });
        }

        // ── Private helpers ────────────────────────────────────────────
        private static Reservation MapReservation(SqlDataReader r) => new Reservation
        {
            Id              = r.GetInt32(r.GetOrdinal("Id")),
            CustomerName    = r.GetString(r.GetOrdinal("CustomerName")),
            PhoneNumber     = r.GetString(r.GetOrdinal("PhoneNumber")),
            Email           = DbHelper.ReadStr(r, "Email"),
            ReservationDate = r.GetDateTime(r.GetOrdinal("ReservationDate")),
            ReservationTime = DbHelper.ReadTime(r, "ReservationTime"),
            PartySize       = r.GetInt32(r.GetOrdinal("PartySize")),
            TableNumber     = r.GetString(r.GetOrdinal("TableNumber")),
            ReservationType = r.GetString(r.GetOrdinal("ReservationType")),
            SpecialRequests = DbHelper.ReadStr(r, "SpecialRequests"),
            Status          = r.GetString(r.GetOrdinal("Status")),
            CreatedAt       = r.GetDateTime(r.GetOrdinal("CreatedAt")),
            UpdatedAt       = DbHelper.ReadDateTime(r, "UpdatedAt"),
            IsPremiumTable  = r.GetBoolean(r.GetOrdinal("IsPremiumTable")),
            EstimatedCost   = DbHelper.ReadDecimal(r, "EstimatedCost")
        };

        private bool SendSmsConfirmation(string phoneNumber, string name, DateTime date, TimeSpan time)
        {
            var accountSid  = _configuration["Twilio:AccountSid"];
            var authToken   = _configuration["Twilio:AuthToken"];
            var fromNumber  = _configuration["Twilio:PhoneNumber"];

            if (!string.IsNullOrEmpty(accountSid) && !string.IsNullOrEmpty(authToken) &&
                !string.IsNullOrEmpty(fromNumber) && !fromNumber.Contains("YOUR_TWILIO_NUMBER"))
            {
                try
                {
                    TwilioClient.Init(accountSid, authToken);
                    var msg = MessageResource.Create(
                        body: $"Hello {name}, your table at FoodHeaven is reserved for {date:d} at {time}. We look forward to seeing you!",
                        from: new PhoneNumber(fromNumber),
                        to:   new PhoneNumber(phoneNumber));
                    Console.WriteLine($"[SMS SENT] SID: {msg.Sid}");
                    return true;
                }
                catch (Exception ex) { Console.WriteLine($"[SMS FAILURE] {ex.Message}"); return false; }
            }
            Console.WriteLine($"[SMS SIMULATION] To: {phoneNumber} - Hello {name}, reserved for {date:d} at {time}.");
            return false;
        }
    }
}
