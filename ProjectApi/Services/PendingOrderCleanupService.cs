using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProjectApi.Data;

namespace ProjectApi.Services
{
    public class PendingOrderCleanupService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<PendingOrderCleanupService> _logger;

        public PendingOrderCleanupService(IServiceScopeFactory scopeFactory, ILogger<PendingOrderCleanupService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🚀 PendingOrderCleanupService started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CleanupPendingOrders();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "🔥 Lỗi trong quá trình xóa đơn hàng pending quá hạn");
                }

                // Lặp lại sau mỗi 5 phút
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        private async Task CleanupPendingOrders()
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<FurnitureDbContext>();

            var cutoff = DateTime.UtcNow.AddMinutes(-10); // 🔥 Quá 10 phút chưa thanh toán thì xóa
            var staleOrders = await context.Orders
                .Where(o => o.Status == "Pending" && o.OrderDate < cutoff)
                .ToListAsync();

            if (staleOrders.Any())
            {
                context.Orders.RemoveRange(staleOrders);
                await context.SaveChangesAsync();

                _logger.LogInformation($"🗑️ Đã xóa {staleOrders.Count} đơn hàng pending quá hạn ({DateTime.UtcNow}).");
            }
        }
    }
}
