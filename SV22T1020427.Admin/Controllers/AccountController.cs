using Azure.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using SV22T1020427.BusinessLayers;
using SV22T1020427.Models.Security;
using System.Threading.Tasks;

namespace SV22T1020427.Admin.Controllers
{
    /// <summary>
    /// Các chức năng liên quan đến tài khoản
    /// </summary>
    
    public class AccountController : Controller
    {
        /// <summary>
        /// Thay đổi mật khẩu admin
        /// </summary>
        /// <returns></returns>
        public IActionResult ChangePassword()
        {
            return View();
        }
        [HttpPost]
        
        public async Task<IActionResult> ChangePassword(string oldPassword,string newPassword, string confirmPassword)
        {
            var userData = User.GetUserData();
            if (userData == null || string.IsNullOrWhiteSpace(userData.UserName))
                return RedirectToAction("Login");
            var oldHash = CryptHelper.HashMD5(oldPassword);
            var userAccount = await SecurityDataService.EmployeeAuthorizeAsync(userData.UserName, oldHash);

            if (userAccount == null)
            {
                ModelState.AddModelError(string.Empty, "Mật khẩu cũ không đúng.");
                return View();
            }
            if (string.IsNullOrWhiteSpace(oldPassword) || string.IsNullOrWhiteSpace(newPassword) || string.IsNullOrWhiteSpace(confirmPassword))
            {
                ModelState.AddModelError(string.Empty, "Vui lòng nhập đầy đủ mật khẩu cũ và mật khẩu mới.");
                return View();
            }
            if (newPassword != confirmPassword)
            {
                ModelState.AddModelError(string.Empty, "Mật khẩu xác nhận không khớp.");
                return View();
            }
            

            var newHash = CryptHelper.HashMD5(newPassword);
            var success = await SecurityDataService.ChangeEmployeePasswordAsync(userData.UserName, newHash);

            if (!success)
            {
                ModelState.AddModelError(string.Empty, "Không thể đổi mật khẩu. Vui lòng thử lại.");
                return View();
            }

            ViewBag.SuccessMessage = "Đổi mật khẩu thành công";
            return View();
        }
        /// <summary>
        /// Giao diện đăng nhập
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login()
        {
            return View();
        }
        /// <summary>
        /// Xử lý đăng nhập
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Login(string username, string password)
        {
            ViewBag.Username = username;
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError("Error", "Vui lòng nhập đầy đủ tên đăng nhập và mật khẩu.");
                return View();
            }
            password = CryptHelper.HashMD5(password);
            // Kiểm tra đăng nhập với username và password đã được mã hóa (hash)
            var userAccount = await SecurityDataService.EmployeeAuthorizeAsync(username, password);
            if (userAccount == null)
            {
                ModelState.AddModelError("Error", "Đăng nhập thất bại");
                return View();
            }
            // Dữ liệu sẽ dùng để ghi vào giấy  chứng nhận (principal)
            var userData = new WebUserData()
            {
                UserId = userAccount.UserId,
                UserName = userAccount.UserName,
                DisplayName = userAccount.DisplayName,
                Email = userAccount.Email,
                Photo = userAccount.Photo,
                Roles = userAccount.RoleNames.Split(',').ToList()
            };
            // Thiết lập phiên đăng nhập nhập (Cấp giấy chứng nhận)
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                userData.CreatePrincipal());
            return RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// Đăng xuất khỏi hệ thống quản trị
        /// </summary>
        /// <returns></returns>
        ///
        public async Task<IActionResult> Logout()

        {
            HttpContext.Session.Clear();
            await HttpContext.SignOutAsync();
            return RedirectToAction("Login");
        }
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
