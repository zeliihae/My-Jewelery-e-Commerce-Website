namespace JeweleryStore1.Exceptions
{
    /// Kayıt bulunamadığında fırlatılır
   
    public class NotFoundException : Exception
    {
        public NotFoundException(string message) : base(message)
        {
        }

        public NotFoundException(string entityName, object key)
            : base($"{entityName} with key {key} not found.")
        {
        }
    }

   
    /// İş mantığı kuralı ihlal edildiğinde fırlatılır
   
    public class BusinessRuleException : Exception
    {
        public BusinessRuleException(string message) : base(message)
        {
        }
    }

    /// Yetersiz stok durumunda fırlatılır
   
    public class InsufficientStockException : Exception
    {
        public string ProductName { get; }
        public int RequestedQuantity { get; }
        public int AvailableStock { get; }

        public InsufficientStockException(string productName, int requestedQuantity, int availableStock)
            : base($"Yetersiz stok: {productName}. İstenen: {requestedQuantity}, Mevcut: {availableStock}")
        {
            ProductName = productName;
            RequestedQuantity = requestedQuantity;
            AvailableStock = availableStock;
        }
    }

  
    /// Validation hatası için

    public class ValidationException : Exception
    {
        public Dictionary<string, List<string>> Errors { get; }

        public ValidationException(Dictionary<string, List<string>> errors)
            : base("Bir veya daha fazla doğrulama hatası oluştu.")
        {
            Errors = errors;
        }

        public ValidationException(string field, string error)
            : base("Doğrulama hatası oluştu.")
        {
            Errors = new Dictionary<string, List<string>>
            {
                { field, new List<string> { error } }
            };
        }
    }

 
    /// Ödeme işlemi başarısız olduğunda fırlatılır
 
    public class PaymentException : Exception
    {
        public PaymentException(string message) : base(message)
        {
        }
    }

    
    /// Kupon geçersiz veya kullanılamaz durumda
   
    public class InvalidCouponException : Exception
    {
        public InvalidCouponException(string message) : base(message)
        {
        }
    }

  
    /// Kullanıcı yetkisi yok
   
    public class ForbiddenException : Exception
    {
        public ForbiddenException(string message) : base(message)
        {
        }

        public ForbiddenException() : base("Bu işlem için yetkiniz yok.")
        {
        }
    }
}