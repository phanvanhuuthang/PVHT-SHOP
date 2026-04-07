using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020427.BusinessLayers;
using SV22T1020427.Models.Catalog;
using SV22T1020427.Models.Sales;
using System.Threading.Tasks;

namespace SV22T1020427.Admin.Controllers
{
    /// <summary>
    /// Các chức năng liên quan đến nghiệp vụ bán hàng
    /// </summary>
    [Authorize(Roles = $"{WebUserRoles.Sales},{WebUserRoles.Administrator}")]
    public class OrderController : Controller
    {

        /// <summary>
        /// Nhập đầu vào Tìm kiếm đơn hàng và hiển thị kết quả tìm kiếm
        /// </summary>
        /// <returns></returns>
        private const string ORDERSEARCHINPUT = "OrderSearchInput";
        private const string PRODUCTSEARCHINPUT = "ProductSearchInput";
        #region Tìm kiếm đơn hàng
        public IActionResult Index()
        {
            var input = ApplicationContext.GetSessionData<OrderSearchInput>(ORDERSEARCHINPUT);
            if (input == null)
                input = new OrderSearchInput()
                {
                    Page = 1,
                    PageSize = ApplicationContext.PageSize,
                    SearchValue = "",
                    Status = OrderStatusEnum.All,
                    DateFrom = null,
                    DateTo = null
                };

            return View(input);




        }
        /// <summary>
        /// Tìm kiếm và hiển thị danh sách đơn hàng
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> Search(OrderSearchInput input)
        {
            var result = await SalesDataService.ListOrdersAsync(input);
            
            ApplicationContext.SetSessionData(ORDERSEARCHINPUT, input);
            return View(result);
        }
        #endregion
        #region Lập đơn hàng
        /// <summary>
        /// Giao diện thực hiện các chức năng lập đơn hàng mới
        /// </summary>
        /// <returns></returns>
        public IActionResult Create()
        {
            var input = ApplicationContext.GetSessionData<ProductSearchInput>(PRODUCTSEARCHINPUT);
            if (input == null)
                input = new ProductSearchInput()
                {
                    Page = 1,
                    PageSize = 3,
                    SearchValue = "",
                    CategoryID = 0,
                    SupplierID = 0,
                    MinPrice = 0,
                    MaxPrice = 0
                };
            return View(input);
        }
        /// <summary>
        /// Tìm mặt hàng để bán
        /// </summary>
        /// <param name="input">sear</param>
        /// <returns></returns>
        public async Task<IActionResult> SearchProduct(ProductSearchInput input)
        {
           var result = await CatalogDataService.ListProductsAsync(input);
            ApplicationContext.SetSessionData(PRODUCTSEARCHINPUT, input);

            return View(result);
        }
        /// <summary>
        /// Hiển thị giỏ hàng
        /// </summary>
        /// <returns></returns>
        public IActionResult ShowCart()
        {
            var cart = ShoppingCartHelper.GetShoppingCart();
            return View(cart);
        }
        /// <summary>
        /// Thêm hàng vào giỏ hàng
        /// </summary>
        /// <param name="id">Mã của đơn hàng</param>
        /// <returns></returns>
        [HttpPost]
        public IActionResult AddCartItem(int productId = 0, int quantity = 0, decimal price = 0)
        {
            
            
            //ví dụ: Quantity = 0 không hợp lệ, Price < 0 không hợp lệ, productId = 0 không hợp lệ
            var product = CatalogDataService.GetProductAsync(productId).Result;
            if (product == null)
            {
                return Json(new ApiResult(0, "Mặt hàng không tồn tại"));
            }
            if (!product.IsSelling)
            {
                return Json(new ApiResult(0, "Mặt hàng đã ngưng bán"));
            }
            if (quantity <= 0)
            {
                    return Json(new ApiResult(0, "Số lượng phải lớn hơn 0"));
            }
            if (price < 0)
            {
                return Json(new ApiResult(0, "Giá bán không hợp lệ"));
            }
            //Thêm hàng vào giỏ hàng
            var item = new OrderDetailViewInfo()
            {
                ProductID = productId,
                ProductName = product.ProductName,
                Unit = product.Unit,
                Photo = product.Photo ?? "nophoto.png",
                Quantity = quantity,
                SalePrice = price,
            };
            ShoppingCartHelper.AddItemToCart(item);
            return Json(new ApiResult(1,""));
        }
        /// <summary>
        ///View để Cập nhật thông tin của mặt hàng trong giỏ hàng 
        /// </summary>
        /// <param name="productId">Mã mặt hàng cần xử lý</param>
        /// <returns></returns>
        public IActionResult EditCartItem(int productId = 0)
        {
            var item = ShoppingCartHelper.GetCartItem(productId);
            return PartialView(item);
        }
        /// <summary>
        /// Cập nhật thông tin mặt hàng trong giỏ hàng (số lượng, giá bán)
        /// </summary>
        /// <param name="productId"></param>
        /// <param name="quantity"></param>
        /// <param name="salePrice"></param>
        /// <returns></returns>
        [HttpPost]
        public IActionResult UpdateCartItem(int productId, int quantity, decimal salePrice)
        {
            
            if (salePrice<0)
                return Json(new ApiResult(0, "Giá bán không hợp lệ"));
            if (productId == 0)
            {
                return Json(new ApiResult(0, "Mặt hàng không tồn tại"));
            }
            if (quantity <= 0)
            {
                return Json(new ApiResult(0, "Số lượng phải lớn hơn 0"));
            }
            //UPDATE trong giỏ hàng
            ShoppingCartHelper.UpdateItemInCart(productId, quantity, salePrice);
            return Json(new ApiResult(1, ""));
        }

