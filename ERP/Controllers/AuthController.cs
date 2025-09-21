using ERP.Models;
using Microsoft.AspNetCore.Mvc;
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
        public async Task<IActionResult> checkLogin(Login login)
        {
            try
            {
                return RedirectToAction("Home","Index");
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
