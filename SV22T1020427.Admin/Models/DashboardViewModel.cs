using SV22T1020427.Models.Sales;

namespace SV22T1020427.Admin.Models
{
    /// <summary>
    /// D? li?u hi?n th? trang ch? dashboard.
    /// </summary>
    public class DashboardViewModel
    {
        public decimal TodayRevenue { get; set; }
        public int TotalOrders { get; set; }
        public int TotalCustomers { get; set; }
        public int TotalProducts { get; set; }

        public int RevenueYear { get; set; }

        public List<OrderMonthlyRevenue> MonthlyRevenue { get; set; } = new();
        public List<TopSellingProductInfo> TopSellingProducts { get; set; } = new();
        public List<OrderViewInfo> ProcessingOrders { get; set; } = new();
    }
}
