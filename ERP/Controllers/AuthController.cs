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
                    _notyf.Error("Email not found. Please check and try again.");
                    return View("ForgetPassword", model);
                }

                string otp = GenerateOtp();
                user.otp = otp;
                user.otp_expiry = DateTime.Now.AddMinutes(2);
                await _context.SaveChangesAsync();

                TempData["ResetEmail"] = model.email;
                TempData.Keep("ResetEmail"); 

                _notyf.Success($"OTP sent successfully! Your OTP is: {otp}"); 
                return RedirectToAction("VerifyOtp");
            }
            catch (Exception ex)
            {
                _notyf.Error($"An error occurred: {ex.Message}");
                return View("ForgetPassword", model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckVerifyOtp(VerifyOtp vo)
        {
            try
            {
                var email = TempData["ResetEmail"]?.ToString();
                if (string.IsNullOrEmpty(email))
                {
                    _notyf.Error("Session expired. Please try again.");
                    return RedirectToAction("ForgetPassword");
                }

                var user = await _context.User.FirstOrDefaultAsync(u => u.email == email);
                if (user == null)
                {
                    _notyf.Error("User not found. Please try again.");
                    return RedirectToAction("ForgetPassword");
                }

                if (string.IsNullOrEmpty(user.otp))
                {
                    _notyf.Error("Invalid or expired OTP. Please request a new one.");
                    return RedirectToAction("ForgetPassword");
                }

                // 🕒 Check if OTP has expired
                if (user.otp_expiry < DateTime.Now)
                {
                    user.otp = null; // clear OTP if expired
                    await _context.SaveChangesAsync();

                    _notyf.Error("Your OTP has expired. Please request a new one.");
                    return RedirectToAction("ForgetPassword");
                }

                // ✅ Check OTP match
                if (user.otp != vo.otp)
                {
                    _notyf.Error("Incorrect OTP. Please try again.");
                    TempData["ResetEmail"] = email;
                    TempData.Keep("ResetEmail");
                    return View("VerifyOtp", vo);
                }

                // ✅ OTP valid
                TempData["ResetEmail"] = email;
                TempData.Keep("ResetEmail");

                _notyf.Success("OTP verified successfully. Please set your new password.");
                return RedirectToAction("ChangePassword");
            }
            catch (Exception ex)
            {
                _notyf.Error($"An error occurred: {ex.Message}");
                var email = TempData["ResetEmail"]?.ToString();
                if (!string.IsNullOrEmpty(email))
                {
                    TempData["ResetEmail"] = email;
                    TempData.Keep("ResetEmail");
                }
                return View("VerifyOtp", vo);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateNewPassword(ChangePassword cp)
        {
            try
            {
                var email = TempData["ResetEmail"]?.ToString();
                if (string.IsNullOrEmpty(email))
                {
                    _notyf.Error("Session expired. Please verify OTP again.");
                    return RedirectToAction("ForgetPassword");
                }

                var user = await _context.User.FirstOrDefaultAsync(u => u.email == email);
                if (user == null)
                {
                    _notyf.Error("User not found. Please try again.");
                    return RedirectToAction("ForgetPassword");
                }

                if (cp.Password != cp.ConfirmPassword)
                {
                    _notyf.Error("Passwords do not match. Please try again.");
                    TempData["ResetEmail"] = email;
                    TempData.Keep("ResetEmail"); 
                    return View("ChangePassword", cp);
                }

                if (string.IsNullOrEmpty(cp.Password) || cp.Password.Length < 6)
                {
                    _notyf.Error("Password must be at least 6 characters long.");
                    TempData["ResetEmail"] = email;
                    TempData.Keep("ResetEmail"); 
                    return View("ChangePassword", cp);
                }

                user.password = BCrypt.Net.BCrypt.HashPassword(cp.Password);
                user.otp = null;
                await _context.SaveChangesAsync();
                _notyf.Success("Password changed successfully! Please login with your new password.");
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                _notyf.Error($"An error occurred: {ex.Message}");
                var email = TempData["ResetEmail"]?.ToString();
                if (!string.IsNullOrEmpty(email))
                {
                    TempData["ResetEmail"] = email;
                    TempData.Keep("ResetEmail");
                }
                return View("ChangePassword", cp);
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