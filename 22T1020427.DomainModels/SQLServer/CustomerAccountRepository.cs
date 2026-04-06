using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020427.DataLayers.Interfaces;
using SV22T1020427.Models.Security;

namespace SV22T1020427.DataLayers.SQLServer
{
    /// <summary>
    /// Cài đặt các phép xử lý dữ liệu liên quan đến tài khoản khách hàng (bên trang Shop)
    /// </summary>
    public class CustomerAccountRepository : IUserAccountRepository
    {
        private readonly string _connectionString;

        public CustomerAccountRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<UserAccount?> AuthorizeAsync(string userName, string password)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            // Khách hàng không có RoleNames, Photo nên đặt giá trị rỗng
            string sql = @"
                SELECT CAST(CustomerID AS nvarchar(20)) AS UserId,
                       Email                            AS UserName,
                       CustomerName                     AS DisplayName,
                       ISNULL(Email, N'')               AS Email,
                       N''                              AS Photo,
                       N''                              AS RoleNames
                FROM   Customers
                WHERE  Email = @UserName
                  AND  Password = @Password
                  AND  IsLocked = 0"; 

            return await connection.QueryFirstOrDefaultAsync<UserAccount>(sql, 
                new { UserName = userName, Password = password });
        }

        public async Task<bool> ChangePasswordAsync(string userName, string password)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            string sql = @"
                UPDATE Customers
                SET    Password = @Password
                WHERE  Email = @UserName";

            int rowsAffected = await connection.ExecuteAsync(sql, 
                new { UserName = userName, Password = password });

            return rowsAffected > 0;
        }
        public Task<bool> ChangeRoleAsync(string userName, string[] roles)
        {
            return Task.FromResult(false);
        }
        public Task<string> GetRoleNamesAsync(string userName)
        {
            return Task.FromResult("");
        }
    }
}