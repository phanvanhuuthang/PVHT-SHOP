using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020427.DataLayers.Interfaces;
using SV22T1020427.Models.Common;
using SV22T1020427.Models.Partner;

namespace SV22T1020427.DataLayers.SQLServer
{
    /// <summary>
    /// Cài đặt các phép xử lý dữ liệu cho khách hàng sử dụng SQL Server
    /// </summary>
    public class CustomerRepository : ICustomerRepository
    {
        private readonly string _connectionString;

        public CustomerRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<PagedResult<Customer>> ListAsync(PaginationSearchInput input)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            string countSql = @"
                SELECT COUNT(*) FROM Customers
                WHERE (@SearchValue = N'' OR CustomerName LIKE @SearchValue OR ContactName LIKE @SearchValue)";

            string dataSql = input.PageSize > 0
                ? @"
                    SELECT * FROM Customers
                    WHERE (@SearchValue = N'' OR CustomerName LIKE @SearchValue OR ContactName LIKE @SearchValue)
                    ORDER BY CustomerName
                    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY"
                : @"
                    SELECT * FROM Customers
                    WHERE (@SearchValue = N'' OR CustomerName LIKE @SearchValue OR ContactName LIKE @SearchValue)
                    ORDER BY CustomerName";

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
            var dataItems = (await connection.QueryAsync<Customer>(dataSql, param)).ToList();

            return new PagedResult<Customer>
            {
                Page = input.Page,
                PageSize = input.PageSize,
                RowCount = rowCount,
                DataItems = dataItems
            };
        }

        public async Task<Customer?> GetAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            string sql = "SELECT * FROM Customers WHERE CustomerID = @CustomerID";
            return await connection.QueryFirstOrDefaultAsync<Customer>(sql, new { CustomerID = id });
        }

        public async Task<int> AddAsync(Customer data)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            string sql = @"
                INSERT INTO Customers(CustomerName, ContactName, Province, Address, Phone, Email, IsLocked)
                VALUES (@CustomerName, @ContactName, @Province, @Address, @Phone, @Email, @IsLocked);
                SELECT SCOPE_IDENTITY();";

            return await connection.ExecuteScalarAsync<int>(sql, new
            {
                data.CustomerName,
                data.ContactName,
                data.Province,
                data.Address,
                data.Phone,
                data.Email,
                data.IsLocked
            });
        }

        public async Task<bool> UpdateAsync(Customer data)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            string sql = @"
                UPDATE Customers
                SET CustomerName = @CustomerName,
                    ContactName  = @ContactName,
                    Province     = @Province,
                    Address      = @Address,
                    Phone        = @Phone,
                    Email        = @Email,
                    IsLocked     = @IsLocked
                WHERE CustomerID = @CustomerID";

            int rowsAffected = await connection.ExecuteAsync(sql, new
            {
                data.CustomerName,
                data.ContactName,
                data.Province,
                data.Address,
                data.Phone,
                data.Email,
                data.IsLocked,
                data.CustomerID
            });

            return rowsAffected > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            string sql = "DELETE FROM Customers WHERE CustomerID = @CustomerID";
            int rowsAffected = await connection.ExecuteAsync(sql, new { CustomerID = id });
            return rowsAffected > 0;
        }

        public async Task<bool> IsUsedAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            string sql = "SELECT COUNT(*) FROM Orders WHERE CustomerID = @CustomerID";
            int count = await connection.ExecuteScalarAsync<int>(sql, new { CustomerID = id });
            return count > 0;
        }

        public async Task<bool> ValidateEmailAsync(string email, int id = 0)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            // Nếu id = 0: kiểm tra email mới, nếu id > 0: bỏ qua bản ghi hiện tại
            string sql = @"
                SELECT COUNT(*) FROM Customers
                WHERE Email = @Email
                  AND (@CustomerID = 0 OR CustomerID <> @CustomerID)";

            int count = await connection.ExecuteScalarAsync<int>(sql, new { Email = email, CustomerID = id });
            return count == 0; // true = email hợp lệ (không bị trùng)
        }
    }
}