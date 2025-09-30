using ERP.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualBasic;
using System.IdentityModel.Tokens.Jwt;
using System.Runtime.CompilerServices;
using AspNetCoreHero.ToastNotification.Abstractions;

using System.Security.Claims;
using System.Text;

namespace ERP.Controllers
{
    public class AuthController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly INotyfService _notyf;
        public AuthController(AppDbContext context, IConfiguration configuration, INotyfService notyf)
        {
            _context = context;
            _configuration = configuration;
            _notyf = notyf;
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
                var user = await _context.User
                    .FirstOrDefaultAsync(u => u.email == login.email && u.status == true);

                if (user == null || !BCrypt.Net.BCrypt.Verify(login.password, user.password))
                {
                    _notyf.Error("Invalid email, password, or inactive account.");
                    return View("Login", login);
                }

                var token = GenerateJwtToken(user);

                HttpContext.Session.SetString("UserId", user.Id.ToString());
                HttpContext.Session.SetString("UserEmail", user.email);
                HttpContext.Session.SetString("JwtToken", token);
                _notyf.Success("Login successful!");
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                _notyf.Error($"Login failed: {ex.Message}");
                return View("Login", login);
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
                _notyf.Success("Registration successful! Please log in.");
                return View("Login");
            }
            catch(Exception ex)
            {
                _notyf.Error($"Registration failed: {ex.Message}");
                return BadRequest(ex.Message);
            }
        }
        private string GenerateJwtToken(User user)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.email),
                new Claim(ClaimTypes.Role, user.roleId?.ToString() ?? "User")
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                _configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddDays(1),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
