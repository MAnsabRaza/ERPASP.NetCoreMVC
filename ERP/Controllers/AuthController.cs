using ERP.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;
using System.Runtime.CompilerServices;

namespace ERP.Controllers
{
    public class AuthController : Controller
    {
        private readonly AppDbContext _context;
        public AuthController(AppDbContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Login()
        {
            return View();
        }
        public async Task<IActionResult> Register()
        {
            var model = new User
            {
                current_date = DateOnly.FromDateTime(DateTime.Now)
            };
            return View("Register",model);
        }
        [HttpPost]
        public async Task<IActionResult> CheckLogin(Login login)
        {
            try
            {
                var user = await _context.User.FirstOrDefaultAsync(u => u.email == login.email && u.status == true);

                if (user != null)
                {
                    bool isPasswordValid = BCrypt.Net.BCrypt.Verify(login.password, user.password);

                    if (isPasswordValid)
                    {
                        return RedirectToAction("Index", "Home");
                    }
                }

                ViewBag.ErrorMessage = "Invalid email, password, or inactive account.";
                return View("Login", login);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        public async Task<IActionResult> CreateRegistration(User user, IFormFile logoFile)
        {
            try
            {
                user.companyId = null;
                user.roleId = null;
                if (logoFile != null && logoFile.Length > 0)
                {
                    using (var ms = new MemoryStream())
                    {
                        await logoFile.CopyToAsync(ms);
                        byte[] fileBytes = ms.ToArray();
                        user.image = Convert.ToBase64String(fileBytes);
                    }
                }
                user.password = BCrypt.Net.BCrypt.HashPassword(user.password);
                _context.User.Add(user);
                await _context.SaveChangesAsync();
                return View("Login");
            }
            catch(Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
