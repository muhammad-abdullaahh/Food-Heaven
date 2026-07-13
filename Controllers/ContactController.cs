using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using FoodHeaven.Data;
using FoodHeaven.Models;
using Microsoft.Data.SqlClient;

namespace FoodHeaven.Controllers
{
    public class ContactController : Controller
    {
        private readonly DbHelper _db;

        public ContactController(DbHelper db)
        {
            _db = db;
        }

        // GET: Contact
        public IActionResult Index() => View();

        // POST: Contact/Submit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(ContactMessage contactMessage)
        {
            if (ModelState.IsValid)
            {
                await _db.ExecuteAsync(
                    @"INSERT INTO ContactMessages (FullName, Email, PhoneNumber, Message, IsRead, CreatedAt)
                      VALUES (@fn, @em, @ph, @msg, 0, @ca)",
                    new[]
                    {
                        new SqlParameter("@fn",  contactMessage.FullName),
                        new SqlParameter("@em",  contactMessage.Email),
                        new SqlParameter("@ph",  contactMessage.PhoneNumber),
                        new SqlParameter("@msg", contactMessage.Message),
                        new SqlParameter("@ca",  DateTime.Now)
                    });

                TempData["SuccessMessage"] = "Thank you for contacting us! We'll get back to you soon.";
                return RedirectToAction(nameof(Index));
            }
            return View("Index", contactMessage);
        }

        // POST: Contact/SubmitAjax
        [HttpPost]
        public async Task<JsonResult> SubmitAjax([FromBody] ContactMessage message)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    await _db.ExecuteAsync(
                        @"INSERT INTO ContactMessages (FullName, Email, PhoneNumber, Message, IsRead, CreatedAt)
                          VALUES (@fn, @em, @ph, @msg, 0, @ca)",
                        new[]
                        {
                            new SqlParameter("@fn",  message.FullName),
                            new SqlParameter("@em",  message.Email),
                            new SqlParameter("@ph",  message.PhoneNumber),
                            new SqlParameter("@msg", message.Message),
                            new SqlParameter("@ca",  DateTime.Now)
                        });

                    return Json(new { success = true, message = "Thank you for contacting us! We'll get back to you soon." });
                }
                return Json(new { success = false, message = "Please fill in all required fields correctly." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
