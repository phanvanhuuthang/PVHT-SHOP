using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020427.DataLayers.Interfaces;
using SV22T1020427.Models.Catalog;
using SV22T1020427.Models.Common;

namespace SV22T1020427.DataLayers.SQLServer
{
    /// <summary>
    /// Cài đặt các phép xử lý dữ liệu cho loại hàng sử dụng SQL Server
    /// </summary>
    public class CategoryRepository : IGenericRepository<Category>
    {
        private readonly string _connectionString;

        public CategoryRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<PagedResult<Category>> ListAsync(PaginationSearchInput input)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            string countSql = @"
        SELECT COUNT(*) FROM Categories
        WHERE (@SearchValue = N'' OR CategoryName LIKE @SearchValue)";

            string dataSql = input.PageSize > 0
                ? @"
            SELECT * FROM Categories
            WHERE (@SearchValue = N'' OR CategoryName LIKE @SearchValue)
            ORDER BY CategoryName
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY"
                : @"
            SELECT * FROM Categories
            WHERE (@SearchValue = N'' OR CategoryName LIKE @SearchValue)
            ORDER BY CategoryName";

            var searchParam = string.IsNullOrWhiteSpace(input.SearchValue)
                ? ""
                : $"%{input.SearchValue}%";

            var param = new
            {
                SearchValue = searchParam,
                Offset = input.Offset,
                PageSize = input.PageSize
            };

            int rowCount = await connection.ExecuteScalarAsync<int>(countSql, param);
            var dataItems = (await connection.QueryAsync<Category>(dataSql, param)).ToList();

            return new PagedResult<Category>
            {
                Page = input.Page,
                PageSize = input.PageSize,
                RowCount = rowCount,
                DataItems = dataItems
            };
        }

        public async Task<Category?> GetAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            string sql = "SELECT * FROM Categories WHERE CategoryID = @CategoryID";
            return await connection.QueryFirstOrDefaultAsync<Category>(sql, new { CategoryID = id });
        }

        public async Task<int> AddAsync(Category data)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            string sql = @"
                INSERT INTO Categories(CategoryName, Description)
                VALUES (@CategoryName, @Description);
                SELECT SCOPE_IDENTITY();";

            return await connection.ExecuteScalarAsync<int>(sql, new
            {
                data.CategoryName,
                data.Description
            });
        }

        public async Task<bool> UpdateAsync(Category data)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            string sql = @"
                UPDATE Categories
                SET CategoryName = @CategoryName,
                    Description  = @Description
                WHERE CategoryID = @CategoryID";

            int rowsAffected = await connection.ExecuteAsync(sql, new
            {
                data.CategoryName,
                data.Description,
                data.CategoryID
            });

            return rowsAffected > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            string sql = "DELETE FROM Categories WHERE CategoryID = @CategoryID";
            int rowsAffected = await connection.ExecuteAsync(sql, new { CategoryID = id });
            return rowsAffected > 0;
        }

        public async Task<bool> IsUsedAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            string sql = "SELECT COUNT(*) FROM Products WHERE CategoryID = @CategoryID";
            int count = await connection.ExecuteScalarAsync<int>(sql, new { CategoryID = id });
            return count > 0;
        }
    }
}