using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020427.DataLayers.Interfaces;
using SV22T1020427.Models.Common;
using SV22T1020427.Models.Partner;

namespace SV22T1020427.DataLayers.SQLServer
{
    /// <summary>
    /// Cài đặt các phép xử lý dữ liệu cho nhà cung cấp sử dụng SQL Server
    /// </summary>
    public class SupplierRepository : IGenericRepository<Supplier>
    {
        private readonly string _connectionString;

        public SupplierRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<PagedResult<Supplier>> ListAsync(PaginationSearchInput input)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            string countSql = @"
        SELECT COUNT(*) FROM Suppliers
        WHERE (@SearchValue = N'' OR SupplierName LIKE @SearchValue OR ContactName LIKE @SearchValue)";

            string dataSql = input.PageSize > 0
                ? @"
            SELECT * FROM Suppliers
            WHERE (@SearchValue = N'' OR SupplierName LIKE @SearchValue OR ContactName LIKE @SearchValue)
            ORDER BY SupplierName
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY"
                : @"
            SELECT * FROM Suppliers
            WHERE (@SearchValue = N'' OR SupplierName LIKE @SearchValue OR ContactName LIKE @SearchValue)
            ORDER BY SupplierName";

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
            var dataItems = (await connection.QueryAsync<Supplier>(dataSql, param)).ToList();

            return new PagedResult<Supplier>
            {
                Page = input.Page,
                PageSize = input.PageSize,
                RowCount = rowCount,
                DataItems = dataItems
            };
        }

        public async Task<Supplier?> GetAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            string sql = "SELECT * FROM Suppliers WHERE SupplierID = @SupplierID";
            return await connection.QueryFirstOrDefaultAsync<Supplier>(sql, new { SupplierID = id });
        }

        public async Task<int> AddAsync(Supplier data)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            string sql = @"
                INSERT INTO Suppliers(SupplierName, ContactName, Province, Address, Phone, Email)
                VALUES (@SupplierName, @ContactName, @Province, @Address, @Phone, @Email);
                SELECT SCOPE_IDENTITY();";

            return await connection.ExecuteScalarAsync<int>(sql, new
            {
                data.SupplierName,
                data.ContactName,
                data.Province,
                data.Address,
                data.Phone,
                data.Email
            });
        }

        public async Task<bool> UpdateAsync(Supplier data)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            string sql = @"
                UPDATE Suppliers
                SET SupplierName = @SupplierName,
                    ContactName  = @ContactName,
                    Province     = @Province,
                    Address      = @Address,
                    Phone        = @Phone,
                    Email        = @Email
                WHERE SupplierID = @SupplierID";

            int rowsAffected = await connection.ExecuteAsync(sql, new
            {
                data.SupplierName,
                data.ContactName,
                data.Province,
                data.Address,
                data.Phone,
                data.Email,
                data.SupplierID
            });

            return rowsAffected > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            string sql = "DELETE FROM Suppliers WHERE SupplierID = @SupplierID";
            int rowsAffected = await connection.ExecuteAsync(sql, new { SupplierID = id });
            return rowsAffected > 0;
        }

        public async Task<bool> IsUsedAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            string sql = "SELECT COUNT(*) FROM Products WHERE SupplierID = @SupplierID";
            int count = await connection.ExecuteScalarAsync<int>(sql, new { SupplierID = id });
            return count > 0;
        }
    }
}