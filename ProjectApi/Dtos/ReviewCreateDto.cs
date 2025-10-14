namespace ProjectApi.Dtos
{
    public class ReviewCreateDto
    {
        public int ProductId { get; set; }
        public int Rating { get; set; } // 1..5
        public string Comment { get; set; } = "";
    }
}
