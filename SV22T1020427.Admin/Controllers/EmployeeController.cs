using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020427.BusinessLayers;
using SV22T1020427.Models.Common;
using SV22T1020427.Models.HR;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SV22T1020427.Admin.Controllers
{
    [Authorize(Roles = $"{WebUserRoles.Administrator}")]
    public class EmployeeController : Controller
    {
        private readonly ILogger<EmployeeController> _logger;
        // Dạng chuẩn của email và số điện thoại để kiểm tra tính hợp lệ của dữ liệu nhập vào
        private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static readonly Regex PhoneRegex = new(@"^(?:\+84|0)\d{9}$", RegexOptions.Compiled);


        public EmployeeController(ILogger<EmployeeController> logger)
        {
            _logger = logger;
        }
        /// <summary>
        /// Tìm kiếm và hiển thị danh sách nhân viên
        /// </summary>
        /// <returns></returns>
        private const int PAGESIZE = 20;
        private const string EMPLOYEESEARCHINPUT = "EmployeeSearchInput";
        public IActionResult Index()
        {
            var input = ApplicationContext.GetSessionData<PaginationSearchInput>(EMPLOYEESEARCHINPUT);
            if (input == null) input = new PaginationSearchInput
            {
                Page = 1,
                PageSize = PAGESIZE,
                SearchValue = ""
            };

            return View(input);
        }
        /// <summary>
        ///  Tìm kiếm nhân viên và trả về kết quả dưới dạng phân trang
        /// </summary>
        /// <param name="input">Đầu vào tìm kiếm</param>
        /// <returns></returns>
        public async Task<IActionResult> Search(PaginationSearchInput input)
        {
       //     await Task.Delay(1000);
            var result = await HRDataService.ListEmployeesAsync(input);
            ApplicationContext.SetSessionData(EMPLOYEESEARCHINPUT, input);
            return View(result);
        }
        /// <summary>
        /// Bổ sung nhân viên mới
        /// </summary>
        /// <returns></returns>
        public IActionResult Create()
        {
            ViewBag.Title = "Bổ sung nhân viên";
            var model = new Employee()
            {
                EmployeeID = 0,
                IsWorking = true
            };
            return View("Edit", model);
        }
        /// <summary>
        /// Cập nhật thông tin nhân viên
        /// </summary>
        /// <param name="id">Mã nhân viên cần cập nhật thông tin</param>
        /// <returns></returns>
        public async Task<IActionResult> Edit(int id)
        {
            ViewBag.Title = "Cập nhật thông tin nhân viên";
            var model = await HRDataService.GetEmployeeAsync(id);
            if (model == null)
                return RedirectToAction("Index");

            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> SaveData(Employee data, IFormFile? uploadPhoto)
        {
            try
            {
                ViewBag.Title = data.EmployeeID == 0 ? "Bổ sung nhân viên" : "Cập nhật thông tin nhân viên";

                //Kiểm tra dữ liệu đầu vào: FullName và Email là bắt buộc, Email chưa được sử dụng bởi nhân viên khác
                if (string.IsNullOrWhiteSpace(data.FullName))
                    ModelState.AddModelError(nameof(data.FullName), "Vui lòng nhập họ tên nhân viên");

                if (string.IsNullOrWhiteSpace(data.Email))
                    ModelState.AddModelError(nameof(data.Email), "Vui lòng nhập email nhân viên");
                else if (!await HRDataService.ValidateEmployeeEmailAsync(data.Email, data.EmployeeID))
                    ModelState.AddModelError(nameof(data.Email), "Email đã được sử dụng bởi nhân viên khác");
                else if (!EmailRegex.IsMatch(data.Email))
                    ModelState.AddModelError(nameof(data.Email), "Email không đúng định dạng");
                if (!string.IsNullOrEmpty(data.Phone) && !PhoneRegex.IsMatch(data.Phone))
                    ModelState.AddModelError(nameof(data.Phone), "Số điện thoại không đúng định dạng");
                if (!ModelState.IsValid)
                    return View("Edit", data);

                //Xử lý upload ảnh
                if (uploadPhoto != null)
                {
                    var fileName = $"{Guid.NewGuid()}{Path.GetExtension(uploadPhoto.FileName)}";
                    var filePath = Path.Combine(ApplicationContext.WWWRootPath, "images/employees", fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await uploadPhoto.CopyToAsync(stream);
                    }
                    data.Photo = fileName;
                }

                //Tiền xử lý dữ liệu trước khi lưu vào database
                if (string.IsNullOrEmpty(data.Address)) data.Address = "";
                if (string.IsNullOrEmpty(data.Phone)) data.Phone = "";
                if (string.IsNullOrEmpty(data.Photo)) data.Photo = "nophoto.png";

                //Lưu dữ liệu vào database (bổ sung hoặc cập nhật)
                if (data.EmployeeID == 0)
                {
                    await HRDataService.AddEmployeeAsync(data);
                }
                else
                {
                    await HRDataService.UpdateEmployeeAsync(data);
                }
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                // Ghi log lỗi chi tiết để dễ dàng theo dõi và khắc phục sự cố
                _logger.LogError(ex, "SaveData failed. EmployeeID={EmployeeID}, Message={Message}", data.EmployeeID, ex.Message);
                ModelState.AddModelError(string.Empty, "Hệ thống đang bận hoặc dữ liệu không hợp lệ. Vui lòng kiểm tra dữ liệu hoặc thử lại sau");
                return View("Edit", data);
            }
        }
        /// <summary>
        /// Xóa nhân viên
        /// </summary>
        /// <param name="id">Mã nhân viên cần xóa</param>
        /// <returns></returns>
        public async Task<IActionResult> Delete(int id)
        {
            if (Request.Method == "POST")
            {
                await HRDataService.DeleteEmployeeAsync(id,ApplicationContext.WWWRootPath);
                return RedirectToAction("Index");
            }
            var model = await HRDataService.GetEmployeeAsync(id);
            if (model == null) return RedirectToAction("Index");
            ViewBag.AllowDelete = !await HRDataService.IsUsedEmployeeAsync(id);
            return View(model);

        }
        /// <summary>
        /// Đổi mật khẩu nhân viên
        /// </summary>
        /// <param name="id">Mã nhân viên cần đổi mật khẩu</param>
        /// <returns></returns>

        public async Task<IActionResult> ChangePassword(int id)
        {
            ViewBag.Title = "Mật khẩu nhân viên";
            var model = await HRDataService.GetEmployeeAsync(id);
            if (model == null) return RedirectToAction("Index");
            return View(model);
        }
        
        [HttpPost]

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(int id, string newPassword, string confirmPassword)
        {
            ViewBag.Title = "Mật khẩu nhân viên";

            var model = await HRDataService.GetEmployeeAsync(id);
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
            var success = await SecurityDataService.ChangeEmployeePasswordAsync(model.Email, newHash);

            if (!success)
            {
                ModelState.AddModelError(string.Empty, "Không thể đổi mật khẩu. Vui lòng thử lại.");
                return View(model);
            }

            ViewBag.SuccessMessage = "Đổi mật khẩu thành công";
            return View(model);
        }
        /// <summary>
        /// Giao diện Phân quyền cho nhân viên
        /// </summary>
        /// <param name="id">Mã nhân viên cần phân quyền</param>
        /// <returns></returns>

  
        public async Task<IActionResult> ChangeRole(int id)
        {
            ViewBag.Title = "Phân quyền nhân viên";
            var model = await HRDataService.GetEmployeeAsync(id);
            if (model == null) return RedirectToAction("Index");
            ViewBag.SelectedRoles = await SecurityDataService.GetEmployeeRolesAsync(model.Email);

            ViewBag.RoleOptions = new[]
            {
                new { Value = WebUserRoles.Administrator, Name = "Quản trị hệ thống", Description = "Quản lý toàn bộ hệ thống" },
                new { Value = WebUserRoles.DataManager, Name = "Quản lý dữ liệu", Description = "Mặt hàng, loại hàng, nhà cung cấp, người giao hàng" },
                new { Value = WebUserRoles.Sales, Name = "Quản lý đơn hàng", Description = "Tạo, xử lý và theo dõi đơn hàng" },
                new { Value = WebUserRoles.CustomerManager, Name = "Quản lý khách hàng", Description = "Thêm, sửa, xóa khách hàng" }
            };

            return View(model);
        }

        [HttpPost]
      
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeRole(int id, string[] roles)
        {
            ViewBag.Title = "Phân quyền nhân viên";

            var model = await HRDataService.GetEmployeeAsync(id);
            if (model == null) return RedirectToAction("Index");

            var ok = await SecurityDataService.ChangeEmployeeRoleAsync(model.Email, roles);
            if (!ok)
                ModelState.AddModelError(string.Empty, "Không thể lưu phân quyền.");
            else
                ViewBag.SuccessMessage = "Lưu phân quyền thành công";
            ViewBag.SelectedRoles = await SecurityDataService.GetEmployeeRolesAsync(model.Email);

            ViewBag.RoleOptions = new[]
            {
        new { Value = WebUserRoles.Administrator, Name = "Quản trị hệ thống", Description = "Quản lý toàn bộ hệ thống" },
        new { Value = WebUserRoles.DataManager, Name = "Quản lý dữ liệu", Description = "Mặt hàng, loại hàng, nhà cung cấp, người giao hàng" },
        new { Value = WebUserRoles.Sales, Name = "Quản lý đơn hàng", Description = "Tạo, xử lý và theo dõi đơn hàng" },
        new { Value = WebUserRoles.CustomerManager, Name = "Quản lý khách hàng", Description = "Thêm, sửa, xóa khách hàng" }
    };

            return View(model);
        }

    }
}
