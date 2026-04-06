using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SV22T1020427.BusinessLayers;
using SV22T1020427.Models.Partner;

namespace SV22T1020427.Shop.Controllers
{
    public class AccountController : Controller
    {
        private async Task PrepareProvinceViewDataAsync(string? selectedProvince = null)
        {
            var provinces = await DictionaryDataService.ListProvincesAsync();
            var items = new List<SelectListItem>
            {
                new SelectListItem
                {
                    Value = "",
                    Text = "-- Chọn Tỉnh/Thành phố --",
                    Selected = string.IsNullOrWhiteSpace(selectedProvince)
                }
            };

            foreach (var p in provinces)
            {
                items.Add(new SelectListItem
                {
                    Value = p.ProvinceName,
                    Text = p.ProvinceName,
                    Selected = !string.IsNullOrWhiteSpace(selectedProvince)
                               && p.ProvinceName.Equals(selectedProvince, StringComparison.OrdinalIgnoreCase)
                });
            }

            ViewBag.Provinces = items;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Login(string username, string password)
        {
            ViewBag.Username = username;
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError("Error", "Vui lòng nhập đầy đủ email và mật khẩu.");
                return View();
            }

            string hashedPassword = CryptHelper.HashMD5(password);
            var userAccount = await SecurityDataService.CustomerAuthorizeAsync(username, hashedPassword);
            if (userAccount == null)
            {
                ModelState.AddModelError("Error", "Email hoặc mật khẩu không đúng.");
                return View();
            }

            var userData = new WebUserData()
            {
                UserId = userAccount.UserId,
                UserName = userAccount.UserName,
                DisplayName = userAccount.DisplayName,
                Email = userAccount.Email,
                Photo = userAccount.Photo
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                userData.CreatePrincipal());

            return RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> Logout()
        {
            HttpContext.Session.Clear();
            await HttpContext.SignOutAsync();
            return RedirectToAction("Login");
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Register()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");

            await PrepareProvinceViewDataAsync();
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Register(string email, string password, string confirmPassword)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError("Error", "Vui lòng nhập email và mật khẩu.");
                return View();
            }

            if (password != confirmPassword)
            {
                ModelState.AddModelError("Error", "Mật khẩu xác nhận không khớp.");
                return View();
            }

            bool emailExists = !(await PartnerDataService.ValidatelCustomerEmailAsync(email, 0));
            if (emailExists)
            {
                ModelState.AddModelError("Error", "Email này đã được sử dụng. Vui lòng dùng email khác.");
                return View();
            }

            var customer = new Customer
            {
                // Đăng ký tối giản: gán mặc định từ email để thỏa dữ liệu bắt buộc
                CustomerName = email,
                ContactName = email,
                Email = email.Trim(),
                Phone = null,
                Address = null,
                Province = null,
                IsLocked = false
            };

            int newId = await PartnerDataService.AddCustomerAsync(customer);
            if (newId <= 0)
            {
                ModelState.AddModelError("Error", "Đăng ký thất bại. Vui lòng thử lại.");
                return View();
            }

            string hashedPassword = CryptHelper.HashMD5(password);
            await SecurityDataService.ChangeCustomerPasswordAsync(email, hashedPassword);

            TempData["SuccessMessage"] = "Đăng ký thành công! Vui lòng đăng nhập.";
            return RedirectToAction("Login");
        }

        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var userData = User.GetUserData();
            if (userData == null || !int.TryParse(userData.UserId, out int customerId))
                return RedirectToAction("Login");

            var customer = await PartnerDataService.GetCustomerAsync(customerId);
            if (customer == null)
                return RedirectToAction("Login");

            await PrepareProvinceViewDataAsync(customer.Province);
            return View(customer);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Profile(int customerID, string customerName, string contactName,
            string email, string phone, string address, string? province)
        {
            var customer = await PartnerDataService.GetCustomerAsync(customerID);
            if (customer == null)
                return RedirectToAction("Login");

            province = string.IsNullOrWhiteSpace(province) ? null : province.Trim();

            if (!string.IsNullOrWhiteSpace(province))
            {
                var validProvinces = await DictionaryDataService.ListProvincesAsync();
                bool isValidProvince = validProvinces.Any(p =>
                    p.ProvinceName.Equals(province, StringComparison.OrdinalIgnoreCase));

                if (!isValidProvince)
                {
                    ModelState.AddModelError("Error", "Tỉnh/Thành phố không hợp lệ.");
                    await PrepareProvinceViewDataAsync(customer.Province);
                    return View(customer);
                }
            }

            customer.CustomerName = customerName;
            customer.ContactName = contactName;
            customer.Phone = phone;
            customer.Address = address;
            customer.Province = province;

            bool result = await PartnerDataService.UpdateCustomerAsync(customer);
            if (result)
                ViewBag.SuccessMessage = "Cập nhật thông tin thành công.";
            else
                ModelState.AddModelError("Error", "Cập nhật thất bại. Vui lòng thử lại.");

            await PrepareProvinceViewDataAsync(customer.Province);
            return View(customer);
        }

        [Authorize]
        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> ChangePassword(string oldPassword, string newPassword, string confirmPassword)
        {
            var userData = User.GetUserData();
            if (userData == null || string.IsNullOrWhiteSpace(userData.UserName))
                return RedirectToAction("Login");

            if (string.IsNullOrWhiteSpace(oldPassword) || string.IsNullOrWhiteSpace(newPassword)
                || string.IsNullOrWhiteSpace(confirmPassword))
            {
                ModelState.AddModelError("Error", "Vui lòng nhập đầy đủ thông tin.");
                return View();
            }

            if (newPassword != confirmPassword)
            {
                ModelState.AddModelError("Error", "Mật khẩu xác nhận không khớp.");
                return View();
            }

            string oldHash = CryptHelper.HashMD5(oldPassword);
            var account = await SecurityDataService.CustomerAuthorizeAsync(userData.UserName, oldHash);
            if (account == null)
            {
                ModelState.AddModelError("Error", "Mật khẩu cũ không đúng.");
                return View();
            }

            string newHash = CryptHelper.HashMD5(newPassword);
            bool success = await SecurityDataService.ChangeCustomerPasswordAsync(userData.UserName, newHash);
            if (!success)
            {
                ModelState.AddModelError("Error", "Đổi mật khẩu thất bại. Vui lòng thử lại.");
                return View();
            }

            ViewBag.SuccessMessage = "Đổi mật khẩu thành công.";
            return View();
        }

        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
