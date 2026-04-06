using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020427.BusinessLayers;
using SV22T1020427.Models.Catalog;
using SV22T1020427.Models.Common;


namespace SV22T1020427.Admin.Controllers
{
    [Authorize(Roles = $"{WebUserRoles.Administrator},{WebUserRoles.DataManager}")]
    public class CategoryController : Controller
    {
        /// <summary>
        /// Tìm kiếm và hiển thị danh sách loại hàng
        /// </summary>
        /// <returns></returns>

        private const string CATEGORYSEARCHINPUT = "CategorySearchInput";
        public IActionResult Index()
        {
            var input = ApplicationContext.GetSessionData<PaginationSearchInput>(CATEGORYSEARCHINPUT);
            if (input == null) input = new PaginationSearchInput
            {
                Page = 1,
                PageSize = ApplicationContext.PageSize,
                SearchValue = ""
            };

            return View(input);
        }
        /// <summary>
        ///  Tìm kiếm loại hàng và trả về kết quả dưới dạng phân trang
        /// </summary>
        /// <param name="input">Đầu vào tìm kiếm</param>
        /// <returns></returns>
        public async Task<IActionResult> Search(PaginationSearchInput input)
        {
      //      await Task.Delay(500);
            var result = await CatalogDataService.ListCategoriesAsync(input);
            ApplicationContext.SetSessionData(CATEGORYSEARCHINPUT, input);
            return View(result);
        }
        /// <summary>
        /// Tạo mới loại hàng
        /// </summary>
        /// <returns></returns>
        public IActionResult Create()
        {
            ViewBag.Title = "Bổ sung loại hàng";
            var model = new Category()
            {
                CategoryID = 0
            };
            return View("Edit",model);
        }

        /// <summary>
        /// Cập nhật thông tin loại hàng
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<IActionResult> Edit(int id)
        {
            ViewBag.Title = "Cập nhật thông tin loại hàng";
            var model = await CatalogDataService.GetCategoryAsync(id);
            if (model == null)
            {
                return RedirectToAction("Index");
            }
            return View(model);
            
        }
        public async Task<IActionResult> SaveData(Category data)
        {
            try
            {
                ViewBag.Title = data.CategoryID == 0 ? "Bổ sung loại hàng" : "Cập nhật thông tin loại hàng";

                if (string.IsNullOrWhiteSpace(data.CategoryName))
                    ModelState.AddModelError(nameof(data.CategoryName), "Vui lòng nhập tên loại hàng");

                if (string.IsNullOrWhiteSpace(data.Description))
                    data.Description = "";

                if (!ModelState.IsValid)
                    return View("Edit", data);
                //Yêu cầu lưu dữ liệu vào CSDL
                if (data.CategoryID == 0)
                {
                    await CatalogDataService.AddCategoryAsync(data);
                }
                else
                {
                    await CatalogDataService.UpdateCategoryAsync(data);
                }
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                return View("Edit", data);
            }
        }
        /// <summary>
        /// Xóa loại hàng
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<IActionResult> Delete(int id)
        {
            if (Request.Method == "POST")
            {
                await CatalogDataService.DeleteCategoryAsync(id);
                return RedirectToAction("Index");
            }
            var model = await CatalogDataService.GetCategoryAsync(id);
            if (model == null) return RedirectToAction("Index");
            ViewBag.AllowDelete = !await CatalogDataService.IsUsedCategoryAsync(id);
            return View(model);
        }
    }
}
