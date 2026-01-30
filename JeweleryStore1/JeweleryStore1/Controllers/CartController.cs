using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using JeweleryStore1.Data;
using JeweleryStore1.Models;
using JeweleryStore1.Models.Responses;
using JeweleryStore1.Exceptions;
using JeweleryStore1.DTOs;

namespace JeweleryStore1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // ✅ Tüm endpoint'ler için giriş zorunlu
    public class CartController : BaseApiController // ✅ BaseApiController'dan türetildi
    {
        private readonly JewDbContext _context;
        private readonly ILogger<CartController> _logger;

        public CartController(JewDbContext context, ILogger<CartController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ✅ GÜNCELLENDİ: GET: api/Cart (userId parametresi kaldırıldı)
        [HttpGet]
        public async Task<ActionResult<ApiResponse<CartResponseDto>>> GetCart()
        {
            var userId = GetCurrentUserId(); // ✅ Token'dan userId al

            // Kullanıcının sepetini bul veya oluştur
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
            {
                // Sepet yoksa yeni oluştur
                cart = new Cart
                {
                    UserId = userId,
                    CreatedAt = DateTime.Now
                };
                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();

                _logger.LogInformation("New cart created for UserId={UserId}", userId);
            }

            // Sepet bilgilerini DTO'ya dönüştür
            var cartResponse = new CartResponseDto
            {
                CartId = cart.CartId,
                UserId = cart.UserId,
                Items = cart.CartItems.Select(ci => new CartItemDto
                {
                    CartItemId = ci.CartItemId,
                    ProductId = ci.ProductId,
                    ProductName = ci.Product.ProductName,
                    ProductImage = ci.Product.ProductImage,
                    ProductPrice = ci.Product.ProductPrice,
                    ProductDiscountPrice = ci.Product.ProductDiscountPrice,
                    Quantity = ci.Quantity,
                    Subtotal = (ci.Product.ProductDiscountPrice ?? ci.Product.ProductPrice) * ci.Quantity
                }).ToList(),
                TotalPrice = cart.CartItems.Sum(ci =>
                    (ci.Product.ProductDiscountPrice ?? ci.Product.ProductPrice) * ci.Quantity),
                TotalItems = cart.CartItems.Sum(ci => ci.Quantity)
            };

            return Ok(ApiResponse<CartResponseDto>.SuccessResponse(
                cartResponse,
                "Sepet getirildi"
            ));
        }

        // ✅ GÜNCELLENDİ: POST: api/Cart/items (userId parametresi kaldırıldı)
        [HttpPost("items")]
        public async Task<ActionResult<ApiResponse>> AddToCart([FromBody] AddToCartDto addToCartDto)
        {
            var userId = GetCurrentUserId(); // ✅ Token'dan userId al

            // Ürünü kontrol et
            var product = await _context.Products.FindAsync(addToCartDto.ProductId);
            if (product == null)
            {
                throw new NotFoundException("Ürün", addToCartDto.ProductId);
            }

            // Stok kontrolü
            if (product.ProductStock < addToCartDto.Quantity)
            {
                throw new InsufficientStockException(
                    product.ProductName,
                    addToCartDto.Quantity,
                    product.ProductStock
                );
            }

            // Kullanıcının sepetini bul veya oluştur
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
            {
                cart = new Cart
                {
                    UserId = userId,
                    CreatedAt = DateTime.Now
                };
                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();
            }

            // Ürün zaten sepette var mı?
            var existingItem = cart.CartItems
                .FirstOrDefault(ci => ci.ProductId == addToCartDto.ProductId);

            if (existingItem != null)
            {
                // Varsa miktarı artır
                var newQuantity = existingItem.Quantity + addToCartDto.Quantity;

                if (newQuantity > product.ProductStock)
                {
                    throw new InsufficientStockException(
                        product.ProductName,
                        newQuantity,
                        product.ProductStock
                    );
                }

                existingItem.Quantity = newQuantity;
                _logger.LogInformation("Cart item quantity updated: ProductId={ProductId}, NewQuantity={Quantity}",
                    addToCartDto.ProductId, newQuantity);
            }
            else
            {
                // Yoksa yeni ekle
                var cartItem = new CartItem
                {
                    CartId = cart.CartId,
                    ProductId = addToCartDto.ProductId,
                    Quantity = addToCartDto.Quantity
                };
                _context.CartItems.Add(cartItem);
                _logger.LogInformation("New item added to cart: ProductId={ProductId}, Quantity={Quantity}",
                    addToCartDto.ProductId, addToCartDto.Quantity);
            }

            await _context.SaveChangesAsync();

            return Ok(ApiResponse.SuccessResponse("Ürün sepete eklendi"));
        }

        // PUT: api/Cart/items/{cartItemId}
        [HttpPut("items/{cartItemId}")]
        public async Task<ActionResult<ApiResponse>> UpdateCartItem(int cartItemId, [FromBody] UpdateCartItemDto updateDto)
        {
            try
            {
                var userId = GetCurrentUserId(); // ✅ Token'dan userId al

                var cartItem = await _context.CartItems
                    .Include(ci => ci.Product)
                    .Include(ci => ci.Cart) // ✅ Cart'ı da include et
                    .FirstOrDefaultAsync(ci => ci.CartItemId == cartItemId);

                if (cartItem == null)
                {
                    throw new NotFoundException("Sepet ürünü", cartItemId);
                }

                // ✅ GÜVENLİK: Kullanıcı sadece kendi sepetindeki ürünü güncelleyebilir
                if (cartItem.Cart.UserId != userId)
                {
                    throw new ForbiddenException("Bu işlem için yetkiniz yok");
                }

                // Stok kontrolü
                if (cartItem.Product.ProductStock < updateDto.Quantity)
                {
                    throw new InsufficientStockException(
                        cartItem.Product.ProductName,
                        updateDto.Quantity,
                        cartItem.Product.ProductStock
                    );
                }

                cartItem.Quantity = updateDto.Quantity;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Cart item updated: CartItemId={CartItemId}, Quantity={Quantity}",
                    cartItemId, updateDto.Quantity);

                return Ok(ApiResponse.SuccessResponse("Miktar güncellendi"));
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // DELETE: api/Cart/items/{cartItemId}
        [HttpDelete("items/{cartItemId}")]
        public async Task<ActionResult<ApiResponse>> RemoveFromCart(int cartItemId)
        {
            var userId = GetCurrentUserId(); // ✅ Token'dan userId al

            var cartItem = await _context.CartItems
                .Include(ci => ci.Cart) // ✅ Cart'ı da include et
                .FirstOrDefaultAsync(ci => ci.CartItemId == cartItemId);

            if (cartItem == null)
            {
                throw new NotFoundException("Sepet ürünü", cartItemId);
            }

            // ✅ GÜVENLİK: Kullanıcı sadece kendi sepetindeki ürünü silebilir
            if (cartItem.Cart.UserId != userId)
            {
                throw new ForbiddenException("Bu işlem için yetkiniz yok");
            }

            _context.CartItems.Remove(cartItem);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Item removed from cart: CartItemId={CartItemId}", cartItemId);

            return Ok(ApiResponse.SuccessResponse("Ürün sepetten çıkarıldı"));
        }

        // ✅ GÜNCELLENDİ: DELETE: api/Cart/clear (userId parametresi kaldırıldı)
        [HttpDelete("clear")]
        public async Task<ActionResult<ApiResponse>> ClearCart()
        {
            var userId = GetCurrentUserId(); // ✅ Token'dan userId al

            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
            {
                throw new NotFoundException("Sepet bulunamadı");
            }

            var itemCount = cart.CartItems.Count;
            _context.CartItems.RemoveRange(cart.CartItems);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Cart cleared: UserId={UserId}, ItemsRemoved={Count}",
                userId, itemCount);

            return Ok(ApiResponse.SuccessResponse("Sepet temizlendi"));
        }
    }
}