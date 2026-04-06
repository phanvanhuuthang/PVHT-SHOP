using Microsoft.AspNetCore.Mvc;
using SV22T1020427.BusinessLayers;
using SV22T1020427.Models.Catalog;
using SV22T1020427.Models.Common;
using SV22T1020427.Shop.Models;
using System.Diagnostics;

namespace SV22T1020427.Shop.Controllers
{
    public class HomeController : Controller
    {
        public async Task<IActionResult> Index()
        {
            var input = new ProductSearchInput { Page = 1, PageSize = 8 };
            var featuredProducts = await CatalogDataService.ListProductsAsync(input);
            var categories = await CatalogDataService.ListCategoriesAsync(new PaginationSearchInput { Page = 1, PageSize = 0 });

            ViewBag.Categories = categories.DataItems;
            return View(featuredProducts.DataItems);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
