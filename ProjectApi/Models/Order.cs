using System;
using System.Collections.Generic;

namespace ProjectApi.Models
{
    public class Order
    {
        public int Id { get; set; }
        public string CustomerName { get; set; } = "";
        public string Address { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Email { get; set; } = "";
        public decimal Total { get; set; }


        public DateTime OrderDate { get; set; }

        // 👇 Trạng thái đơn hàng
        public string Status { get; set; } = "Pending"; // Pending, Confirmed, Shipped, Delivered, Cancelled,...

        // 👇 Liên kết với User (nếu có đăng nhập)
        public int? UserId { get; set; }
        public User? User { get; set; }

        public ICollection<OrderItem>? Items { get; set; }

        public string? PaymentTransactionId { get; set; }
        public decimal? PaymentAmount { get; set; }
        public DateTime? PaidAt { get; set; }
    }
}
