using Microsoft.AspNetCore.Mvc;
using JeweleryStore1.Exceptions;
using System.Security.Claims;

namespace JeweleryStore1.Controllers
{
    [ApiController]
    public class BaseApiController : ControllerBase
    {
        
        /// Token'dan kullanıcı ID'sini alır
        protected int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim))
            {
                throw new ForbiddenException("Token geçersiz veya kullanıcı bilgisi bulunamadı");
            }

            if (!int.TryParse(userIdClaim, out int userId))
            {
                throw new ForbiddenException("Geçersiz kullanıcı bilgisi");
            }

            return userId;
        }

    
        /// Token'dan kullanıcı email'ini alır
    
        protected string GetCurrentUserEmail()
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;

            if (string.IsNullOrEmpty(email))
            {
                throw new ForbiddenException("Email bilgisi bulunamadı");
            }

            return email;
        }

      
        /// Token'dan kullanıcı adını alır
     
        protected string GetCurrentUserName()
        {
            var name = User.FindFirst(ClaimTypes.Name)?.Value;

            if (string.IsNullOrEmpty(name))
            {
                throw new ForbiddenException("Kullanıcı adı bulunamadı");
            }

            return name;
        }

       
        /// Kullanıcının Admin olup olmadığını kontrol eder
       
        protected bool IsAdmin()
        {
            return User.IsInRole("Admin");
        }
    }
}