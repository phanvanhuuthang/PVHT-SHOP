using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020427.DataLayers.Interfaces;
using SV22T1020427.Models.Catalog;
using SV22T1020427.Models.Common;

namespace SV22T1020427.DataLayers.SQLServer
{
    /// <summary>
    /// Cài đặt các phép xử lý dữ liệu cho mặt hàng sử dụng SQL Server
    /// </summary>
    public class ProductRepository : IProductRepository
    {
        private readonly string _connectionString;

        public ProductRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<PagedResult<Product>> ListAsync(ProductSearchInput input)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var searchParam = string.IsNullOrWhiteSpace(input.SearchValue)
                ? ""
                : $"%{input.SearchValue}%";

            string countSql = @"
        SELECT COUNT(*) FROM Products
        WHERE (@SearchValue = N'' OR ProductName LIKE @SearchValue)
          AND (@CategoryID = 0  OR CategoryID = @CategoryID)
          AND (@SupplierID = 0  OR SupplierID = @SupplierID)
          AND (@MinPrice = 0    OR Price >= @MinPrice)
          AND (@MaxPrice = 0    OR Price <= @MaxPrice)";

            string dataSql = input.PageSize > 0
                ? @"
            SELECT * FROM Products
            WHERE (@SearchValue = N'' OR ProductName LIKE @SearchValue)
              AND (@CategoryID = 0  OR CategoryID = @CategoryID)
              AND (@SupplierID = 0  OR SupplierID = @SupplierID)
              AND (@MinPrice = 0    OR Price >= @MinPrice)
              AND (@MaxPrice = 0    OR Price <= @MaxPrice)
            ORDER BY ProductName
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY"
                : @"
            SELECT * FROM Products
            WHERE (@SearchValue = N'' OR ProductName LIKE @SearchValue)
              AND (@CategoryID = 0  OR CategoryID = @CategoryID)
              AND (@SupplierID = 0  OR SupplierID = @SupplierID)
              AND (@MinPrice = 0    OR Price >= @MinPrice)
              AND (@MaxPrice = 0    OR Price <= @MaxPrice)
            ORDER BY ProductName";

            var param = new
            {
                SearchValue = searchParam,
                input.CategoryID,
                input.SupplierID,
                input.MinPrice,
                input.MaxPrice,
                Offset = input.Offset,
                input.PageSize
            };

            int rowCount = await connection.ExecuteScalarAsync<int>(countSql, param);
            var dataItems = (await connection.QueryAsync<Product>(dataSql, param)).ToList();

            return new PagedResult<Product>
            {
                Page = input.Page,
                PageSize = input.PageSize,
                RowCount = rowCount,
                DataItems = dataItems
            };
        }

