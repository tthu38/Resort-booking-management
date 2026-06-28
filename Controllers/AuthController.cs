using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ResortBookingMVC.Data;
using ResortBookingMVC.Interfaces;
using ResortBookingMVC.Models;
using ResortBookingMVC.Models.Enums;
using ResortBookingMVC.ViewModels;
using System.Collections.Concurrent;
using System.Security.Claims;

namespace ResortBookingMVC.Controllers
{
    public class AuthController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IEmailService _emailService;

        // Lưu OTP và Token tạm trong bộ nhớ
        private static readonly ConcurrentDictionary<string, (string Otp, DateTime Expiry)> _otpStore = new();
        private static readonly ConcurrentDictionary<string, (string Email, DateTime Expiry)> _tokenStore = new();

        public AuthController(AppDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        // ─────────────────────────────────────
        // LOGIN
        // ─────────────────────────────────────

        [HttpGet]
        public IActionResult Login(string? returnUrl)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");

            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == model.Email && u.IsActive);

            if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
            {
                ModelState.AddModelError("", "Email hoặc mật khẩu không đúng.");
                return View(model);
            }

            await SignInUser(user, model.RememberMe);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return user.Role == Role.ADMIN || user.Role == Role.STAFF
                ? RedirectToAction("Index", "Admin")
                : RedirectToAction("Index", "Home");
        }

        // ─────────────────────────────────────
        // GOOGLE LOGIN
        // ─────────────────────────────────────

        [HttpGet]
        public IActionResult LoginWithGoogle()
        {
            var redirectUrl = Url.Action("GoogleCallback", "Auth");
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        [HttpGet]
        public async Task<IActionResult> GoogleCallback()
        {
            var result = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);

            if (!result.Succeeded)
            {
                TempData["Error"] = "Đăng nhập Google thất bại.";
                return RedirectToAction("Login");
            }

            var claims = result.Principal!.Claims.ToList();
            var email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var fullName = claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;

            if (string.IsNullOrEmpty(email))
            {
                TempData["Error"] = "Không lấy được email từ Google.";
                return RedirectToAction("Login");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                // Tạo tài khoản mới từ Google
                user = new User
                {
                    FullName = fullName ?? email,
                    Email = email,
                    PasswordHash = null,
                    Role = Role.CUSTOMER,
                    IsEmailVerified = true,
                    IsActive = true
                };
                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Chào mừng {user.FullName}! Tài khoản đã được tạo.";
            }
            else if (!user.IsActive)
            {
                TempData["Error"] = "Tài khoản của bạn đã bị khóa.";
                return RedirectToAction("Login");
            }

            await SignInUser(user, false);
            return RedirectToAction("Index", "Home");
        }

        // ─────────────────────────────────────
        // REGISTER
        // ─────────────────────────────────────

        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            if (await _context.Users.AnyAsync(u => u.Email == model.Email))
            {
                ModelState.AddModelError("Email", "Email này đã được sử dụng.");
                return View(model);
            }

            var user = new User
            {
                FullName = model.FullName,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                Role = Role.CUSTOMER,
                IsEmailVerified = true,
                IsActive = true
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            await SignInUser(user, false);
            TempData["Success"] = "Đăng ký thành công! Chào mừng bạn đến với Resort Booking.";
            return RedirectToAction("Index", "Home");
        }

        // ─────────────────────────────────────
        // LOGOUT
        // ─────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // Xóa cookie khỏi trình duyệt
            Response.Cookies.Delete(".AspNetCore.Cookies");

            return RedirectToAction("Index", "Home");
        }

        // ─────────────────────────────────────
        // PROFILE
        // ─────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            if (User.Identity?.IsAuthenticated != true)
                return RedirectToAction("Login");

            var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            return View(user);
        }

        [HttpGet]
        public async Task<IActionResult> EditProfile()
        {
            if (User.Identity?.IsAuthenticated != true)
                return RedirectToAction("Login");

            var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            var vm = new EditProfileViewModel
            {
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(EditProfileViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            if (await _context.Users.AnyAsync(u => u.Email == model.Email && u.Id != userId))
            {
                ModelState.AddModelError("Email", "Email này đã được sử dụng bởi tài khoản khác.");
                return View(model);
            }

            user.FullName = model.FullName;
            user.Email = model.Email;
            user.PhoneNumber = model.PhoneNumber;
            user.UpdatedAt = DateTime.UtcNow;

            if (!string.IsNullOrWhiteSpace(model.NewPassword))
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);

            await _context.SaveChangesAsync();
            await SignInUser(user, false);

            TempData["Success"] = "Cập nhật thông tin thành công!";
            return RedirectToAction("Profile");
        }

        // ─────────────────────────────────────
        // FORGOT PASSWORD — Bước 1: Nhập email
        // ─────────────────────────────────────

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == model.Email && u.IsActive);

            if (user != null)
            {
                var otp = new Random().Next(100000, 999999).ToString();
                _otpStore[model.Email] = (otp, DateTime.UtcNow.AddMinutes(10));
                await _emailService.SendOtpAsync(user.Email, user.FullName, otp);
            }

            TempData["Info"] = "Nếu email tồn tại, mã OTP đã được gửi.";
            return RedirectToAction("VerifyOtp", new { email = model.Email });
        }

        // ─────────────────────────────────────
        // FORGOT PASSWORD — Bước 2: Nhập OTP
        // ─────────────────────────────────────

        [HttpGet]
        public IActionResult VerifyOtp(string email)
        {
            return View(new VerifyOtpViewModel { Email = email });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult VerifyOtp(VerifyOtpViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            if (!_otpStore.TryGetValue(model.Email, out var stored)
                || stored.Expiry < DateTime.UtcNow
                || stored.Otp != model.Otp)
            {
                ModelState.AddModelError("Otp", "Mã OTP không đúng hoặc đã hết hạn.");
                return View(model);
            }

            var token = Guid.NewGuid().ToString("N");
            _tokenStore[token] = (model.Email, DateTime.UtcNow.AddMinutes(15));
            _otpStore.TryRemove(model.Email, out _);

            return RedirectToAction("ResetPassword", new { token });
        }

        // ─────────────────────────────────────
        // FORGOT PASSWORD — Bước 3: Đặt lại mật khẩu
        // ─────────────────────────────────────

        [HttpGet]
        public IActionResult ResetPassword(string token)
        {
            if (!_tokenStore.TryGetValue(token, out var stored)
                || stored.Expiry < DateTime.UtcNow)
            {
                TempData["Error"] = "Liên kết đặt lại mật khẩu đã hết hạn.";
                return RedirectToAction("ForgotPassword");
            }

            return View(new ResetPasswordViewModel
            {
                Token = token,
                Email = stored.Email
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            if (!_tokenStore.TryGetValue(model.Token, out var stored)
                || stored.Expiry < DateTime.UtcNow
                || stored.Email != model.Email)
            {
                TempData["Error"] = "Liên kết đã hết hạn. Vui lòng thử lại.";
                return RedirectToAction("ForgotPassword");
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == model.Email && u.IsActive);

            if (user == null) return NotFound();

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _tokenStore.TryRemove(model.Token, out _);

            TempData["Success"] = "Đặt lại mật khẩu thành công! Vui lòng đăng nhập.";
            return RedirectToAction("Login");
        }

        // ─────────────────────────────────────
        // HELPER
        // ─────────────────────────────────────

        private async Task SignInUser(User user, bool isPersistent)
        {
            var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new(ClaimTypes.Name,           user.FullName),
        new(ClaimTypes.Email,          user.Email),
        new(ClaimTypes.Role,           user.Role.ToString())
    };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            var properties = new AuthenticationProperties
            {
                IsPersistent = isPersistent,
                ExpiresUtc = isPersistent
                    ? DateTimeOffset.UtcNow.AddDays(1)   // nhớ đăng nhập → 1 ngày
                    : null                                 // không nhớ → đóng tab là hết
            };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, properties);
        }
    }
}