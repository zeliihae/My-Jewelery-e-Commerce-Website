using System;
using System.Collections.Generic;
using JeweleryStore1.Models;
using Microsoft.EntityFrameworkCore;

namespace JeweleryStore1.Data;

public partial class JewDbContext : DbContext
{
    public JewDbContext()
    {
    }

    public JewDbContext(DbContextOptions<JewDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Address> Addresses { get; set; }

    public virtual DbSet<Cart> Carts { get; set; }

    public virtual DbSet<CartItem> CartItems { get; set; }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<Coupon> Coupons { get; set; }

    public virtual DbSet<Favorite> Favorites { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<OrderItem> OrderItems { get; set; }

    public virtual DbSet<OrderStatusHistory> OrderStatusHistories { get; set; }

    public virtual DbSet<OrderSummary> OrderSummaries { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<ProductImage> ProductImages { get; set; }

    public virtual DbSet<ProductStockStatus> ProductStockStatuses { get; set; }

    public virtual DbSet<Receipt> Receipts { get; set; }

    public virtual DbSet<ReceiptItem> ReceiptItems { get; set; }

    public virtual DbSet<Review> Reviews { get; set; }

    public virtual DbSet<User> Users { get; set; }

   
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Address>(entity =>
        {
            entity.HasKey(e => e.AddressId).HasName("PK__Addresse__0382FFC2D05403EC");

            entity.HasIndex(e => e.IsDefault, "IX_Addresses_default");

            entity.HasIndex(e => e.UserId, "IX_Addresses_user");

            entity.Property(e => e.AddressId).HasColumnName("Address_id");
            entity.Property(e => e.AddressDetail).HasColumnName("Address_detail");
            entity.Property(e => e.AddressTitle)
                .HasMaxLength(100)
                .HasColumnName("Address_title");
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.Country)
                .HasMaxLength(50)
                .HasDefaultValue("Türkiye");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("Created_at");
            entity.Property(e => e.District).HasMaxLength(100);
            entity.Property(e => e.IsBilling).HasColumnName("Is_billing");
            entity.Property(e => e.IsDefault).HasColumnName("Is_default");
            entity.Property(e => e.IsShipping)
                .HasDefaultValue(true)
                .HasColumnName("Is_shipping");
            entity.Property(e => e.PostalCode)
                .HasMaxLength(10)
                .HasColumnName("Postal_code");
            entity.Property(e => e.RecipientName)
                .HasMaxLength(100)
                .HasColumnName("Recipient_name");
            entity.Property(e => e.RecipientPhone)
                .HasMaxLength(20)
                .HasColumnName("Recipient_phone");
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("Updated_at");
            entity.Property(e => e.UserId).HasColumnName("User_id");

            entity.HasOne(d => d.User).WithMany(p => p.Addresses)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_Addresses_Users");
        });

        modelBuilder.Entity<Cart>(entity =>
        {
            entity.HasKey(e => e.CartId).HasName("PK__Carts__D6862FC119C682D8");

            entity.HasIndex(e => e.SessionId, "IX_Carts_session");

            entity.HasIndex(e => e.UserId, "IX_Carts_user");

            entity.Property(e => e.CartId).HasColumnName("Cart_id");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("Created_at");
            entity.Property(e => e.ExpiresAt)
                .HasPrecision(3)
                .HasColumnName("Expires_at");
            entity.Property(e => e.SessionId)
                .HasMaxLength(100)
                .HasColumnName("Session_id");
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("Updated_at");
            entity.Property(e => e.UserId).HasColumnName("User_id");

            entity.HasOne(d => d.User).WithMany(p => p.Carts)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_Carts_Users");
        });

        modelBuilder.Entity<CartItem>(entity =>
        {
            entity.HasKey(e => e.CartItemId).HasName("PK__CartItem__64E9CA49C667D340");

            entity.HasIndex(e => e.CartId, "IX_CartItems_cart");

            entity.HasIndex(e => e.ProductId, "IX_CartItems_product");

            entity.HasIndex(e => new { e.CartId, e.ProductId }, "UQ_Cart_Product").IsUnique();

            entity.Property(e => e.CartItemId).HasColumnName("CartItem_id");
            entity.Property(e => e.AddedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("Added_at");
            entity.Property(e => e.CartId).HasColumnName("Cart_id");
            entity.Property(e => e.ProductId).HasColumnName("Product_id");
            entity.Property(e => e.Quantity).HasDefaultValue(1);

            entity.HasOne(d => d.Cart).WithMany(p => p.CartItems)
                .HasForeignKey(d => d.CartId)
                .HasConstraintName("FK_CartItems_Carts");

            entity.HasOne(d => d.Product).WithMany(p => p.CartItems)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("FK_CartItems_Products");
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.CategoryId).HasName("PK__Categori__6DB2813624D05D52");

            entity.HasIndex(e => e.DisplayOrder, "IX_Categories_order");

            entity.HasIndex(e => e.CategoryStatus, "IX_Categories_status");

            entity.HasIndex(e => e.CategoryName, "UQ__Categori__871F4343AAE6D792").IsUnique();

            entity.Property(e => e.CategoryId).HasColumnName("Category_id");
            entity.Property(e => e.CategoryDescription).HasColumnName("Category_description");
            entity.Property(e => e.CategoryIcon)
                .HasMaxLength(50)
                .HasDefaultValue("fa-gem")
                .HasColumnName("Category_icon");
            entity.Property(e => e.CategoryName)
                .HasMaxLength(100)
                .HasColumnName("Category_name");
            entity.Property(e => e.CategoryStatus).HasColumnName("Category_status");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("Created_at");
            entity.Property(e => e.DisplayOrder).HasColumnName("Display_order");
        });

        modelBuilder.Entity<Coupon>(entity =>
        {
            entity.HasKey(e => e.CouponId).HasName("PK__Coupons__2A61A4F40EFB4779");

            entity.HasIndex(e => e.CouponCode, "IX_Coupons_code");

            entity.HasIndex(e => e.CouponStatus, "IX_Coupons_status");

            entity.HasIndex(e => e.CouponCode, "UQ__Coupons__4E97936C3C5D5062").IsUnique();

            entity.Property(e => e.CouponId).HasColumnName("Coupon_id");
            entity.Property(e => e.CouponCode)
                .HasMaxLength(50)
                .HasColumnName("Coupon_code");
            entity.Property(e => e.CouponStatus).HasColumnName("Coupon_status");
            entity.Property(e => e.CouponType).HasColumnName("Coupon_type");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("Created_at");
            entity.Property(e => e.DiscountValue)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("Discount_value");
            entity.Property(e => e.MaxDiscount)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("Max_discount");
            entity.Property(e => e.MinOrderAmount)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("Min_order_amount");
            entity.Property(e => e.UsageLimit).HasColumnName("Usage_limit");
            entity.Property(e => e.UsedCount).HasColumnName("Used_count");
            entity.Property(e => e.ValidFrom)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("Valid_from");
            entity.Property(e => e.ValidUntil)
                .HasPrecision(3)
                .HasColumnName("Valid_until");
        });

        modelBuilder.Entity<Favorite>(entity =>
        {
            entity.HasKey(e => e.FavoriteId).HasName("PK__Favorite__7490A9AF80C67431");

            entity.HasIndex(e => e.ProductId, "IX_Favorites_product");

            entity.HasIndex(e => e.UserId, "IX_Favorites_user");

            entity.HasIndex(e => new { e.UserId, e.ProductId }, "UQ_Favorite_User_Product").IsUnique();

            entity.Property(e => e.FavoriteId).HasColumnName("Favorite_id");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("Created_at");
            entity.Property(e => e.ProductId).HasColumnName("Product_id");
            entity.Property(e => e.UserId).HasColumnName("User_id");

            entity.HasOne(d => d.Product).WithMany(p => p.Favorites)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("FK_Favorites_Products");

            entity.HasOne(d => d.User).WithMany(p => p.Favorites)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_Favorites_Users");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.OrderId).HasName("PK__Orders__F1FF84533A9BF9E9");

            entity.HasIndex(e => e.OrderDate, "IX_Orders_date");

            entity.HasIndex(e => e.TrackingNumber, "IX_Orders_number");

            entity.HasIndex(e => e.OrderStatus, "IX_Orders_status");

            entity.HasIndex(e => e.UserId, "IX_Orders_user");

            entity.HasIndex(e => e.TrackingNumber, "UQ__Orders__018FD14125040951").IsUnique();

            entity.Property(e => e.OrderId).HasColumnName("Order_id");
            entity.Property(e => e.BillingAddressId).HasColumnName("Billing_address_id");
            entity.Property(e => e.CouponId).HasColumnName("Coupon_id");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("Created_at");
            entity.Property(e => e.DiscountAmount)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("Discount_amount");
            entity.Property(e => e.OrderDate)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("Order_date");
            entity.Property(e => e.TrackingNumber)
                .HasMaxLength(50)
                .HasColumnName("Order_number");
            entity.Property(e => e.OrderStatus).HasColumnName("Order_status");
            entity.Property(e => e.PaymentMethod)
                .HasMaxLength(50)
                .HasColumnName("Payment_method");
            entity.Property(e => e.PaymentStatus).HasColumnName("Payment_status");
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.ShippingAddressId).HasColumnName("Shipping_address_id");
            entity.Property(e => e.TotalAmount)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("Total_amount");
            entity.Property(e => e.UserId).HasColumnName("User_id");

            entity.HasOne(d => d.Coupon).WithMany(p => p.Orders)
                .HasForeignKey(d => d.CouponId)
                .HasConstraintName("FK_Orders_Coupons");

            entity.HasOne(d => d.User).WithMany(p => p.Orders)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_Orders_Users");
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(e => e.OrderItemId).HasName("PK__OrderIte__2F31262A692D7EE8");

            entity.HasIndex(e => e.OrderId, "IX_OrderItems_order");

            entity.HasIndex(e => e.ProductId, "IX_OrderItems_product");

            entity.Property(e => e.OrderItemId).HasColumnName("OrderItem_id");
            entity.Property(e => e.DiscountPrice)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("Discount_price");
            entity.Property(e => e.OrderId).HasColumnName("Order_id");
            entity.Property(e => e.ProductId).HasColumnName("Product_id");
            entity.Property(e => e.Quantity).HasDefaultValue(1);
            entity.Property(e => e.Subtotal).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.UnitPrice)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("Unit_price");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("FK_OrderItems_Orders");

            entity.HasOne(d => d.Product).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrderItems_Products");
        });

