using System;
using System.Collections.Generic;

namespace JeweleryStore1.Models;

public partial class Address
{
    public int AddressId { get; set; }

    public int UserId { get; set; }

    public string AddressTitle { get; set; } = null!;

    public string RecipientName { get; set; } = null!;

    public string RecipientPhone { get; set; } = null!;

    public string Country { get; set; } = null!;

    public string City { get; set; } = null!;

    public string District { get; set; } = null!;

    public string AddressDetail { get; set; } = null!;

    public string? PostalCode { get; set; }

    public bool IsDefault { get; set; }

    public bool IsBilling { get; set; }

    public bool IsShipping { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
