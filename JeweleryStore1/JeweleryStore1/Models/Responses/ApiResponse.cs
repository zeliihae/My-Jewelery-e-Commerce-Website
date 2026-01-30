namespace JeweleryStore1.Models.Responses
{
    /// <summary>
    /// Standart API yanıt modeli - Generic version
    /// </summary>
    /// <typeparam name="T">Dönen data'nın tipi</typeparam>
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public List<string>? Errors { get; set; }
        public DateTime Timestamp { get; set; }

        public ApiResponse()
        {
            Timestamp = DateTime.UtcNow;
        }

        // Başarılı yanıt
        public static ApiResponse<T> SuccessResponse(T data, string? message = null)
        {
            return new ApiResponse<T>
            {
                Success = true,
                Message = message ?? "İşlem başarılı",
                Data = data,
                Errors = null,
                Timestamp = DateTime.UtcNow
            };
        }

        // Başarısız yanıt (tek hata)
        public static ApiResponse<T> ErrorResponse(string errorMessage)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = "İşlem başarısız",
                Data = default,
                Errors = new List<string> { errorMessage },
                Timestamp = DateTime.UtcNow
            };
        }

        // Başarısız yanıt (çoklu hata)
        public static ApiResponse<T> ErrorResponse(List<string> errors, string? message = null)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = message ?? "İşlem başarısız",
                Data = default,
                Errors = errors,
                Timestamp = DateTime.UtcNow
            };
        }

        // Validation hataları için
        public static ApiResponse<T> ValidationErrorResponse(Dictionary<string, List<string>> validationErrors)
        {
            var errors = new List<string>();
            foreach (var error in validationErrors)
            {
                errors.AddRange(error.Value);
            }

            return new ApiResponse<T>
            {
                Success = false,
                Message = "Doğrulama hatası",
                Data = default,
                Errors = errors,
                Timestamp = DateTime.UtcNow
            };
        }
    }

    /// <summary>
    /// Standart API yanıt modeli - Non-generic version (data olmadan)
    /// </summary>
    public class ApiResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public object? Data { get; set; }
        public List<string>? Errors { get; set; }
        public DateTime Timestamp { get; set; }

        public ApiResponse()
        {
            Timestamp = DateTime.UtcNow;
        }

        // Başarılı yanıt (data olmadan)
        public static ApiResponse SuccessResponse(string? message = null)
        {
            return new ApiResponse
            {
                Success = true,
                Message = message ?? "İşlem başarılı",
                Data = null,
                Errors = null,
                Timestamp = DateTime.UtcNow
            };
        }

        // Başarısız yanıt (tek hata)
        public static ApiResponse ErrorResponse(string errorMessage)
        {
            return new ApiResponse
            {
                Success = false,
                Message = "İşlem başarısız",
                Data = null,
                Errors = new List<string> { errorMessage },
                Timestamp = DateTime.UtcNow
            };
        }

        // Başarısız yanıt (çoklu hata)
        public static ApiResponse ErrorResponse(List<string> errors, string? message = null)
        {
            return new ApiResponse
            {
                Success = false,
                Message = message ?? "İşlem başarısız",
                Data = null,
                Errors = errors,
                Timestamp = DateTime.UtcNow
            };
        }
    }
}