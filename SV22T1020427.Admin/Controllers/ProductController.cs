using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualBasic;
using SV22T1020427.BusinessLayers;
using SV22T1020427.Models.Catalog;
using SV22T1020427.Models.Common;
using SV22T1020427.Models.HR;
using System.Numerics;
using System.Threading.Tasks;

namespace SV22T1020427.Admin.Controllers
{
    [Authorize(Roles = $"{WebUserRoles.Administrator},{WebUserRoles.DataManager}")]
    public class ProductController : Controller
    {
        private readonly ILogger<ProductController> _logger;

        public ProductController(ILogger<ProductController> logger)
        {
            _logger = logger;
        }
        /// <summary>
        /// Nhập đầu vào tìm kiếm mặt hàng
        /// </summary>
        /// <returns></returns>
        private const string PRODUCTSEARCHINPUT = "ProductSearchInput";
        public  IActionResult Index( )
        {
            var input = ApplicationContext.GetSessionData<ProductSearchInput>(PRODUCTSEARCHINPUT);
            if (input == null) 
             input = new ProductSearchInput
             {
                 Page = 1,
                 PageSize = ApplicationContext.PageSize,
                 SearchValue = "",
                 CategoryID = 0,
                    SupplierID = 0,
                    MinPrice = 0,
                    MaxPrice = 0
             };
            
            return View(input);

          

           
        }
        /// <summary>
        /// Tìm kiếm và hiển thị đơn hàng
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public async Task<IActionResult> Search(ProductSearchInput input)
        {
        //    await Task.Delay(1000);
            var result = await CatalogDataService.ListProductsAsync(input);
            ApplicationContext.SetSessionData(PRODUCTSEARCHINPUT, input);
            return View(result);
        }
        /// <summary>
        /// Bổ sung mặt hàng mới
        /// </summary>
        /// <returns></returns>
        public IActionResult Create()
        {
            ViewBag.Title = "Bổ sung mặt hàng";
            var model = new Product()
            {
                ProductID = 0,
                IsSelling = true

            };
            return View("Edit",model);
        }
        /// <summary>
        /// Cập nhật thông tin mặt hàng
        /// </summary>
        /// <param name="id">Mã của mặt hàng cần cập nhật</param>
        /// <returns></returns>
        public  async Task<IActionResult> Edit(int id)
        {
            ViewBag.Title = "Cập nhật thông tin mặt hàng";
            var model = await CatalogDataService.GetProductAsync(id);
            if (model == null)
                return RedirectToAction("Index");
            ViewBag.ProductID = id;
            ViewBag.ProductPhotos =
                await CatalogDataService.ListPhotosAsync(id);
            ViewBag.ProductAttributes = await CatalogDataService.ListAttributesAsync(id);

            return View(model);
        }
        /// <summary>
        /// Lưu dữ liệu vào CSDL
        /// </summary>
        /// <param name="data"></param>
        /// <param name="uploadPhoto"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> SaveData(Product data, IFormFile? uploadPhoto)
        {
            try
            {
                ViewBag.Title = data.ProductID == 0 ? "Bổ sung mặt hàng" : "Cập nhật thông tin mặt hàng";

                if (string.IsNullOrWhiteSpace(data.ProductName))
                    ModelState.AddModelError(nameof(data.ProductName), "Vui lòng nhập tên sản phẩm");
                if (data.Price <= 0)
                    ModelState.AddModelError(nameof(data.Price), "Vui lòng nhập giá sản phẩm > 0");
                if (string.IsNullOrWhiteSpace(data.Unit))
                    ModelState.AddModelError(nameof(data.Unit), "Vui lòng nhập đơn vị tính");
                if (!data.CategoryID.HasValue || data.CategoryID == 0)
                    ModelState.AddModelError(nameof(data.CategoryID), "Vui lòng chọn loại hàng");
                if (!data.SupplierID.HasValue || data.SupplierID == 0)
                    ModelState.AddModelError(nameof(data.SupplierID), "Vui lòng chọn nhà cung cấp");

                if (!ModelState.IsValid)
                {
                    await LoadEditBagsAsync(data.ProductID);
                    return View("Edit", data);
                }

                if (uploadPhoto != null)
                    data.Photo = await SaveProductImageAsync(uploadPhoto);

                data.ProductName ??= "";
                data.ProductDescription ??= "";
                data.Unit ??= "";
                data.Photo ??= "nophoto.png";

                if (data.ProductID == 0)
                {
                    var newId = await CatalogDataService.AddProductAsync(data);
                    if (newId <= 0)
                    {
                        ModelState.AddModelError(string.Empty, "Không thể bổ sung mặt hàng. Vui lòng kiểm tra dữ liệu.");
                        await LoadEditBagsAsync(data.ProductID);
                        return View("Edit", data);
                    }
                }
                else
                {
                    var success = await CatalogDataService.UpdateProductAsync(data);
                    if (!success)
                    {
                        ModelState.AddModelError(string.Empty, "Không thể cập nhật mặt hàng. Vui lòng kiểm tra dữ liệu.");
                        await LoadEditBagsAsync(data.ProductID);
                        return View("Edit", data);
                    }
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SaveData thất bại. ProductID={ProductID}, Message={Message}", data.ProductID, ex.Message);
                ModelState.AddModelError(string.Empty, "Hệ thống đang bận hoặc dữ liệu không hợp lệ. Vui lòng kiểm tra dữ liệu hoặc thử lại sau");
                await LoadEditBagsAsync(data.ProductID);
                return View("Edit", data);
            }
        }
        /// <summary>
        /// Xóa mặt hàng
        /// </summary>
        /// <param name="id">Mã mặt hàng cần xóa</param>
        /// <returns></returns>
        public async Task<IActionResult> Delete(int id)
        {
            if (Request.Method == "POST")
            {
                var deleted = await CatalogDataService.DeleteProductAsync(id, ApplicationContext.WWWRootPath);
                if (!deleted)
                {
                    TempData["ErrorMessage"] = "Mặt hàng đang có trong đơn hàng, không thể xóa.";
                    return RedirectToAction("Delete", new { id });
                }

                TempData["SuccessMessage"] = "Đã xóa mặt hàng thành công.";
                return RedirectToAction("Index");
            }

            var model = await CatalogDataService.GetProductAsync(id);
            if (model == null)
                return RedirectToAction("Index");

            ViewBag.AllowDelete = !await CatalogDataService.IsUsedProductAsync(id);

            var category = model.CategoryID.HasValue
                ? await CatalogDataService.GetCategoryAsync(model.CategoryID.Value)
                : null;

            var supplier = model.SupplierID.HasValue
                ? await PartnerDataService.GetSupplierAsync(model.SupplierID.Value)
                : null;

            ViewBag.CategoryName = category?.CategoryName ?? "";
            ViewBag.SupplierName = supplier?.SupplierName ?? "";

            return View(model);
        }
        /// <summary>
        /// Hiển thị danh sách các thuộc tính của mặt hàng
        /// </summary>
        /// <param name="id">Mã của mặt hàng cần lấy thuộc tính</param>
        /// <returns></returns>
        public async Task<IActionResult> ListAttributes(int id)
        {
            ViewBag.Title = "Các Thuộc Tính";
            var result = await CatalogDataService.ListAttributesAsync(id);
            ViewBag.ProductId = id; 
            return View(result);
        }
        /// <summary>
        /// Bổ sung thuộc tính mới cho mặt hàng
        /// </summary>
        /// <param name="id">Mã của mặt hàng cần bổ sung thuộc tính</param>
        /// <returns></returns>
        public IActionResult CreateAttribute(int id)
        {
            var model = new ProductAttribute()
            {
                ProductID = id,
                AttributeID = 0
            };
            ViewBag.Title = "Bổ sung thuộc tính";
            ViewBag.ProductId = id;
            return View("EditAttribute",model);
        }
        /// <summary>
        /// Cập nhật thuộc tính của mặt hàng
        /// </summary>
        /// <param name="id">Mã mặt hàng có thuộc tính</param>
        /// <param name="attributeId">Mã thuộc tính cần cập nhật</param>
        /// <returns></returns>
        public async Task<IActionResult> EditAttribute(int id, long attributeId)
        {
            ViewBag.Title = "Cập nhật thuộc tính";
            var model = await CatalogDataService.GetAttributeAsync(attributeId);
            if (model == null)
                return RedirectToAction("Index");

            return View(model);
        }
        /// <summary>
        /// Xóa một thuộc tính của mặt hàng
        /// </summary>
        /// <param name="id">Mã mặt hàng có thuộc tính cần xóa</param>
        /// <param name="attributeId">Mã thuộc tính cần xóa</param>
        /// <returns></returns>
        public async Task<IActionResult> DeleteAttribute(int id, long attributeId)
        {
            if (Request.Method == "POST")
            {
                await CatalogDataService.DeleteAttributeAsync(attributeId);
                return Redirect($"~/Product/Edit/{id}#attributes");
            }

            var model = await CatalogDataService.GetAttributeAsync(attributeId);
            if (model == null)
                return RedirectToAction("Edit", new { id });

            var product = await CatalogDataService.GetProductAsync(id);
            ViewBag.ProductName = product?.ProductName ?? "";
            ViewBag.ProductID = id;
            ViewBag.AttributeID = attributeId;
            ViewBag.AllowDelete = true;

            return View(model);
        }
        /// <summary>
        /// Hiển thị danh sách hình ảnh của một mặt hàng
        /// </summary>
        /// <param name="id">Mã mặt hàng cần lấy danh sách ảnh</param>
        /// <returns></returns>
        public async Task<IActionResult> ListPhotos(int id)
        {
            ViewBag.Title = "Thư viện ảnh";
            var result = await CatalogDataService.ListPhotosAsync(id);
            ViewBag.ProductId = id;
            return View(result);
        }
        /// <summary>
        /// Bổ sung ảnh cho mặt hàng
        /// </summary>
        /// <param name="id">Mã của mặt hàng cần bổ sung hình ảnh</param>
        /// <returns></returns>
        public IActionResult CreatePhoto(int id)
        {
            ViewBag.Title = "Bổ sung hình ảnh";
            var model = new ProductPhoto()
            {
                ProductID = id,
                PhotoID = 0
            };
            ViewBag.ProductId = id;
            return View("EditPhoto", model);
        }
        /// <summary>
        /// Cập nhật ảnh của mặt hàng
        /// </summary>
        /// <param name="id">Mã mặt hàng có ảnh cần cập nhật</param>
        /// <param name="photoId">Mã ảnh cần cập nhật</param>
        /// <returns></returns>
        public async Task<IActionResult> EditPhoto(int id, int photoId)
        {
            ViewBag.Title = "Cập nhật hình ảnh";
            
            var model = await CatalogDataService.GetPhotoAsync(photoId);
            if (model == null)
                return RedirectToAction("Index");
            ViewBag.ProductId = id;
            return View(model);
        }
        /// <summary>
        /// Xóa một ảnh của mặt hàng
        /// </summary>
        /// <param name="id">Mã mặt hàng có ảnh cần xóa</param>
        /// <param name="photoId">Mã ảnh cần xóa</param>
        /// <returns></returns>
        public async Task<IActionResult> DeletePhoto(int id, int photoId)
        {
            if (Request.Method == "POST")
            {
                await CatalogDataService.DeletePhotoAsync(photoId);
                return Redirect($"~/Product/Edit/{id}#photos");
            }

            var model = await CatalogDataService.GetPhotoAsync(photoId);
            if (model == null)
                return RedirectToAction("Edit", new { id });

            var product = await CatalogDataService.GetProductAsync(id);
            ViewBag.ProductName = product?.ProductName ?? "";
            ViewBag.AllowDelete = true;

            return View(model);

        }
        [HttpPost]
        public async Task<IActionResult> SaveProductAttribute(ProductAttribute data)
        {
            try
            {
                ViewBag.Title = data.AttributeID == 0 ? "Bổ sung thuộc tính" : "Cập nhật thuộc tính";
                ViewBag.ProductId = data.ProductID;

                if (data.ProductID <= 0)
                    ModelState.AddModelError(nameof(data.ProductID), "Mặt hàng không hợp lệ");
                if (string.IsNullOrWhiteSpace(data.AttributeName))
                    ModelState.AddModelError(nameof(data.AttributeName), "Vui lòng nhập tên thuộc tính");
                if (string.IsNullOrWhiteSpace(data.AttributeValue))
                    ModelState.AddModelError(nameof(data.AttributeValue), "Vui lòng nhập giá trị thuộc tính");
                if (data.DisplayOrder < 0)
                    ModelState.AddModelError(nameof(data.DisplayOrder), "Thứ tự hiển thị phải >= 0");

                if (!ModelState.IsValid)
                    return View("EditAttribute", data);

                if (data.AttributeID == 0)
                {
                    var newId = await CatalogDataService.AddAttributeAsync(data);
                    if (newId <= 0)
                    {
                        ModelState.AddModelError(string.Empty, "Không thể lưu thuộc tính. Vui lòng kiểm tra dữ liệu.");
                        return View("EditAttribute", data);
                    }
                }
                else
                {
                    var success = await CatalogDataService.UpdateAttributeAsync(data);
                    if (!success)
                    {
                        ModelState.AddModelError(string.Empty, "Không thể cập nhật thuộc tính. Vui lòng kiểm tra dữ liệu.");
                        return View("EditAttribute", data);
                    }
                }

                return Redirect($"~/Product/Edit/{data.ProductID}#attributes");
            }
            catch
            {
                ModelState.AddModelError(string.Empty, "Hệ thống đang bận hoặc dữ liệu không hợp lệ. Vui lòng thử lại sau.");
                ViewBag.ProductId = data.ProductID;
                return View("EditAttribute", data);
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveProductPhoto(ProductPhoto data, IFormFile? uploadPhoto)
        {
            try
            {
                ViewBag.Title = data.PhotoID == 0 ? "Bổ sung ảnh" : "Cập nhật ảnh";
                ViewBag.ProductId = data.ProductID;

                if (data.ProductID <= 0)
                    ModelState.AddModelError(nameof(data.ProductID), "Mặt hàng không hợp lệ");
                if (string.IsNullOrWhiteSpace(data.Description))
                    ModelState.AddModelError(nameof(data.Description), "Vui lòng nhập mô tả ảnh");
                if (data.DisplayOrder < 0)
                    ModelState.AddModelError(nameof(data.DisplayOrder), "Thứ tự hiển thị phải >= 0");

                if (uploadPhoto != null)
                {
                    var fileName = await SaveProductImageAsync(uploadPhoto);
                    data.Photo = fileName;
                }

                if (string.IsNullOrWhiteSpace(data.Photo))
                    ModelState.AddModelError(nameof(data.Photo), "Vui lòng chọn ảnh");

                if (!ModelState.IsValid)
                    return View("EditPhoto", data);

                if (data.PhotoID == 0)
                {
                    var newId = await CatalogDataService.AddPhotoAsync(data);
                    if (newId <= 0)
                    {
                        ModelState.AddModelError(string.Empty, "Không thể lưu ảnh. Vui lòng kiểm tra dữ liệu.");
                        return View("EditPhoto", data);
                    }
                }
                else
                {
                    var success = await CatalogDataService.UpdatePhotoAsync(data);
                    if (!success)
                    {
                        ModelState.AddModelError(string.Empty, "Không thể cập nhật ảnh. Vui lòng kiểm tra dữ liệu.");
                        return View("EditPhoto", data);
                    }
                }

                return Redirect($"~/Product/Edit/{data.ProductID}#photos");
            }
            catch
            {
                ModelState.AddModelError(string.Empty, "Hệ thống đang bận hoặc dữ liệu không hợp lệ. Vui lòng thử lại sau.");
                ViewBag.ProductId = data.ProductID;
                return View("EditPhoto", data);
            }
        }

        private async Task<string> SaveProductImageAsync(IFormFile uploadPhoto)
        {
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(uploadPhoto.FileName)}";

            var productFolder = ApplicationContext.GetConfigValue("ImageStorage:ProductFolder");
    if (string.IsNullOrWhiteSpace(productFolder))
    {
        productFolder = Path.Combine(ApplicationContext.WWWRootPath, "images", "products");
    }

    Directory.CreateDirectory(productFolder);

    var filePath = Path.Combine(productFolder, fileName);
    await using (var stream = new FileStream(filePath, FileMode.Create))
    {
        await uploadPhoto.CopyToAsync(stream);
    }

    return fileName;
        }

        private async Task LoadEditBagsAsync(int productId)
{
    ViewBag.ProductID = productId;
    ViewBag.ProductPhotos = productId > 0
        ? await CatalogDataService.ListPhotosAsync(productId)
        : new List<ProductPhoto>();

    ViewBag.ProductAttributes = productId > 0
        ? await CatalogDataService.ListAttributesAsync(productId)
        : new List<ProductAttribute>();
}
    }
}