        public async Task<Product?> GetAsync(int productID)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            string sql = "SELECT * FROM Products WHERE ProductID = @ProductID";
            return await connection.QueryFirstOrDefaultAsync<Product>(sql, new { ProductID = productID });
        }

        public async Task<int> AddAsync(Product data)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            string sql = @"
                INSERT INTO Products(ProductName, ProductDescription, SupplierID, CategoryID, Unit, Price, Photo, IsSelling)
                VALUES (@ProductName, @ProductDescription, @SupplierID, @CategoryID, @Unit, @Price, @Photo, @IsSelling);
                SELECT SCOPE_IDENTITY();";

            return await connection.ExecuteScalarAsync<int>(sql, new
            {
                data.ProductName,
                data.ProductDescription,
                data.SupplierID,
                data.CategoryID,
                data.Unit,
                data.Price,
                data.Photo,
                data.IsSelling
            });
        }

        public async Task<bool> UpdateAsync(Product data)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            string sql = @"
                UPDATE Products
                SET ProductName        = @ProductName,
                    ProductDescription = @ProductDescription,
                    SupplierID         = @SupplierID,
                    CategoryID         = @CategoryID,
                    Unit               = @Unit,
                    Price              = @Price,
                    Photo              = @Photo,
                    IsSelling          = @IsSelling
                WHERE ProductID = @ProductID";

            int rowsAffected = await connection.ExecuteAsync(sql, new
            {
                data.ProductName,
                data.ProductDescription,
                data.SupplierID,
                data.CategoryID,
                data.Unit,
                data.Price,
                data.Photo,
                data.IsSelling,
                data.ProductID
            });

            return rowsAffected > 0;
        }

        public async Task<bool> DeleteAsync(int productID)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            // Xóa ảnh và thuộc tính liên quan trước, sau đó xóa mặt hàng
            string sql = @"
                DELETE FROM ProductPhotos     WHERE ProductID = @ProductID;
                DELETE FROM ProductAttributes WHERE ProductID = @ProductID;
                DELETE FROM Products          WHERE ProductID = @ProductID;";

            int rowsAffected = await connection.ExecuteAsync(sql, new { ProductID = productID });
            return rowsAffected > 0;
        }

        public async Task<bool> IsUsedAsync(int productID)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            string sql = "SELECT COUNT(*) FROM OrderDetails WHERE ProductID = @ProductID";
            int count = await connection.ExecuteScalarAsync<int>(sql, new { ProductID = productID });
            return count > 0;
        }

        // ── Attributes ────────────────────────────────────────────────────────────

        public async Task<List<ProductAttribute>> ListAttributesAsync(int productID)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            string sql = @"
                SELECT * FROM ProductAttributes
                WHERE ProductID = @ProductID
                ORDER BY DisplayOrder";

            return (await connection.QueryAsync<ProductAttribute>(sql, new { ProductID = productID })).ToList();
        }

        public async Task<ProductAttribute?> GetAttributeAsync(long attributeID)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            string sql = "SELECT * FROM ProductAttributes WHERE AttributeID = @AttributeID";
            return await connection.QueryFirstOrDefaultAsync<ProductAttribute>(sql, new { AttributeID = attributeID });
        }

        public async Task<long> AddAttributeAsync(ProductAttribute data)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            string sql = @"
                INSERT INTO ProductAttributes(ProductID, AttributeName, AttributeValue, DisplayOrder)
                VALUES (@ProductID, @AttributeName, @AttributeValue, @DisplayOrder);
                SELECT SCOPE_IDENTITY();";

            return await connection.ExecuteScalarAsync<long>(sql, new
            {
                data.ProductID,
                data.AttributeName,
                data.AttributeValue,
                data.DisplayOrder
            });
        }

        public async Task<bool> UpdateAttributeAsync(ProductAttribute data)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            string sql = @"
                UPDATE ProductAttributes
                SET AttributeName  = @AttributeName,
                    AttributeValue = @AttributeValue,
                    DisplayOrder   = @DisplayOrder
                WHERE AttributeID = @AttributeID";

            int rowsAffected = await connection.ExecuteAsync(sql, new
            {
                data.AttributeName,
                data.AttributeValue,
                data.DisplayOrder,
                data.AttributeID
            });

            return rowsAffected > 0;
        }

        public async Task<bool> DeleteAttributeAsync(long attributeID)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            string sql = "DELETE FROM ProductAttributes WHERE AttributeID = @AttributeID";
            int rowsAffected = await connection.ExecuteAsync(sql, new { AttributeID = attributeID });
            return rowsAffected > 0;
        }

        // ── Photos ────────────────────────────────────────────────────────────────

        public async Task<List<ProductPhoto>> ListPhotosAsync(int productID)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            string sql = @"
                SELECT * FROM ProductPhotos
                WHERE ProductID = @ProductID
                ORDER BY DisplayOrder";

            return (await connection.QueryAsync<ProductPhoto>(sql, new { ProductID = productID })).ToList();
        }

        public async Task<ProductPhoto?> GetPhotoAsync(long photoID)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            string sql = "SELECT * FROM ProductPhotos WHERE PhotoID = @PhotoID";
            return await connection.QueryFirstOrDefaultAsync<ProductPhoto>(sql, new { PhotoID = photoID });
        }

        public async Task<long> AddPhotoAsync(ProductPhoto data)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            string sql = @"
                INSERT INTO ProductPhotos(ProductID, Photo, Description, DisplayOrder, IsHidden)
                VALUES (@ProductID, @Photo, @Description, @DisplayOrder, @IsHidden);
                SELECT SCOPE_IDENTITY();";

            return await connection.ExecuteScalarAsync<long>(sql, new
            {
                data.ProductID,
                data.Photo,
                data.Description,
                data.DisplayOrder,
                data.IsHidden
            });
        }

        public async Task<bool> UpdatePhotoAsync(ProductPhoto data)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            string sql = @"
                UPDATE ProductPhotos
                SET Photo        = @Photo,
                    Description  = @Description,
                    DisplayOrder = @DisplayOrder,
                    IsHidden     = @IsHidden
                WHERE PhotoID = @PhotoID";

            int rowsAffected = await connection.ExecuteAsync(sql, new
            {
                data.Photo,
                data.Description,
                data.DisplayOrder,
                data.IsHidden,
                data.PhotoID
            });

            return rowsAffected > 0;
        }

        public async Task<bool> DeletePhotoAsync(long photoID)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            string sql = "DELETE FROM ProductPhotos WHERE PhotoID = @PhotoID";
            int rowsAffected = await connection.ExecuteAsync(sql, new { PhotoID = photoID });
            return rowsAffected > 0;
        }
    }
}