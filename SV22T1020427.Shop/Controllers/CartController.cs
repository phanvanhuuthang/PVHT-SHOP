using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020427.BusinessLayers;
using SV22T1020427.Models.Sales;

namespace SV22T1020427.Shop.Controllers
{
    public class CartController : Controller
    {
        public IActionResult Index()
        {
            var cart = ShoppingCartHelper.GetShoppingCart();
            return View(cart);
        }

        [HttpPost]
        public async Task<IActionResult> AddToCart(int productID, int quantity = 1)
        {
            var product = await CatalogDataService.GetProductAsync(productID);
            if (product == null)
                return Json(new { success = false, message = "Sản phẩm không tồn tại." });

            if (quantity <= 0) quantity = 1;

            var item = new OrderDetailViewInfo
            {
                ProductID = productID,
                ProductName = product.ProductName,
                Unit = product.Unit,
                Photo = product.Photo ?? "nophoto.png",
                Quantity = quantity,
                SalePrice = product.Price
            };

            ShoppingCartHelper.AddItemToCart(item);
            int cartCount = ShoppingCartHelper.GetCartItemCount();

            return Json(new { success = true, cartCount, message = "Đã thêm vào giỏ hàng." });
        }

        [HttpPost]
        public IActionResult UpdateCart(int productID, int quantity, decimal salePrice)
        {
            if (quantity <= 0)
                ShoppingCartHelper.RemoveItemFromCart(productID);
            else
                ShoppingCartHelper.UpdateItemInCart(productID, quantity, salePrice);

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult RemoveFromCart(int productID)
        {
            ShoppingCartHelper.RemoveItemFromCart(productID);
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult ClearCart()
        {
            ShoppingCartHelper.ClearCart();
            return RedirectToAction("Index");
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            var cart = ShoppingCartHelper.GetShoppingCart();
            if (!cart.Any())
                return RedirectToAction("Index");

            var userData = User.GetUserData();
            if (userData == null || !int.TryParse(userData.UserId, out int customerId))
                return RedirectToAction("Login", "Account");

            var customer = await PartnerDataService.GetCustomerAsync(customerId);
            ViewBag.Customer = customer;
            ViewBag.Cart = cart;

            return View(cart);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Checkout(string deliveryProvince, string deliveryAddress)
        {
            var cart = ShoppingCartHelper.GetShoppingCart();
            if (!cart.Any())
                return RedirectToAction("Index");

            var userData = User.GetUserData();
            if (userData == null || !int.TryParse(userData.UserId, out int customerId))
                return RedirectToAction("Login", "Account");

            if (string.IsNullOrWhiteSpace(deliveryProvince) || string.IsNullOrWhiteSpace(deliveryAddress))
            {
                var customer = await PartnerDataService.GetCustomerAsync(customerId);
                ViewBag.Customer = customer;
                ViewBag.Cart = cart;
                ModelState.AddModelError("Error", "Vui lòng nhập đầy đủ địa chỉ giao hàng.");
                return View(cart);
            }

            int orderID = await SalesDataService.AddOrderAsync(customerId, deliveryProvince, deliveryAddress);
            if (orderID <= 0)
            {
                var customer = await PartnerDataService.GetCustomerAsync(customerId);
                ViewBag.Customer = customer;
                ViewBag.Cart = cart;
                ModelState.AddModelError("Error", "Đặt hàng thất bại. Vui lòng thử lại.");
                return View(cart);
            }

            foreach (var item in cart)
            {
                var detail = new OrderDetail
                {
                    OrderID = orderID,
                    ProductID = item.ProductID,
                    Quantity = item.Quantity,
                    SalePrice = item.SalePrice
                };
                await SalesDataService.AddDetailAsync(detail);
            }

            ShoppingCartHelper.ClearCart();
            TempData["SuccessMessage"] = "Đặt hàng thành công! Đơn hàng của bạn đang được xử lý.";
            return RedirectToAction("Details", "Order", new { id = orderID });
        }
    }
}
