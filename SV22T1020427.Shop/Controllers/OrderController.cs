using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020427.BusinessLayers;
using SV22T1020427.Models.Common;
using SV22T1020427.Models.Sales;

namespace SV22T1020427.Shop.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {
        public async Task<IActionResult> Index(int page = 1, OrderStatusEnum status = OrderStatusEnum.All)
        {
            var userData = User.GetUserData();
            if (userData == null || !int.TryParse(userData.UserId, out int customerId))
                return RedirectToAction("Login", "Account");

            var input = new OrderSearchInput
            {
                Page = page,
                PageSize = ApplicationContext.PageSize,
                Status = status
            };

            var allOrders = await SalesDataService.ListOrdersAsync(input);

            // Filter by customer
            var customerOrders = allOrders.DataItems
                .Where(o => o.CustomerID == customerId)
                .ToList();

            ViewBag.Status = status;
            ViewBag.Page = page;

            return View(customerOrders);
        }

        public async Task<IActionResult> Details(int id)
        {
            var userData = User.GetUserData();
            if (userData == null || !int.TryParse(userData.UserId, out int customerId))
                return RedirectToAction("Login", "Account");

            var order = await SalesDataService.GetOrderAsync(id);
            if (order == null || order.CustomerID != customerId)
                return RedirectToAction("Index");

            var details = await SalesDataService.ListDetailsAsync(id);
            ViewBag.Details = details;

            return View(order);
        }
    }
}
