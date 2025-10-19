using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectApi.Data;
using ProjectApi.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ProjectApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DashboardController : ControllerBase
    {
        private readonly FurnitureDbContext _context;

        public DashboardController(FurnitureDbContext context)
        {
            _context = context;
        }

        // 📌 1️⃣ Ghi log truy cập mỗi khi có request từ frontend
        [HttpPost("visit")]
        public async Task<IActionResult> LogVisit()
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var ua = Request.Headers["User-Agent"].ToString();
            var path = HttpContext.Request.Headers["Referer"].ToString(); // URL người dùng đang truy cập

            // ✅ Chỉ log nếu người dùng vào trang chủ (ví dụ: https://domain/ hoặc http://localhost:5173/)
            if (!string.IsNullOrEmpty(path) && !path.EndsWith("/"))
            {
                return Ok(new { message = "Not homepage — skip log" });
            }

            var tenMinutesAgo = DateTime.UtcNow.AddMinutes(-10);

            // ✅ Kiểm tra có bản ghi cùng IP trong 10 phút qua chưa
            var recentVisit = await _context.VisitorLogs
                .Where(v => v.IpAddress == ip && v.VisitTime >= tenMinutesAgo)
                .FirstOrDefaultAsync();

            if (recentVisit != null)
            {
                return Ok(new { message = "Duplicate visit ignored" });
            }

            // ✅ Ghi mới
            var visit = new VisitorLog
            {
                IpAddress = ip,
                UserAgent = ua,
                VisitTime = DateTime.UtcNow
            };

            _context.VisitorLogs.Add(visit);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Visit logged" });
        }


        // 📌 2️⃣ Tổng quan Dashboard
        [HttpGet("overview")]
        public async Task<IActionResult> GetOverview()
        {
            var today = DateTime.UtcNow.Date;
            var sevenDaysAgo = today.AddDays(-6);

            var totalOrders = await _context.Orders.CountAsync();
            var todayOrders = await _context.Orders.CountAsync(o => o.OrderDate.Date == today);

            var totalRevenue = await _context.Orders
                .Where(o => o.Status == "Delivered" || o.Status == "Confirmed")
                .SumAsync(o => (decimal?)o.Total) ?? 0;

            var todayRevenue = await _context.Orders
                .Where(o => (o.Status == "Delivered" || o.Status == "Confirmed") && o.OrderDate.Date == today)
                .SumAsync(o => (decimal?)o.Total) ?? 0;

            var totalUsers = await _context.Users.CountAsync();
            var totalVisits = await _context.VisitorLogs.CountAsync();
            var todayVisits = await _context.VisitorLogs.CountAsync(v => v.VisitTime.Date == today);

            // 🆕 Tổng lượt truy cập 7 ngày gần nhất
            var last7DaysVisits = await _context.VisitorLogs
                .CountAsync(v => v.VisitTime.Date >= sevenDaysAgo && v.VisitTime.Date <= today);

            return Ok(new
            {
                totalOrders,
                todayOrders,
                totalRevenue,
                todayRevenue,
                totalUsers,
                totalVisits,
                todayVisits,
                last7DaysVisits // 🆕 thêm vào đây
            });
        }


        [HttpGet("revenue-chart")]
        public async Task<IActionResult> GetRevenueChart()
        {
            var now = DateTime.UtcNow.Date;

            // Group theo ngày (chưa ToString)
            var rawData = await _context.Orders
                .Where(o => o.OrderDate >= now.AddDays(-6))
                .GroupBy(o => o.OrderDate.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Revenue = g.Sum(x => x.Total)
                })
                .ToListAsync();

            // Format lại sau khi EF đã lấy ra (chạy trong bộ nhớ)
            var result = rawData
                .Select(g => new
                {
                    Date = g.Date.ToString("yyyy-MM-dd"),
                    g.Revenue
                })
                .OrderBy(x => x.Date)
                .ToList();

            return Ok(result);
        }


        // 📈 4️⃣ Biểu đồ lượt truy cập 7 ngày
        [HttpGet("visit-chart")]
        public async Task<IActionResult> GetVisitChart()
        {
            var now = DateTime.UtcNow.Date;

            var rawData = await _context.VisitorLogs
                .Where(v => v.VisitTime >= now.AddDays(-6))
                .GroupBy(v => v.VisitTime.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();

            var result = rawData
                .Select(g => new
                {
                    Date = g.Date.ToString("yyyy-MM-dd"),
                    g.Count
                })
                .OrderBy(x => x.Date)
                .ToList();

            return Ok(result);
        }

    }
}
