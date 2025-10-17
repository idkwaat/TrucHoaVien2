namespace ProjectApi.Dtos
{
    public class OrderItemRequest
    {
        public int ProductId { get; set; }
        public int? VariantId { get; set; }
        public string? VariantName { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }

}
