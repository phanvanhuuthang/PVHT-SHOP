using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020427.BusinessLayers;
using SV22T1020427.Models.Common;
using SV22T1020427.Models.Partner;
using System.Threading.Tasks;

namespace SV22T1020427.Admin.Controllers
{
    [Authorize(Roles = $"{WebUserRoles.Administrator},{WebUserRoles.Sales},{WebUserRoles.CustomerManager}")]
    public class CustomerController : Controller
    {
        private readonly ILogger<CustomerController> _logger;

        public CustomerController(ILogger<CustomerController> logger)
        {
            _logger = logger;
        }
        /// <summary>
        /// Tên biến session lưu lại điều kiện tìm kiếm khách hàng.
        /// </summary>
        private const string CUSTOMERSEARCHINPUT = "CustomerSearchInput";

        /// <summary>
        /// Giao diện để nhập đầu vào tìm kiếm và hiển thị kết quả tìm kiếm
        /// </summary>
        /// <returns></returns>
        public IActionResult Index()
        {
            var input = ApplicationContext.GetSessionData<PaginationSearchInput>(CUSTOMERSEARCHINPUT);
            if (input == null) 
             input = new PaginationSearchInput()
            {
                Page = 1,
                PageSize = ApplicationContext.PageSize,
                SearchValue = ""
            };
            return View(input);
        }
        /// <summary>
        ///  Tìm kiếm khách hàng và trả về kết quả dưới dạng phân trang
        /// </summary>
        /// <param name="input">Đầu vào tìm kiếm</param>
        /// <returns></returns>
        public async Task<IActionResult> Search(PaginationSearchInput input)
        {
          //  await Task.Delay(1000);
            var result = await PartnerDataService.ListCustomersAsync(input);
            ApplicationContext.SetSessionData(CUSTOMERSEARCHINPUT, input);
            return View(result);
        }
        /// <summary>
        /// Bổ sung thông tin khách hàng mới
        /// </summary>
        /// <returns></returns>
        public IActionResult Create()
        {
            ViewBag.Title = "Bổ sung khách hàng";
            var model = new Customer()
            {
                CustomerID = 0
            };
            return View("Edit",model);
        }
        /// <summary>
        /// Cập nhật thông tin khách hàng
        /// </summary>
        /// <param name="id">Mã khach hàng cần cập nhật</param>
        /// <returns></returns>
        public async Task<IActionResult> Edit(int id)
        {
            ViewBag.Title = "Cập nhật thông tin khách hàng";
            var model = await PartnerDataService.GetCustomerAsync(id);
            if (model == null) return RedirectToAction("Index");
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> SaveData(Customer data)
        {
            try
            {
                ViewBag.Title = data.CustomerID == 0 ? "Bổ sung khách hàng" : "Cập nhật thông tin khách hàng";

          
                //Sử dụng ModelState để kiểm soát thông báo lỗi và gửi thông báo lỗi cho view
                if (string.IsNullOrWhiteSpace(data.CustomerName))
                    ModelState.AddModelError(nameof(data.CustomerName), "Vui lòng nhập tên khách hàng");
                if (string.IsNullOrWhiteSpace(data.Email))
                    ModelState.AddModelError(nameof(data.Email), "Vui lòng cho biết email của của khách hàng");
                else if (!(await PartnerDataService.ValidatelCustomerEmailAsync(data.Email, data.CustomerID)))
                    ModelState.AddModelError(nameof(data.Email), "Email này đã có người sử dụng");

                if (string.IsNullOrWhiteSpace(data.Province))
                    ModelState.AddModelError(nameof(data.Province), "Vui lòng chọn tỉnh/thành");
                //Điều chỉnh lại các giá trị dữ liệu khác theo qui định/ qui ước của APP
                if (string.IsNullOrWhiteSpace(data.ContactName)) data.ContactName = "";
                if (string.IsNullOrWhiteSpace(data.Phone)) data.Phone = "";
                if (string.IsNullOrWhiteSpace(data.Address)) data.Address = "";

                if (!ModelState.IsValid)
                {
                    return View("Edit", data);
                }
                //Yêu cầu lưu dữ liệu vào CSDL
                if (data.CustomerID == 0)
                {
                    await PartnerDataService.AddCustomerAsync(data);
                }
                else
                {
                    await PartnerDataService.UpdateCustomerAsync(data);
                }
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SaveData failed. CustomerID={CustomerID}, Message={Message}", data.CustomerID, ex.Message);
                ModelState.AddModelError(string.Empty, "Hệ thống đang bận, vui lòng thử lại sau");
                return View("Edit", data);
            }

        }
        /// <summary>
        /// Xóa khách hàng
        /// </summary>
        /// <param name="id">Mã khách hàng cần xóa</param>
        /// <returns></returns>
        public async Task<IActionResult> Delete(int id)
        {
            if (Request.Method == "POST")
            {
                await PartnerDataService.DeleteCustomerAsync(id);
                return RedirectToAction("Index");
            }
            var model = await PartnerDataService.GetCustomerAsync(id);
            if (model == null) return RedirectToAction("Index");

            ViewBag.AllowDelete = !await PartnerDataService.IsUsedCustomerAsync(id);
            return View(model);
        }
        /// <summary>
        /// Đổi mật khẩu khách hàng
        /// </summary>
        /// <param name="id">Mã nhân viên cần đổi mật khẩu</param>
        /// <returns></returns>
       [Authorize(Roles = $"{WebUserRoles.Administrator}")]
        public async Task<IActionResult> ChangePassword(int id)
        {
            ViewBag.Title = "Mật khẩu khách hàng";
            var model = await PartnerDataService.GetCustomerAsync(id);
            if (model == null) return RedirectToAction("Index");
            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = $"{WebUserRoles.Administrator}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(int id, string newPassword, string confirmPassword)
        {
            ViewBag.Title = "Mật khẩu nhân viên";

            var model = await PartnerDataService.GetCustomerAsync(id);
            if (model == null) return RedirectToAction("Index");

            if (string.IsNullOrWhiteSpace(newPassword) || string.IsNullOrWhiteSpace(confirmPassword))
            {
                ModelState.AddModelError(string.Empty, "Vui lòng nhập đầy đủ mật khẩu mới.");
                return View(model);
            }

            if (newPassword != confirmPassword)
            {
                ModelState.AddModelError(string.Empty, "Mật khẩu xác nhận không khớp.");
                return View(model);
            }

            var newHash = CryptHelper.HashMD5(newPassword);
            var success = await SecurityDataService.ChangeCustomerPasswordAsync(model.Email, newHash);

            if (!success)
            {
                ModelState.AddModelError(string.Empty, "Không thể đổi mật khẩu. Vui lòng thử lại.");
                return View(model);
            }

            ViewBag.SuccessMessage = "Đổi mật khẩu thành công";
            return View(model);
        }
    }
}
