namespace JeweleryStore1.DTOs
{
    public class ReviewResponseDto
    {
        public int ReviewId { get; set; }
        public int ProductId { get; set; }
        public string? ProductName { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; }
        public byte Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateReviewDto
    {
        public int ProductId { get; set; }
        public int UserId { get; set; }
        public byte Rating { get; set; }
        public string? Comment { get; set; }
    }

    public class UpdateReviewDto
    {
        public int UserId { get; set; }
        public byte Rating { get; set; }
        public string? Comment { get; set; }
    }

    public class ReviewStatsDto
    {
        public int TotalReviews { get; set; }
        public double AverageRating { get; set; }
        public Dictionary<int, int> RatingDistribution { get; set; }
    }
}