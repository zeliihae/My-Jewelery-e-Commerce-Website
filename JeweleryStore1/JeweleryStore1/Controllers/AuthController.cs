using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using JeweleryStore1.Data;
using JeweleryStore1.Models;
using JeweleryStore1.Models.Responses;
using JeweleryStore1.Exceptions;
using JeweleryStore1.DTOs;
using JeweleryStore1.Services;
using BCrypt.Net;

namespace JeweleryStore1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : BaseApiController // ✅ BaseApiController'dan türet
    {
        private readonly JewDbContext _context;
        private readonly JwtService _jwtService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(JewDbContext context, JwtService jwtService, ILogger<AuthController> logger)
        {
            _context = context;
            _jwtService = jwtService;
            _logger = logger;
        }

        // POST: api/Auth/Register
        [HttpPost("Register")]
        public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Register([FromBody] RegisterDto registerDto)
        {
            // Email kontrolü
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.UserEmail == registerDto.UserEmail);

            if (existingUser != null)
            {
                throw new BusinessRuleException("Bu email zaten kayıtlı");
            }

            // Şifreyi hash'le
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(registerDto.UserPassword);

            // Yeni kullanıcı oluştur
            var user = new User
            {
                UserName = registerDto.UserName,
                UserEmail = registerDto.UserEmail,
                UserPassword = hashedPassword,
                UserPhone = registerDto.UserPhone,
                UserRole = 0, // ✅ 0 = Customer (varsayılan)
                UserStatus = 1, // ✅ 1 = Active
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("New user registered: UserId={UserId}, Email={Email}",
                user.UserId, user.UserEmail);

            // JWT Token oluştur (✅ UserRole eklendi)
            var token = _jwtService.GenerateToken(user.UserId, user.UserEmail, user.UserName, user.UserRole);

            var response = new AuthResponseDto
            {
                UserId = user.UserId,
                UserName = user.UserName,
                UserEmail = user.UserEmail,
                UserRole= user.UserRole,
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddMinutes(60)
            };

            return Ok(ApiResponse<AuthResponseDto>.SuccessResponse(
                response,
                "Kayıt başarılı"
            ));
        }

        // POST: api/Auth/Login
        [HttpPost("Login")]
        public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Login([FromBody] LoginDto loginDto)
        {
            // Kullanıcıyı bul
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserEmail == loginDto.UserEmail);

            if (user == null)
            {
                throw new ForbiddenException("Email veya şifre hatalı");
            }

            // ✅ Kullanıcı durumu kontrolü
            if (user.UserStatus == 0)
            {
                throw new ForbiddenException("Hesabınız devre dışı bırakılmış");
            }

            // Şifreyi kontrol et
            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(loginDto.UserPassword, user.UserPassword);

            if (!isPasswordValid)
            {
                throw new ForbiddenException("Email veya şifre hatalı");
            }

            // ✅ LastLogin güncelle
            user.LastLogin = DateTime.Now;
            await _context.SaveChangesAsync();

            _logger.LogInformation("User logged in: UserId={UserId}, Email={Email}",
                user.UserId, user.UserEmail);

            // JWT Token oluştur (✅ UserRole eklendi)
            var token = _jwtService.GenerateToken(user.UserId, user.UserEmail, user.UserName, user.UserRole);

            var response = new AuthResponseDto
            {
                UserId = user.UserId,
                UserName = user.UserName,
                UserEmail = user.UserEmail,
                UserRole= user.UserRole,
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddMinutes(60)
            };

            return Ok(ApiResponse<AuthResponseDto>.SuccessResponse(
                response,
                "Giriş başarılı"
            ));
        }

        // ✅ GÜNCELLENDİ: GET: api/Auth/Profile (Token'dan userId alınıyor)
        [HttpGet("Profile")]
        [Authorize] // ✅ Giriş zorunlu
        public async Task<ActionResult<ApiResponse<UserProfileDto>>> GetProfile()
        {
            var userId = GetCurrentUserId(); // ✅ Token'dan userId al

            var user = await _context.Users
                .Select(u => new UserProfileDto
                {
                    UserId = u.UserId,
                    UserName = u.UserName,
                    UserEmail = u.UserEmail,
                    UserPhone = u.UserPhone,
                    UserRole = u.UserRole == 1 ? "Admin" : "Customer",
                    CreatedAt = u.CreatedAt,
                    LastLogin = u.LastLogin
                })
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
            {
                throw new NotFoundException("Kullanıcı bulunamadı");
            }

            return Ok(ApiResponse<UserProfileDto>.SuccessResponse(
                user,
                "Profil bilgileri getirildi"
            ));
        }

        // ✅ YENİ: PUT: api/Auth/Profile (Profil güncelleme)
        [HttpPut("Profile")]
        [Authorize]
        public async Task<ActionResult<ApiResponse>> UpdateProfile([FromBody] UpdateProfileDto updateDto)
        {
            var userId = GetCurrentUserId(); // ✅ Token'dan userId al

            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                throw new NotFoundException("Kullanıcı bulunamadı");
            }

            // Güncelleme
            user.UserName = updateDto.UserName ?? user.UserName;
            user.UserPhone = updateDto.UserPhone ?? user.UserPhone;
            user.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            _logger.LogInformation("User profile updated: UserId={UserId}", userId);

            return Ok(ApiResponse.SuccessResponse("Profil güncellendi"));
        }

        // ✅ YENİ: PUT: api/Auth/ChangePassword (Şifre değiştirme)
        [HttpPut("ChangePassword")]
        [Authorize]
        public async Task<ActionResult<ApiResponse>> ChangePassword([FromBody] ChangePasswordDto changePasswordDto)
        {
            var userId = GetCurrentUserId(); // ✅ Token'dan userId al

            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                throw new NotFoundException("Kullanıcı bulunamadı");
            }

            // Eski şifreyi kontrol et
            bool isOldPasswordValid = BCrypt.Net.BCrypt.Verify(changePasswordDto.OldPassword, user.UserPassword);

            if (!isOldPasswordValid)
            {
                throw new ForbiddenException("Mevcut şifre hatalı");
            }

            // Yeni şifreyi hash'le ve güncelle
            user.UserPassword = BCrypt.Net.BCrypt.HashPassword(changePasswordDto.NewPassword);
            user.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            _logger.LogInformation("User password changed: UserId={UserId}", userId);

            return Ok(ApiResponse.SuccessResponse("Şifre değiştirildi"));
        }
    }
}