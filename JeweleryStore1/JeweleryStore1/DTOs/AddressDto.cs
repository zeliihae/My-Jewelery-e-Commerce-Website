// DTOs
namespace JeweleryStore1.DTOs
{
    public class AddressResponseDto
    {
        public int AddressId { get; set; }
        public int UserId { get; set; }
        public string AddressTitle { get; set; }
        public string RecipientName { get; set; }
        public string RecipientPhone { get; set; }
        public string Country { get; set; }
        public string City { get; set; }
        public string District { get; set; }
        public string AddressDetail { get; set; }
        public string? PostalCode { get; set; }
        public bool IsDefault { get; set; }
        public bool IsBilling { get; set; }
        public bool IsShipping { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class CreateAddressDto
    {
        public int UserId { get; set; }
        public string AddressTitle { get; set; } = string.Empty;
        public string RecipientName { get; set; } = string.Empty;
        public string RecipientPhone { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string District { get; set; } = string.Empty;
        public string AddressDetail { get; set; } = string.Empty;
        public string? PostalCode { get; set; }
        public bool IsDefault { get; set; }
        public bool IsBilling { get; set; }
        public bool IsShipping { get; set; }
    }

    public class UpdateAddressDto
    {
        public string AddressTitle { get; set; } = string.Empty;
        public string RecipientName { get; set; } = string.Empty;
        public string RecipientPhone { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string District { get; set; } = string.Empty;
        public string AddressDetail { get; set; } = string.Empty;
        public string? PostalCode { get; set; }
        public bool IsDefault { get; set; }
        public bool IsBilling { get; set; }
        public bool IsShipping { get; set; }
    }
}