        modelBuilder.Entity<OrderStatusHistory>(entity =>
        {
            entity.HasKey(e => e.HistoryId).HasName("PK__OrderSta__A145B1FF42BD806E");

            entity.ToTable("OrderStatusHistory");

            entity.HasIndex(e => e.OrderId, "IX_OrderStatusHistory_order");

            entity.Property(e => e.HistoryId).HasColumnName("History_id");
            entity.Property(e => e.ChangedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("Changed_at");
            entity.Property(e => e.ChangedBy).HasColumnName("Changed_by");
            entity.Property(e => e.NewStatus).HasColumnName("New_status");
            entity.Property(e => e.OldStatus).HasColumnName("Old_status");
            entity.Property(e => e.OrderId).HasColumnName("Order_id");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderStatusHistories)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("FK_OrderStatusHistory_Orders");
        });

        modelBuilder.Entity<OrderSummary>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("OrderSummary");

            entity.Property(e => e.ItemCount).HasColumnName("Item_count");
            entity.Property(e => e.OrderDate)
                .HasPrecision(3)
                .HasColumnName("Order_date");
            entity.Property(e => e.OrderId).HasColumnName("Order_id");
            entity.Property(e => e.OrderNumber)
                .HasMaxLength(50)
                .HasColumnName("Order_number");
            entity.Property(e => e.OrderStatus).HasColumnName("Order_status");
            entity.Property(e => e.TotalAmount)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("Total_amount");
            entity.Property(e => e.UserEmail)
                .HasMaxLength(150)
                .HasColumnName("User_email");
            entity.Property(e => e.UserName)
                .HasMaxLength(100)
                .HasColumnName("User_name");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.ProductId).HasName("PK__Products__9833FF92BB08F366");

            entity.ToTable(tb => tb.HasTrigger("TRG_Products_UpdateTimestamp"));

            entity.HasIndex(e => e.CategoryId, "IX_Products_category");

            entity.HasIndex(e => e.ProductPrice, "IX_Products_price");

            entity.HasIndex(e => e.ProductStatus, "IX_Products_status");

            entity.HasIndex(e => e.ProductStock, "IX_Products_stock");

            entity.Property(e => e.ProductId).HasColumnName("Product_id");
            entity.Property(e => e.CategoryId).HasColumnName("Category_id");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("Created_at");
            entity.Property(e => e.ProductDescription).HasColumnName("Product_description");
            entity.Property(e => e.ProductDiscountPrice)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("Product_discount_price");
            entity.Property(e => e.ProductImage)
                .HasMaxLength(255)
                .HasColumnName("Product_image");
            entity.Property(e => e.ProductName)
                .HasMaxLength(200)
                .HasColumnName("Product_name");
            entity.Property(e => e.ProductPrice)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("Product_price");
            entity.Property(e => e.ProductStatus).HasColumnName("Product_status");
            entity.Property(e => e.ProductStock).HasColumnName("Product_stock");
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("Updated_at");
            entity.Property(e => e.ViewCount).HasColumnName("View_count");

            entity.HasOne(d => d.Category).WithMany(p => p.Products)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_Products_Categories");
        });

        modelBuilder.Entity<ProductImage>(entity =>
        {
            entity.HasKey(e => e.ImageId).HasName("PK__ProductI__3CAC591127F61521");

            entity.HasIndex(e => e.ImageOrder, "IX_ProductImages_order");

            entity.HasIndex(e => e.ProductId, "IX_ProductImages_product");

            entity.Property(e => e.ImageId).HasColumnName("Image_id");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("Created_at");
            entity.Property(e => e.ImageOrder).HasColumnName("Image_order");
            entity.Property(e => e.ImageUrl)
                .HasMaxLength(255)
                .HasColumnName("Image_url");
            entity.Property(e => e.IsPrimary).HasColumnName("Is_primary");
            entity.Property(e => e.ProductId).HasColumnName("Product_id");

            entity.HasOne(d => d.Product).WithMany(p => p.ProductImages)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("FK_ProductImages_Products");
        });

        modelBuilder.Entity<ProductStockStatus>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("ProductStockStatus");

            entity.Property(e => e.CategoryName)
                .HasMaxLength(100)
                .HasColumnName("Category_name");
            entity.Property(e => e.ProductId).HasColumnName("Product_id");
            entity.Property(e => e.ProductName)
                .HasMaxLength(200)
                .HasColumnName("Product_name");
            entity.Property(e => e.ProductPrice)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("Product_price");
            entity.Property(e => e.ProductStock).HasColumnName("Product_stock");
            entity.Property(e => e.StockStatus)
                .HasMaxLength(10)
                .HasColumnName("Stock_status");
        });

        modelBuilder.Entity<Receipt>(entity =>
        {
            entity.HasKey(e => e.ReceiptId).HasName("PK__Receipts__38B69B8436120C12");

            entity.HasIndex(e => e.ReceiptNumber, "IX_Receipts_number");

            entity.HasIndex(e => e.OrderId, "IX_Receipts_order");

            entity.HasIndex(e => e.ReceiptNumber, "UQ__Receipts__833C62A28B9CFEB8").IsUnique();

            entity.Property(e => e.ReceiptId).HasColumnName("Receipt_id");
            entity.Property(e => e.OrderId).HasColumnName("Order_id");
            entity.Property(e => e.ReceiptDate)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("Receipt_date");
            entity.Property(e => e.ReceiptNumber)
                .HasMaxLength(50)
                .HasColumnName("Receipt_number");
            entity.Property(e => e.ReceiptStatus)
                .HasDefaultValue((byte)1)
                .HasColumnName("Receipt_status");
            entity.Property(e => e.ReceiptType).HasColumnName("Receipt_type");
            entity.Property(e => e.TaxAmount)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("Tax_amount");
            entity.Property(e => e.TotalAmount)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("Total_amount");

            entity.HasOne(d => d.Order).WithMany(p => p.Receipts)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("FK_Receipts_Orders");
        });

        modelBuilder.Entity<ReceiptItem>(entity =>
        {
            entity.HasKey(e => e.ReceiptItemId).HasName("PK__ReceiptI__AFAB2AE15C8627A9");

            entity.HasIndex(e => e.ReceiptId, "IX_ReceiptItems_receipt");

            entity.Property(e => e.ReceiptItemId).HasColumnName("ReceiptItem_id");
            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.ReceiptId).HasColumnName("Receipt_id");
            entity.Property(e => e.TotalAmount)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("Total_amount");
            entity.Property(e => e.UnitPrice)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("Unit_price");

            entity.HasOne(d => d.Receipt).WithMany(p => p.ReceiptItems)
                .HasForeignKey(d => d.ReceiptId)
                .HasConstraintName("FK_ReceiptItems_Receipts");
        });

        modelBuilder.Entity<Review>(entity =>
        {
            entity.HasKey(e => e.ReviewId).HasName("PK__Reviews__F803F2C35F9E8D55");

            entity.ToTable(tb => tb.HasTrigger("TRG_Reviews_UpdateTimestamp"));

            entity.HasIndex(e => e.ProductId, "IX_Reviews_product");

            entity.HasIndex(e => e.Rating, "IX_Reviews_rating");

            entity.HasIndex(e => e.UserId, "IX_Reviews_user");

            entity.Property(e => e.ReviewId).HasColumnName("Review_id");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("Created_at");
            entity.Property(e => e.ProductId).HasColumnName("Product_id");
            entity.Property(e => e.ReviewStatus).HasColumnName("Review_status");
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("Updated_at");
            entity.Property(e => e.UserId).HasColumnName("User_id");

            entity.HasOne(d => d.Product).WithMany(p => p.Reviews)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("FK_Reviews_Products");

            entity.HasOne(d => d.User).WithMany(p => p.Reviews)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_Reviews_Users");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__206A9DF8FAA8CC35");

            entity.ToTable(tb => tb.HasTrigger("TRG_Users_UpdateTimestamp"));

            entity.HasIndex(e => e.UserEmail, "IX_Users_email");

            entity.HasIndex(e => e.UserStatus, "IX_Users_status");

            entity.HasIndex(e => e.UserEmail, "UQ__Users__EB5FD3461C2520DA").IsUnique();

            entity.Property(e => e.UserId).HasColumnName("User_id");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("Created_at");
            entity.Property(e => e.LastLogin)
                .HasPrecision(3)
                .HasColumnName("Last_login");
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("Updated_at");
            entity.Property(e => e.UserEmail)
                .HasMaxLength(150)
                .HasColumnName("User_email");
            entity.Property(e => e.UserName)
                .HasMaxLength(100)
                .HasColumnName("User_name");
            entity.Property(e => e.UserPassword)
                .HasMaxLength(255)
                .HasColumnName("User_password");
            entity.Property(e => e.UserPhone)
                .HasMaxLength(20)
                .HasColumnName("User_phone");
            entity.Property(e => e.UserRole).HasColumnName("User_role");
            entity.Property(e => e.UserStatus).HasColumnName("User_status");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