        /// <summary>
        /// Xóa mặt hàng ra khỏi giỏ hàng 
        /// </summary>
        /// <param name="productId">Mã mặt hàng cần xử lý</param>
        /// <returns></returns>
        public IActionResult DeleteCartItem(int productId = 0)
        {
            if (Request.Method == "POST")
            {
                ShoppingCartHelper.RemoveItemFromCart(productId);
                return Json(new ApiResult(1, ""));
            }

            // GET: Hiển thị giao diện để xác nhận
            var item = ShoppingCartHelper.GetCartItem(productId);
            ViewBag.ProductID = productId;
            ViewBag.ProductName = item?.ProductName ?? "(Không xác định)";

            return PartialView(item);
        }
        /// <summary>
        /// Xóa giỏ hàng
        /// </summary>
        /// <returns></returns>
        public IActionResult ClearCart()
        {
            // POST: Xóa giỏ hàng
            if (Request.Method == "POST")
            {
                ShoppingCartHelper.ClearCart();
                return Json(new ApiResult(1, ""));
            }
            // Hiển thị giao diện để xác nhận
            return PartialView();
        }
        /// <summary>
        /// Tạo đơn hàng từ giỏ hàng
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> CreateOrder(int customerID = 0, string province = "", string address = "")
        {
    
            var cart = ShoppingCartHelper.GetShoppingCart();
            if (cart.Count == 0)
            {
                return Json(new ApiResult(0, "Giỏ hàng rỗng"));
            }
            if (customerID == 0)
            {
                return Json(new ApiResult(0, "Khách hàng không hợp lệ"));
            }
            if (string.IsNullOrWhiteSpace(province))
            {
                return Json(new ApiResult(0, "Tỉnh/Thành phố không hợp lệ"));
            }
            if (string.IsNullOrWhiteSpace(address))
            {
                return Json(new ApiResult(0, "Vui lòng nhập địa chỉ giao hàng"));
            }
            //  ghi chi tiết của đơn hàng
            int orderId = await SalesDataService.AddOrderAsync(customerID, province, address);
            if (orderId <= 0) return Json(new ApiResult(0, "Tạo đơn hàng thất bại"));
            foreach (var item in cart)
            {
                var detail = new OrderDetail()
                {
                    OrderID = orderId,
                    ProductID = item.ProductID,
                    Quantity = item.Quantity,
                    SalePrice = item.SalePrice
                };
                await SalesDataService.AddDetailAsync(detail);
            }
            return Json(new ApiResult(orderId, ""));

        }
        #endregion

        #region Xử lý đơn hàng
        /// <summary>
        /// Chi tiết đơn hàng
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> Detail(int id)
        {
            ViewBag.Title = "Chi tiết đơn hàng";

            var model = await SalesDataService.GetOrderAsync(id);
            if (model == null) return RedirectToAction("Index");

            ViewBag.OrderDetails = await SalesDataService.ListDetailsAsync(id);

            return View(model);
        }
        /// <summary>
        ///View để Cập nhật thông tin của mặt hàng trong giỏ hàng 
        /// </summary>
        /// <param name="productId">Mã mặt hàng cần xử lý</param>
        /// <returns></returns>
        public async Task<IActionResult> EditOrderItem(int orderId = 0,int productId = 0)
        {
            var item = await SalesDataService.GetDetailAsync(orderId,productId);
            return PartialView(item); 
        }
        /// <summary>
        /// Cập nhật thông tin mặt hàng trong đơn hàng (số lượng, giá bán)
        /// </summary>
        /// <param name="productId"></param>
        /// <param name="quantity"></param>
        /// <param name="salePrice"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> UpdateOrderItem(int orderId,int productId, int quantity, decimal salePrice)
        {
    
            if (salePrice < 0)
                return Json(new ApiResult(0, "Giá bán không hợp lệ"));
            if (productId == 0)
                return Json(new ApiResult(0, "Mặt hàng không tồn tại"));
            if (quantity <= 0)
                return Json(new ApiResult(0, "Số lượng phải lớn hơn 0"));
            if (orderId == 0)
                return Json(new ApiResult(0, "Đơn hàng không tồn tại"));
            //UPDATE trong giỏ hàng
            var data = new OrderDetail
            {
                OrderID = orderId,
                ProductID = productId,
                Quantity = quantity,
                SalePrice = salePrice
            };
            var result = await SalesDataService.UpdateDetailAsync(data);
            if (!result)
                return Json(new ApiResult(0, "Cập nhật không thành công"));
            return Json(new ApiResult(1, ""));
        }

