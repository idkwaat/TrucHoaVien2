namespace ProjectApi.Dtos
{
    public class OrderRequest
    {
        public string CustomerName { get; set; } = "";
        public string Address { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Email { get; set; } = "";
        public decimal TotalAmount { get; set; }

        public List<OrderItemRequest> Items { get; set; } = new();

    }
}
