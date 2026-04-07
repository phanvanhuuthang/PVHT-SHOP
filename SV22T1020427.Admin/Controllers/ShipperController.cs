
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020427.BusinessLayers;
using SV22T1020427.Models.Common;
using SV22T1020427.Models.Partner;
using System.Threading.Tasks;

namespace SV22T1020427.Admin.Controllers
{
    [Authorize(Roles = $"{WebUserRoles.Administrator},{WebUserRoles.DataManager}")]
    public class ShipperController : Controller
    {
        /// <summary>
        /// Tìm kiếm và hiển thị thông tin người giao hàng
        /// </summary>
        /// <returns></returns>
        private const string SHIPPERSEARCHINPUT = "ShipperSearchInput";
        public IActionResult Index()
        {
            var input = ApplicationContext.GetSessionData<PaginationSearchInput>(SHIPPERSEARCHINPUT);
            if (input == null) input = new PaginationSearchInput
            {
                Page = 1,
                PageSize = ApplicationContext.PageSize,
                SearchValue = ""
            };

            return View(input);
        }
        /// <summary>
        ///  Tìm kiếm người giao hàng và trả về kết quả dưới dạng phân trang
        /// </summary>
        /// <param name="input">Đầu vào tìm kiếm</param>
        /// <returns></returns>
        public async Task<IActionResult> Search(PaginationSearchInput input)
        {
           // await Task.Delay(500);
            var result = await PartnerDataService.ListShippersAsync(input);
            ApplicationContext.SetSessionData(SHIPPERSEARCHINPUT, input);
            return View(result);
        }
        /// <summary>
        /// Bổ sung thông tin người giao hàng
        /// </summary>
        /// <returns></returns>
        public IActionResult Create()
        {
            ViewBag.Title = "Bổ sung thông tin người giao hàng";
            var model = new Shipper()
            {
                ShipperID = 0
            };
            return View("Edit",model);
        }
        /// <summary>
        /// Cập nhật thông tin người giao hàng
        /// </summary>
        /// <param name="id">Mã của người giao hàng</param>
        /// <returns></returns>
        public async Task<IActionResult> Edit(int id)
        {
            ViewBag.Title = "Cập nhật thông tin người giao hàng";
            var model = await PartnerDataService.GetShipperAsync(id);
            if (model == null)
            {
                return RedirectToAction("Index");
            }
            return View(model);

        }
        public async Task<IActionResult> SaveData(Shipper data)
        {
            try
            {
                ViewBag.Title = data.ShipperID == 0 ? "Bổ sung người giao hàng" : "Cập nhật thông tin người giao hàng";

                if (string.IsNullOrWhiteSpace(data.ShipperName))
                    ModelState.AddModelError(nameof(data.ShipperName), "Vui lòng nhập tên người giao hàng");

                if (string.IsNullOrWhiteSpace(data.Phone))
                    ModelState.AddModelError(nameof(data.Phone), "Vui lòng nhập số điện thoại");

                if (!ModelState.IsValid)
                    return View("Edit", data);

                //Yêu cầu lưu dữ liệu vào CSDL
                if (data.ShipperID == 0)
                {
                    await PartnerDataService.AddShipperAsync(data);
                }
                else
                {
                    await PartnerDataService.UpdateShipperAsync(data);
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
        /// Xóa người giao hàng
        /// </summary>
        /// <param name="id">Mã người giao hàng</param>
        /// <returns></returns>
        public async Task<IActionResult> Delete(int id)
        {
            if (Request.Method == "POST")
            {
                await PartnerDataService.DeleteShipperAsync(id);
                return RedirectToAction("Index");
            }
            var model = await PartnerDataService.GetShipperAsync(id);
            if (model == null) return RedirectToAction("Index");
            ViewBag.AllowDelete = !await PartnerDataService.IsUsedShipperAsync(id);
            return View(model);
        }
    }
}
