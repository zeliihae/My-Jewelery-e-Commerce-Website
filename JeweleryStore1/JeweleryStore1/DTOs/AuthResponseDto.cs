namespace JeweleryStore1.DTOs
{
    public class AuthResponseDto
    {
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string UserEmail { get; set; }
        public string Token { get; set; }
        public DateTime ExpiresAt { get; set; }

        public byte UserRole { get; set; }
    }

    public class UserProfileDto
    {
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string UserEmail { get; set; }
        public string? UserPhone { get; set; }
        public string UserRole { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLogin { get; set; }
    }

    public class UpdateProfileDto
    {
        public string? UserName { get; set; }
        public string? UserPhone { get; set; }
    }

    public class ChangePasswordDto
    {
        public string OldPassword { get; set; }
        public string NewPassword { get; set; }
    }

}