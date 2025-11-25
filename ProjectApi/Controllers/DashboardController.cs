using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectApi.Data;
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

        // -------------------------------
        // 📌 FUNCTION: Xử lý range
        // -------------------------------
        private (DateTime From, DateTime To) GetRange(string range)
        {
            var today = DateTime.UtcNow.Date;

            return range switch
            {
                "month" => (today.AddDays(-29), today),
                "year"  => (today.AddYears(-1), today),
                "all"   => (DateTime.UtcNow.AddYears(-10), today),
                _       => (today.AddDays(-6), today) // default = week
            };
        }

        // -------------------------------------
        // 📌 1. VISIT CHART — Lượt truy cập
        // -------------------------------------
        [HttpGet("visit-chart")]
        public async Task<IActionResult> GetVisitChart([FromQuery] string range = "week")
        {
            var (fromDate, toDate) = GetRange(range);

            var raw = await _context.VisitorLogs
                .Where(v => v.VisitTime.Date >= fromDate && v.VisitTime.Date <= toDate)
                .GroupBy(v => v.VisitTime.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();

            var result = raw
                .Select(x => new
                {
                    Date = x.Date.ToString("yyyy-MM-dd"),
                    x.Count
                })
                .OrderBy(x => x.Date)
                .ToList();

            return Ok(result);
        }

        // -------------------------------------
        // 📌 2. REVENUE CHART — Doanh thu
        // -------------------------------------
        [HttpGet("revenue-chart")]
        public async Task<IActionResult> GetRevenueChart([FromQuery] string range = "week")
        {
            var (fromDate, toDate) = GetRange(range);

            var raw = await _context.Orders
                .Where(o => o.OrderDate.Date >= fromDate && o.OrderDate.Date <= toDate)
                .Where(o => o.Status == "Delivered" || o.Status == "Confirmed")
                .GroupBy(o => o.OrderDate.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Revenue = g.Sum(x => x.Total)
                })
                .ToListAsync();

            var result = raw
                .Select(x => new
                {
                    Date = x.Date.ToString("yyyy-MM-dd"),
                    x.Revenue
                })
                .OrderBy(x => x.Date)
                .ToList();

            return Ok(result);
        }

        // -------------------------------------
        // 📌 3. OVERVIEW — Tổng quan dashboard
        // -------------------------------------
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
                .Where(o => (o.Status == "Delivered" || o.Status == "Confirmed") &&
                            o.OrderDate.Date == today)
                .SumAsync(o => (decimal?)o.Total) ?? 0;

            var totalUsers = await _context.Users.CountAsync();

            var totalVisits = await _context.VisitorLogs.CountAsync();
            var todayVisits = await _context.VisitorLogs.CountAsync(v => v.VisitTime.Date == today);

            var last7DaysVisits = await _context.VisitorLogs
                .CountAsync(v => v.VisitTime.Date >= sevenDaysAgo);

            return Ok(new
            {
                totalOrders,
                todayOrders,
                totalRevenue,
                todayRevenue,
                totalUsers,
                totalVisits,
                todayVisits,
                last7DaysVisits
            });
        }

        // -------------------------------------
        // 📌 4. LOG VISIT — Ghi log truy cập
        // -------------------------------------
        [HttpPost("visit")]
        public async Task<IActionResult> LogVisit()
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var ua = Request.Headers["User-Agent"].ToString();
            var referer = Request.Headers["Referer"].ToString();

            if (!referer.EndsWith("/"))
                return Ok(new { message = "Not homepage — skip log" });

            var tenMinAgo = DateTime.UtcNow.AddMinutes(-10);

            var exists = await _context.VisitorLogs
                .AnyAsync(v => v.IpAddress == ip && v.VisitTime >= tenMinAgo);

            if (exists)
                return Ok(new { message = "Duplicate visit ignored" });

            _context.VisitorLogs.Add(new Models.VisitorLog
            {
                IpAddress = ip,
                UserAgent = ua,
                VisitTime = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            return Ok(new { message = "Visit logged" });
        }
    }
}
