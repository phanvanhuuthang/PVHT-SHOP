using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020427.DataLayers.Interfaces;
using SV22T1020427.Models.Common;
using SV22T1020427.Models.HR;

namespace SV22T1020427.DataLayers.SQLServer
{
    /// <summary>
    /// Cài đặt các phép xử lý dữ liệu cho nhân viên sử dụng SQL Server
    /// </summary>
    public class EmployeeRepository : IEmployeeRepository
    {
        private readonly string _connectionString;

        public EmployeeRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<PagedResult<Employee>> ListAsync(PaginationSearchInput input)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            string countSql = @"
                SELECT COUNT(*) FROM Employees
                WHERE (@SearchValue = N'' OR FullName LIKE @SearchValue OR Email LIKE @SearchValue)";

            string dataSql = @"
                SELECT * FROM Employees
                WHERE (@SearchValue = N'' OR FullName LIKE @SearchValue OR Email LIKE @SearchValue)
                ORDER BY FullName
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

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
            var dataItems = (await connection.QueryAsync<Employee>(dataSql, param)).ToList();

            return new PagedResult<Employee>
            {
                Page = input.Page,
                PageSize = input.PageSize,
                RowCount = rowCount,
                DataItems = dataItems
            };
        }

        public async Task<Employee?> GetAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            string sql = "SELECT * FROM Employees WHERE EmployeeID = @EmployeeID";
            return await connection.QueryFirstOrDefaultAsync<Employee>(sql, new { EmployeeID = id });
        }

        public async Task<int> AddAsync(Employee data)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            string sql = @"
                INSERT INTO Employees(FullName, BirthDate, Address, Phone, Email, IsWorking)
                VALUES (@FullName, @BirthDate, @Address, @Phone, @Email, @IsWorking);
                SELECT SCOPE_IDENTITY();";

            return await connection.ExecuteScalarAsync<int>(sql, new
            {
                data.FullName,
                data.BirthDate,
                data.Address,
                data.Phone,
                data.Email,
                data.IsWorking
            });
        }

        public async Task<bool> UpdateAsync(Employee data)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            string sql = @"
                UPDATE Employees
                SET FullName   = @FullName,
                    BirthDate  = @BirthDate,
                    Address    = @Address,
                    Phone      = @Phone,
                    Email      = @Email,
                    IsWorking  = @IsWorking
                WHERE EmployeeID = @EmployeeID";

            int rowsAffected = await connection.ExecuteAsync(sql, new
            {
                data.FullName,
                data.BirthDate,
                data.Address,
                data.Phone,
                data.Email,
                data.IsWorking,
                data.EmployeeID
            });

            return rowsAffected > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            string sql = "DELETE FROM Employees WHERE EmployeeID = @EmployeeID";
            int rowsAffected = await connection.ExecuteAsync(sql, new { EmployeeID = id });
            return rowsAffected > 0;
        }

        public async Task<bool> IsUsedAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            string sql = "SELECT COUNT(*) FROM Orders WHERE EmployeeID = @EmployeeID";
            int count = await connection.ExecuteScalarAsync<int>(sql, new { EmployeeID = id });
            return count > 0;
        }

        public async Task<bool> ValidateEmailAsync(string email, int id = 0)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            string sql = @"
                SELECT COUNT(*) FROM Employees
                WHERE Email = @Email
                  AND (@EmployeeID = 0 OR EmployeeID <> @EmployeeID)";

            int count = await connection.ExecuteScalarAsync<int>(sql, new { Email = email, EmployeeID = id });
            return count == 0; // true = email hợp lệ (không bị trùng)
        }
    }
}