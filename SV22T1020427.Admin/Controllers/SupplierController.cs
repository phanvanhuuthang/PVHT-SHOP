using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020427.BusinessLayers;
using SV22T1020427.Models.Common;
using SV22T1020427.Models.Partner;
using System.Threading.Tasks;

namespace SV22T1020427.Admin.Controllers
{
    [Authorize(Roles = $"{WebUserRoles.Administrator},{WebUserRoles.DataManager}")]
    public class SupplierController : Controller
    {
        /// <summary>
        /// Tìm kiếm và hiển thị danh sách nhà cung cấp
        /// </summary>
        /// <returns></returns>
        //private const int PAGESIZE = 20;
        private const string SUPPLIERSEARCHINPUT = "SupplierSearchInput";
        public IActionResult Index()
        {
            var input =  ApplicationContext.GetSessionData<PaginationSearchInput>(SUPPLIERSEARCHINPUT);
            if (input == null) input = new PaginationSearchInput
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
       //     await Task.Delay(1000);
            var result = await PartnerDataService.ListSuppliersAsync(input);
            ApplicationContext.SetSessionData(SUPPLIERSEARCHINPUT, input);
            return View(result);
        }
        /// <summary>
        /// Bổ sung nhà cung cấp
        /// </summary>
        /// <returns></returns>
        public IActionResult Create()
        {
            ViewBag.Title = "Bổ sung nhà cung cấp";
            var model = new Supplier()
            {
                SupplierID = 0
            };
            return View("Edit",model);
        }
        
        /// <summary>
        /// Cập nhật thông tin nhà cung cấp
        /// </summary>
        /// <param name="id">Mã nhà cung cấp cần cập nhật</param>
        /// <returns></returns>
        public async Task<IActionResult> Edit(int id)
        {
            ViewBag.Title = "Cập nhật thông tin nhà cung cấp";
            var model = await PartnerDataService.GetSupplierAsync(id);
            if (model == null) return RedirectToAction("Index");
            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> SaveData(Supplier data)
        {
            try
            {
                ViewBag.Title = data.SupplierID == 0 ? "Bổ sung nhà cung cấp" : "Cập nhật thông tin nhà cung cấp";

                if (string.IsNullOrWhiteSpace(data.SupplierName))
                    ModelState.AddModelError(nameof(data.SupplierName), "Vui lòng nhập tên nhà cung cấp");

                if (string.IsNullOrWhiteSpace(data.ContactName))
                    data.ContactName = "";

                if (string.IsNullOrWhiteSpace(data.Phone))
                    ModelState.AddModelError(nameof(data.Phone), "Vui lòng nhập số điện thoại");

                if (string.IsNullOrWhiteSpace(data.Email))
                    ModelState.AddModelError(nameof(data.Email), "Vui lòng nhập email");

                if (string.IsNullOrWhiteSpace(data.Province))
                    ModelState.AddModelError(nameof(data.Province), "Vui lòng chọn tỉnh/thành");

                if (string.IsNullOrWhiteSpace(data.Address))
                    data.Address = "";

                if (!ModelState.IsValid)
                    return View("Edit", data);

                if (data.SupplierID == 0)
                {
                    await PartnerDataService.AddSupplierAsync(data);
                }
                else
                {
                    await PartnerDataService.UpdateSupplierAsync(data);
                }
                return RedirectToAction("Index");
            }
            catch 
            {
                ModelState.AddModelError(string.Empty, "Hệ thống đang bận, vui lòng thử lại sau");
                return View("Edit", data);
            }
        }
        /// <summary>
        /// Xóa nhà cung cấp
        /// </summary>
        /// <param name="id">Mã nhà cung cấp cần xóa</param>
        /// <returns></returns>
        public async Task<IActionResult> Delete(int id)
        {
            if (Request.Method == "POST")
            {
                await PartnerDataService.DeleteSupplierAsync(id);
                return RedirectToAction("Index");
            }
            var model = await PartnerDataService.GetSupplierAsync(id);
            if (model == null) return RedirectToAction("Index");
            ViewBag.AllowDelete = !await PartnerDataService.IsUsedSupplierAsync(id);
            return View(model);
        }
    }
}
