using System.ComponentModel.DataAnnotations;

namespace JeweleryStore1.DTOs
{
    public class LoginDto
    {
        [Required(ErrorMessage = "Email gereklidir")]
        [EmailAddress]
        public string UserEmail { get; set; }

        [Required(ErrorMessage = "Şifre gereklidir")]
        public string UserPassword { get; set; }
    }
}