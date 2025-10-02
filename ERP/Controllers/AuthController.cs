using ERP.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

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
            return View("Register", model);
        }
        public async Task<IActionResult> ForgetPassword()
        {
            return View();
        }
        public async Task<IActionResult> VerifyOtp()
        {
            return View();
        }
        public async Task<IActionResult> ChangePassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CheckLogin(Login login)
        {
            try
            {
                var user = await _context.User
                    .Include(u => u.Company)
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.email == login.email && u.status == true);

                if (user == null || !BCrypt.Net.BCrypt.Verify(login.password, user.password))
                {
                    _notyf.Error("Invalid email, password, or inactive account.");
                    return View("Login", login);
                }

                var token = GenerateJwtToken(user);

                HttpContext.Session.SetString("UserId", user.Id.ToString());
                HttpContext.Session.SetString("UserEmail", user.email);
                HttpContext.Session.SetString("UserImage", user.image);
                HttpContext.Session.SetString("JwtToken", token);
                HttpContext.Session.SetString("UserName", user.name);
                HttpContext.Session.SetString("CompanyName", user.Company.company_name);
                HttpContext.Session.SetString("RoleName", user.Role?.role_name ?? "User");

                _notyf.Success("Login successful!");
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                _notyf.Error($"Login failed: {ex.Message}");
                return View("Login", login);
            }
        }

        [HttpPost]
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
            catch (Exception ex)
            {
                _notyf.Error($"Registration failed: {ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            _notyf.Success("Logged out successfully!");
            return RedirectToAction("Login", "Auth");
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckEmailForForgetPassword(Login model)
        {
            try
            {
                var user = await _context.User.FirstOrDefaultAsync(u => u.email == model.email);
                if (user == null)
                {
                    return View("Login");
                }
                string otp = GenerateOtp();
                user.otp = otp;
                user.otp_expire = DateOnly.FromDateTime(DateTime.UtcNow);
                await _context.SaveChangesAsync();
                TempData["ResetEmail"] = model.email;
                return RedirectToAction("VerifyOtp");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckVerifyOtp(VerifyOtp vo)
        {
            try
            {
                var email = TempData["ResetEmail"] as string;
                if (string.IsNullOrEmpty(email))
                {
                    _notyf.Error("Session expired. Please try again.");
                    return RedirectToAction("ForgetPassword");
                }

                var user = await _context.User.FirstOrDefaultAsync(u => u.email == email);
                if (user == null || user.otp == null || user.otp_expire == null)
                {
                    _notyf.Error("Invalid or expired OTP. Please request a new one.");
                    return RedirectToAction("ForgetPassword");
                }
                if (user.otp != vo.otp)
                {
                    _notyf.Error("Incorrect OTP. Please try again.");
                    return View("VerifyOtp", vo);
                }

                if (user.otp_expire < DateOnly.FromDateTime(DateTime.UtcNow))
                {
                    _notyf.Error("OTP has expired. Please request a new one.");
                    return RedirectToAction("ForgetPassword");
                }

                TempData["ResetUserId"] = user.Id;
                _notyf.Success("OTP verified. Change your password.");
                return RedirectToAction("ChangePassword");
            }
            catch (Exception ex)
            {
                _notyf.Error($"An error occurred: {ex.Message}");
                return BadRequest(ex.Message);
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateNewPassword(ChangePassword cp)
        {
            try
            {
                var userId = TempData["ResetUserId"] as int?;
                if (!userId.HasValue)
                {
                    return RedirectToAction("Login");
                }
                var user=await _context.User.FindAsync(userId.Value);
                if(user == null)
                {
                    return RedirectToAction("Login");
                }
                if (cp.Password != cp.ConfirmPassword)
                {
                    _notyf.Error("Passwords do not match.");
                    return View("ChangePassword", cp);
                }
                user.password = BCrypt.Net.BCrypt.HashPassword(cp.Password);
                user.otp = null;
                user.otp_expire = null;
                await _context.SaveChangesAsync();
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        private string GenerateJwtToken(User user)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.email),
                new Claim(ClaimTypes.Role, user.roleId.ToString())
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
       
        private string GenerateOtp(int length = 6)
        {
            var random = new Random();
            const string chars = "0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}