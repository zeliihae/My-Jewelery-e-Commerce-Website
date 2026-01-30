using System.ComponentModel.DataAnnotations;

namespace JeweleryStore1.DTOs
{
    public class RegisterDto
    {
        [Required(ErrorMessage = "İsim gereklidir")]
        [StringLength(100)]
        public string UserName { get; set; }

        [Required(ErrorMessage = "Email gereklidir")]
        [EmailAddress(ErrorMessage = "Geçerli bir email giriniz")]
        public string UserEmail { get; set; }

        [Required(ErrorMessage = "Şifre gereklidir")]
        [MinLength(6, ErrorMessage = "Şifre en az 6 karakter olmalıdır")]
        public string UserPassword { get; set; }

        [Phone(ErrorMessage = "Geçerli bir telefon numarası giriniz")]
        public string UserPhone { get; set; }
    }
}