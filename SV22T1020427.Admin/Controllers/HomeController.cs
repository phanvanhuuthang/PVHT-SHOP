using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020427.Admin.Models;
using SV22T1020427.BusinessLayers;
using SV22T1020427.Models.Catalog;
using SV22T1020427.Models.Common;
using SV22T1020427.Models.Sales;


namespace SV22T1020427.Admin.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Hiển thị thông tin dashboard của hệ thống
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> Index()
        {
            int year = DateTime.Today.Year;

            var orderCountTask = SalesDataService.ListOrdersAsync(new OrderSearchInput
            {
                Page = 1,
                PageSize = 1,
                SearchValue = "",
                Status = OrderStatusEnum.All,
                DateFrom = null,
                DateTo = null
            });

            var customerCountTask = PartnerDataService.ListCustomersAsync(new PaginationSearchInput
            {
                Page = 1,
                PageSize = 1,
                SearchValue = ""
            });

            var productCountTask = CatalogDataService.ListProductsAsync(new ProductSearchInput
            {
                Page = 1,
                PageSize = 1,
                SearchValue = "",
                CategoryID = 0,
                SupplierID = 0,
                MinPrice = 0,
                MaxPrice = 0
            });

            var todayRevenueTask = SalesDataService.GetTodayRevenueAsync();
            var monthlyRevenueTask = SalesDataService.GetMonthlyRevenueAsync(year);
            var topProductsTask = SalesDataService.GetTopSellingProductsAsync(5);
            var processingOrdersTask = SalesDataService.ListProcessingOrdersAsync(10);

            await Task.WhenAll(
                orderCountTask,
                customerCountTask,
                productCountTask,
                todayRevenueTask,
                monthlyRevenueTask,
                topProductsTask,
                processingOrdersTask);

            var model = new DashboardViewModel
            {
                RevenueYear = year,
                TodayRevenue = todayRevenueTask.Result,
                TotalOrders = orderCountTask.Result.RowCount,
                TotalCustomers = customerCountTask.Result.RowCount,
                TotalProducts = productCountTask.Result.RowCount,
                MonthlyRevenue = monthlyRevenueTask.Result,
                TopSellingProducts = topProductsTask.Result,
                ProcessingOrders = processingOrdersTask.Result
            };

            return View(model);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