        /// <summary>
        /// Xóa mặt hàng ra khỏi giỏ hàng 
        /// </summary>
        /// <param name="productId">Mã mặt hàng cần xử lý</param>
        /// <returns></returns>
        public async Task<IActionResult> DeleteOrderItem(int orderId =0,int productId = 0)
        {
            if (Request.Method == "POST")
            {
               var result = await SalesDataService.DeleteDetailAsync(orderId,productId);
                if (!result)
                    return Json(new ApiResult(0, "Xóa không thành công"));
                return Json(new ApiResult(1, ""));
            }
            //GET: Hiển thị giao diện dể xác nhận
            ViewBag.ProductId = productId;
            ViewBag.OrderId = orderId;
            return PartialView();
        }
        /// <summary>
        /// Duyệt chấp nhận đơn hàng
        /// </summary>
        /// <param name="id">Mã đơn hàng cần duyệt</param>
        /// <returns></returns>
        public async Task<IActionResult> Accept(int id)
        {
            if (Request.Method == "POST")
            {
                var userData = User.GetUserData();
                if (userData == null || !int.TryParse(userData.UserId, out var employeeId))
                    return Json(new ApiResult(0, "Không xác định được nhân viên đăng nhập"));

                var ok = await SalesDataService.AcceptOrderAsync(id, employeeId);
                if (!ok)
                    return Json(new ApiResult(0, "Duyệt đơn hàng không thành công"));

                return Json(new ApiResult(1, ""));

            }
            ViewBag.OrderId = id;
            return PartialView();
        }
        /// <summary>
        /// Chuyển hàng cho người giao hàng
        /// </summary>
        /// <param name="id">Mã đơn hàng cần chuyền</param>
        /// <returns></returns>

        [HttpGet]
        public IActionResult Shipping(int id)
        {
            ViewBag.OrderId = id;
            return PartialView();
        }

        [HttpPost]
        public async Task<IActionResult> Shipping(int id, int shipperId)
        {
            var ok = await SalesDataService.ShipOrderAsync(id, shipperId);
            if (!ok)
                return Json(new ApiResult(0, "Chuyển giao hàng không thành công"));

            return Json(new ApiResult(1, ""));
        }
        /// <summary>
        /// Kết thúc thành công công đơn hàng
        /// </summary>
        /// <param name="id">Mã đơn hàng</param>
        /// <returns></returns>
        public async Task<IActionResult> Finish(int id)
        {
            if (Request.Method == "POST")
            {
               
                var ok = await SalesDataService.CompleteOrderAsync(id);
                if (!ok)
                    return Json(new ApiResult(0, "Hoàn thành đơn hàng không thành công"));

                return Json(new ApiResult(1, ""));

            }
            ViewBag.OrderId = id;
            return PartialView();
            
        }
        /// <summary>
        /// Từ chối đơn hàng
        /// </summary>
        /// <param name="id">Mã đơn hàng</param>
        /// <returns></returns>
        public async Task<IActionResult> Reject(int id)
        {
            if (Request.Method == "POST")
            {
                var userData = User.GetUserData();
                if (userData == null || !int.TryParse(userData.UserId, out var employeeId))
                    return Json(new ApiResult(0, "Không xác định được nhân viên đăng nhập"));

                var ok = await SalesDataService.RejectOrderAsync(id, employeeId);
                if (!ok)
                    return Json(new ApiResult(0, "Từ chối đơn hàng không thành công"));

                return Json(new ApiResult(1, ""));
            }
            ViewBag.OrderId = id;
            return PartialView();
        }
        /// <summary>
        /// Hủy đơn hàng
        /// </summary>
        /// <param name="id">Mã đơn hàng</param>
        /// <returns></returns>
        public async Task<IActionResult> Cancel(int id)
        {
            if (Request.Method == "POST")
            {
                var userData = User.GetUserData();
                if (userData == null || !int.TryParse(userData.UserId, out var employeeId))
                    return Json(new ApiResult(0, "Không xác định được nhân viên đăng nhập"));

                var ok = await SalesDataService.CancelOrderAsync(id, employeeId);
                if (!ok)
                    return Json(new ApiResult(0, "Hủy đơn hàng không thành công"));

                return Json(new ApiResult(1, ""));
            }
            ViewBag.OrderId = id;
            return PartialView();
        }
        /// <summary>
        /// Xóa đơn hàng
        /// </summary>
        /// <param name="id">Mã đơn hàng cần xóa</param>
        /// <returns></returns>
        public async Task<IActionResult> Delete(int id)
        {
            if (Request.Method == "POST")
            {
                var order = await SalesDataService.DeleteOrderAsync(id);
                if (!order)
                    return Json(new ApiResult(0, "Xóa không thành công! "));
                return Json(new ApiResult(1, ""));


            }
            ViewBag.OrderId = id;
            return PartialView();
        }
        #endregion
    }
}