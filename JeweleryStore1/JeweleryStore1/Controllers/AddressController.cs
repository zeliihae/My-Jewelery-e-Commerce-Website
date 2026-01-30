using JeweleryStore1.Data;
using JeweleryStore1.DTOs;
using JeweleryStore1.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JeweleryStore1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
  
    public class AddressesController : ControllerBase
    {
        private readonly JewDbContext _context;

        public AddressesController(JewDbContext context)
        {
            _context = context;
        }

        // GET: api/Addresses/user/{userId}
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<AddressResponseDto>>> GetUserAddresses(int userId)
        {
            try
            {
                var addresses = await _context.Addresses
                    .Where(a => a.UserId == userId)
                    .OrderByDescending(a => a.IsDefault)
                    .ThenByDescending(a => a.CreatedAt)
                    .ToListAsync();

                var addressResponses = addresses.Select(a => new AddressResponseDto
                {
                    AddressId = a.AddressId,
                    UserId = a.UserId,
                    AddressTitle = a.AddressTitle,
                    RecipientName = a.RecipientName,
                    RecipientPhone = a.RecipientPhone,
                    Country = a.Country,
                    City = a.City,
                    District = a.District,
                    AddressDetail = a.AddressDetail,
                    PostalCode = a.PostalCode,
                    IsDefault = a.IsDefault,
                    IsBilling = a.IsBilling,
                    IsShipping = a.IsShipping,
                    CreatedAt = a.CreatedAt,
                    UpdatedAt = a.UpdatedAt
                }).ToList();

                return Ok(addressResponses);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Adresler alınırken hata oluştu", error = ex.Message });
            }
        }

        // GET: api/Addresses/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<AddressResponseDto>> GetAddress(int id)
        {
            try
            {
                var address = await _context.Addresses.FindAsync(id);

                if (address == null)
                {
                    return NotFound(new { message = "Adres bulunamadı" });
                }

                var addressResponse = new AddressResponseDto
                {
                    AddressId = address.AddressId,
                    UserId = address.UserId,
                    AddressTitle = address.AddressTitle,
                    RecipientName = address.RecipientName,
                    RecipientPhone = address.RecipientPhone,
                    Country = address.Country,
                    City = address.City,
                    District = address.District,
                    AddressDetail = address.AddressDetail,
                    PostalCode = address.PostalCode,
                    IsDefault = address.IsDefault,
                    IsBilling = address.IsBilling,
                    IsShipping = address.IsShipping,
                    CreatedAt = address.CreatedAt,
                    UpdatedAt = address.UpdatedAt
                };

                return Ok(addressResponse);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Adres alınırken hata oluştu", error = ex.Message });
            }
        }

        // GET: api/Addresses/user/{userId}/default
        [HttpGet("user/{userId}/default")]
        public async Task<ActionResult<AddressResponseDto>> GetDefaultAddress(int userId)
        {
            try
            {
                var address = await _context.Addresses
                    .FirstOrDefaultAsync(a => a.UserId == userId && a.IsDefault);

                if (address == null)
                {
                    return NotFound(new { message = "Varsayılan adres bulunamadı" });
                }

                var addressResponse = new AddressResponseDto
                {
                    AddressId = address.AddressId,
                    UserId = address.UserId,
                    AddressTitle = address.AddressTitle,
                    RecipientName = address.RecipientName,
                    RecipientPhone = address.RecipientPhone,
                    Country = address.Country,
                    City = address.City,
                    District = address.District,
                    AddressDetail = address.AddressDetail,
                    PostalCode = address.PostalCode,
                    IsDefault = address.IsDefault,
                    IsBilling = address.IsBilling,
                    IsShipping = address.IsShipping,
                    CreatedAt = address.CreatedAt,
                    UpdatedAt = address.UpdatedAt
                };

                return Ok(addressResponse);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Varsayılan adres alınırken hata oluştu", error = ex.Message });
            }
        }

        // GET: api/Addresses/user/{userId}/shipping
        [HttpGet("user/{userId}/shipping")]
        public async Task<ActionResult<IEnumerable<AddressResponseDto>>> GetShippingAddresses(int userId)
        {
            try
            {
                var addresses = await _context.Addresses
                    .Where(a => a.UserId == userId && a.IsShipping)
                    .OrderByDescending(a => a.IsDefault)
                    .ToListAsync();

                var addressResponses = addresses.Select(a => new AddressResponseDto
                {
                    AddressId = a.AddressId,
                    UserId = a.UserId,
                    AddressTitle = a.AddressTitle,
                    RecipientName = a.RecipientName,
                    RecipientPhone = a.RecipientPhone,
                    Country = a.Country,
                    City = a.City,
                    District = a.District,
                    AddressDetail = a.AddressDetail,
                    PostalCode = a.PostalCode,
                    IsDefault = a.IsDefault,
                    IsBilling = a.IsBilling,
                    IsShipping = a.IsShipping,
                    CreatedAt = a.CreatedAt,
                    UpdatedAt = a.UpdatedAt
                }).ToList();

                return Ok(addressResponses);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Teslimat adresleri alınırken hata oluştu", error = ex.Message });
            }
        }

        // GET: api/Addresses/user/{userId}/billing
        [HttpGet("user/{userId}/billing")]
        public async Task<ActionResult<IEnumerable<AddressResponseDto>>> GetBillingAddresses(int userId)
        {
            try
            {
                var addresses = await _context.Addresses
                    .Where(a => a.UserId == userId && a.IsBilling)
                    .OrderByDescending(a => a.IsDefault)
                    .ToListAsync();

                var addressResponses = addresses.Select(a => new AddressResponseDto
                {
                    AddressId = a.AddressId,
                    UserId = a.UserId,
                    AddressTitle = a.AddressTitle,
                    RecipientName = a.RecipientName,
                    RecipientPhone = a.RecipientPhone,
                    Country = a.Country,
                    City = a.City,
                    District = a.District,
                    AddressDetail = a.AddressDetail,
                    PostalCode = a.PostalCode,
                    IsDefault = a.IsDefault,
                    IsBilling = a.IsBilling,
                    IsShipping = a.IsShipping,
                    CreatedAt = a.CreatedAt,
                    UpdatedAt = a.UpdatedAt
                }).ToList();

                return Ok(addressResponses);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Fatura adresleri alınırken hata oluştu", error = ex.Message });
            }
        }

        // POST: api/Addresses
        [HttpPost]
        public async Task<ActionResult<AddressResponseDto>> CreateAddress([FromBody] CreateAddressDto createDto)
        {
            try
            {
                // Kullanıcı var mı kontrol et
                var user = await _context.Users.FindAsync(createDto.UserId);
                if (user == null)
                {
                    return NotFound(new { message = "Kullanıcı bulunamadı" });
                }

                // Eğer bu ilk adres ise otomatik olarak varsayılan yap
                var hasExistingAddresses = await _context.Addresses
                    .AnyAsync(a => a.UserId == createDto.UserId);

                var isFirstAddress = !hasExistingAddresses;

                // Eğer varsayılan olarak işaretlenmişse, diğerlerini varsayılan olmaktan çıkar
                if (createDto.IsDefault || isFirstAddress)
                {
                    var existingAddresses = await _context.Addresses
                        .Where(a => a.UserId == createDto.UserId && a.IsDefault)
                        .ToListAsync();

                    foreach (var addr in existingAddresses)
                    {
                        addr.IsDefault = false;
                    }
                }

                var address = new Address
                {
                    UserId = createDto.UserId,
                    AddressTitle = createDto.AddressTitle,
                    RecipientName = createDto.RecipientName,
                    RecipientPhone = createDto.RecipientPhone,
                    Country = createDto.Country,
                    City = createDto.City,
                    District = createDto.District,
                    AddressDetail = createDto.AddressDetail,
                    PostalCode = createDto.PostalCode,
                    IsDefault = createDto.IsDefault || isFirstAddress,
                    IsBilling = createDto.IsBilling,
                    IsShipping = createDto.IsShipping,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                _context.Addresses.Add(address);
                await _context.SaveChangesAsync();

                var response = new AddressResponseDto
                {
                    AddressId = address.AddressId,
                    UserId = address.UserId,
                    AddressTitle = address.AddressTitle,
                    RecipientName = address.RecipientName,
                    RecipientPhone = address.RecipientPhone,
                    Country = address.Country,
                    City = address.City,
                    District = address.District,
                    AddressDetail = address.AddressDetail,
                    PostalCode = address.PostalCode,
                    IsDefault = address.IsDefault,
                    IsBilling = address.IsBilling,
                    IsShipping = address.IsShipping,
                    CreatedAt = address.CreatedAt,
                    UpdatedAt = address.UpdatedAt
                };

                return CreatedAtAction(nameof(GetAddress), new { id = address.AddressId }, response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Adres eklenirken hata oluştu", error = ex.Message });
            }
        }

        // PUT: api/Addresses/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateAddress(int id, [FromBody] UpdateAddressDto updateDto)
        {
            try
            {
                // Validation
                if (string.IsNullOrWhiteSpace(updateDto.AddressTitle) ||
                    string.IsNullOrWhiteSpace(updateDto.RecipientName) ||
                    string.IsNullOrWhiteSpace(updateDto.RecipientPhone) ||
                    string.IsNullOrWhiteSpace(updateDto.Country) ||
                    string.IsNullOrWhiteSpace(updateDto.City) ||
                    string.IsNullOrWhiteSpace(updateDto.District) ||
                    string.IsNullOrWhiteSpace(updateDto.AddressDetail))
                {
                    return BadRequest(new { message = "Zorunlu alanlar boş bırakılamaz" });
                }

                var address = await _context.Addresses.FindAsync(id);

                if (address == null)
                {
                    return NotFound(new { message = "Adres bulunamadı" });
                }

                // Eğer varsayılan olarak işaretlenmişse, diğerlerini varsayılan olmaktan çıkar
                if (updateDto.IsDefault && !address.IsDefault)
                {
                    var existingAddresses = await _context.Addresses
                        .Where(a => a.UserId == address.UserId && a.IsDefault && a.AddressId != id)
                        .ToListAsync();

                    foreach (var addr in existingAddresses)
                    {
                        addr.IsDefault = false;
                    }
                }

                address.AddressTitle = updateDto.AddressTitle;
                address.RecipientName = updateDto.RecipientName;
                address.RecipientPhone = updateDto.RecipientPhone;
                address.Country = updateDto.Country;
                address.City = updateDto.City;
                address.District = updateDto.District;
                address.AddressDetail = updateDto.AddressDetail;
                address.PostalCode = updateDto.PostalCode;
                address.IsDefault = updateDto.IsDefault;
                address.IsBilling = updateDto.IsBilling;
                address.IsShipping = updateDto.IsShipping;
                address.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                return Ok(new { message = "Adres güncellendi" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Adres güncellenirken hata oluştu", error = ex.Message });
            }
        }

        // PUT: api/Addresses/{id}/set-default
        [HttpPut("{id}/set-default")]
        public async Task<ActionResult> SetDefaultAddress(int id, [FromQuery] int userId)
        {
            try
            {
                var address = await _context.Addresses.FindAsync(id);

                if (address == null)
                {
                    return NotFound(new { message = "Adres bulunamadı" });
                }

                if (address.UserId != userId)
                {
                    return Forbid();
                }

                // Diğer adresleri varsayılan olmaktan çıkar
                var existingAddresses = await _context.Addresses
                    .Where(a => a.UserId == userId && a.IsDefault)
                    .ToListAsync();

                foreach (var addr in existingAddresses)
                {
                    addr.IsDefault = false;
                }

                // Bu adresi varsayılan yap
                address.IsDefault = true;
                address.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                return Ok(new { message = "Varsayılan adres ayarlandı" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Varsayılan adres ayarlanırken hata oluştu", error = ex.Message });
            }
        }

        // DELETE: api/Addresses/{id}
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteAddress(int id, [FromQuery] int userId)
        {
            try
            {
                var address = await _context.Addresses.FindAsync(id);

                if (address == null)
                {
                    return NotFound(new { message = "Adres bulunamadı" });
                }

                // Sadece adres sahibi silebilir
                if (address.UserId != userId)
                {
                    return Forbid();
                }

                var wasDefault = address.IsDefault;

                _context.Addresses.Remove(address);
                await _context.SaveChangesAsync();

                // Eğer silinen adres varsayılandıysa, başka bir adresi varsayılan yap
                if (wasDefault)
                {
                    var newDefaultAddress = await _context.Addresses
                        .Where(a => a.UserId == userId)
                        .OrderByDescending(a => a.CreatedAt)
                        .FirstOrDefaultAsync();

                    if (newDefaultAddress != null)
                    {
                        newDefaultAddress.IsDefault = true;
                        await _context.SaveChangesAsync();
                    }
                }

                return Ok(new { message = "Adres silindi" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Adres silinirken hata oluştu", error = ex.Message });
            }
        }
    }
}

