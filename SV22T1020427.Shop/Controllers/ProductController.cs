using Microsoft.AspNetCore.Mvc;
using SV22T1020427.BusinessLayers;
using SV22T1020427.Models.Catalog;
using SV22T1020427.Models.Common;

namespace SV22T1020427.Shop.Controllers
{
    public class ProductController : Controller
    {
        private const string PRODUCTSEARCHINPUT = "ProductSearchInput";

        public async Task<IActionResult> Index(ProductSearchInput? input)
        {
            bool hasQuery =
                Request.Query.ContainsKey("searchValue") ||
                Request.Query.ContainsKey("categoryID") ||
                Request.Query.ContainsKey("minPrice") ||
                Request.Query.ContainsKey("maxPrice") ||
                Request.Query.ContainsKey("page") ||
                Request.Query.ContainsKey("pageSize");

            ProductSearchInput model;
            if (hasQuery && input != null)
            {
                model = input;
            }
            else
            {
                model = ApplicationContext.GetSessionData<ProductSearchInput>(PRODUCTSEARCHINPUT)
                        ?? new ProductSearchInput
                        {
                            Page = 1,
                            PageSize = ApplicationContext.PageSize,
                            SearchValue = string.Empty,
                            CategoryID = 0,
                            MinPrice = 0,
                            MaxPrice = 0
                        };
            }

            if (model.Page <= 0) model.Page = 1;
            if (model.PageSize <= 0) model.PageSize = ApplicationContext.PageSize;
            model.SearchValue ??= string.Empty;

            await LoadCategoriesAsync(model.CategoryID, model.SearchValue);
            ApplicationContext.SetSessionData(PRODUCTSEARCHINPUT, model);

            return View(model);
        }

        /// <summary>
        /// Tìm kiếm sản phẩm và hiển thị danh sách sản phẩm
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Search(ProductSearchInput input)
        {
            if (input.Page <= 0) input.Page = 1;
            if (input.PageSize <= 0) input.PageSize = ApplicationContext.PageSize;
            input.SearchValue ??= string.Empty;

            var result = await CatalogDataService.ListProductsAsync(input);
            ApplicationContext.SetSessionData(PRODUCTSEARCHINPUT, input);

            return View(result);
        }

        public async Task<IActionResult> Details(int id)
        {
            var product = await CatalogDataService.GetProductAsync(id);
            if (product == null)
                return RedirectToAction("Index");

            var photos = await CatalogDataService.ListPhotosAsync(id);
            var attributes = await CatalogDataService.ListAttributesAsync(id);

            ViewBag.Photos = photos;
            ViewBag.Attributes = attributes;

            return View(product);
        }

        private async Task LoadCategoriesAsync(int categoryID, string searchValue)
        {
            var categories = await CatalogDataService.ListCategoriesAsync(
                new PaginationSearchInput { Page = 1, PageSize = 0 });

            ViewBag.Categories = categories.DataItems;
            ViewBag.CategoryID = categoryID;
            ViewBag.SearchValue = searchValue;
        }
    }
}
