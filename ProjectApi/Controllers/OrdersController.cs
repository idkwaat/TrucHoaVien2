using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectApi.Data;
using ProjectApi.Dtos;
using ProjectApi.Models;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text.RegularExpressions;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly FurnitureDbContext _context;

    public OrdersController(FurnitureDbContext context)
    {
        _context = context;
    }

    // ✅ Tạo đơn hàng
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] OrderRequest req)
    {
        Console.WriteLine("📦 Nhận request tạo đơn hàng!");
        if (req == null || req.Items == null || req.Items.Count == 0)
            return BadRequest("Invalid order data");

        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim))
            return Unauthorized("User not authenticated");

        if (!int.TryParse(userIdClaim, out int userId))
            return BadRequest("Invalid user ID format");

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return Unauthorized("User not found");

        // ✅ Tạo đơn hàng mới
        var order = new Order
        {
            CustomerName = req.CustomerName,
            Address = req.Address,
            Phone = req.Phone,
            Email = req.Email,
            Total = req.TotalAmount,
            OrderDate = DateTime.UtcNow,
            Status = "Pending",
            UserId = user.Id
        };

        await _context.Orders.AddAsync(order);
        await _context.SaveChangesAsync();

        // ✅ Thêm chi tiết sản phẩm
        var items = req.Items.Select(item => new OrderItem
        {
            OrderId = order.Id,
            ProductId = item.ProductId,
            Quantity = item.Quantity,
            Price = item.Price
        });
        await _context.OrderItems.AddRangeAsync(items);
        await _context.SaveChangesAsync();

        // ✅ Sinh nội dung chuyển khoản (SePay sẽ match cái này)
        var transferContent = $"DH_{order.Id}";

        // ✅ Sinh QR VietQR
        const string BANK_ID = "970423"; // Mã ngân hàng TPBANK
        const string ACCOUNT_NO = "26266363999";
        const string ACCOUNT_NAME = "PHUNG TO UYEN";

        var qrUrl =
            $"https://img.vietqr.io/image/{BANK_ID}-{ACCOUNT_NO}-qr_only.png?amount={order.Total}&addInfo={transferContent}&accountName={Uri.EscapeDataString(ACCOUNT_NAME)}";

        // ✅ Trả về cho frontend hiển thị
        return Ok(new
        {
            id = order.Id,
            totalAmount = order.Total,
            transferContent,
            qrUrl
        });
    }


    // ✅ Chỉ admin được quyền đổi trạng thái
    [Authorize(Roles = "Admin")]
    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] string status)
    {
        var order = await _context.Orders.FindAsync(id);
        if (order == null)
            return NotFound(new { message = $"Order with id {id} not found" });

        order.Status = status;
        await _context.SaveChangesAsync();

        return Ok(new { message = $"Order {id} status updated to {status}" });
    }

    // ✅ Lấy đơn hàng của chính người dùng (không phá code cũ)
    [Authorize]
    [HttpGet("my-orders")]
    public async Task<IActionResult> GetMyOrders()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out int userId))
            return Unauthorized("Invalid user ID");

        // ✅ Lấy order (không include Items)
        var orders = await _context.Orders
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();

        // ✅ Lấy items riêng (tránh lỗi mapping)
        var orderIds = orders.Select(o => o.Id).ToList();
        var orderItems = await _context.OrderItems
            .Where(i => orderIds.Contains(i.OrderId))
            .Include(i => i.Product)
            .ToListAsync();

        // ✅ Map lại dữ liệu để trả cho frontend
        var result = orders.Select(o => new
        {
            o.Id,
            TotalAmount = o.Total,
            Status = o.Status,
            CreatedAt = o.OrderDate,
            Items = orderItems
                .Where(i => i.OrderId == o.Id)
                .Select(i => new
                {
                    i.ProductId,
                    ProductName = i.Product?.Name,
                    i.Quantity,
                    i.Price,
                    ImageUrl = i.Product?.ImageUrl // ✅
                })

        });

        return Ok(result);
    }

    // ✅ Lấy toàn bộ đơn hàng (Admin)
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> GetAllOrders()
    {
        var orders = await _context.Orders
            .Include(o => o.User)
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();

        var result = orders.Select(o => new
        {
            o.Id,
            o.CustomerName,
            o.Email,
            o.Phone,
            o.Address,
            o.Total,
            o.Status,
            o.OrderDate,
            UserName = o.User != null ? o.User.Username : "(Guest)",
            Items = o.Items.Select(i => new
            {
                i.ProductId,
                ProductName = i.Product != null ? i.Product.Name : "N/A",
                i.Quantity,
                i.Price,
                ImageUrl = i.Product != null ? i.Product.ImageUrl : null // ✅ thêm dòng này
            })

        });

        return Ok(result);
    }

    // ✅ Chi tiết 1 đơn hàng
    [Authorize(Roles = "Admin")]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetOrderById(int id)
    {
        var order = await _context.Orders
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null) return NotFound();

        return Ok(new
        {
            order.Id,
            order.CustomerName,
            order.Email,
            order.Address,
            order.Phone,
            order.Status,
            order.Total,
            order.OrderDate,
            Items = order.Items.Select(i => new
            {
                i.ProductId,
                ProductName = i.Product?.Name ?? "N/A",
                i.Quantity,
                i.Price,
                i.Product?.ImageUrl
            })
        });
    }



}
