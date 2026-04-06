using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020427.DataLayers.Interfaces;
using SV22T1020427.Models.Security;

namespace SV22T1020427.DataLayers.SQLServer
{
    /// <summary>
    /// Cài đặt các phép xử lý dữ liệu liên quan đến tài khoản nhân viên (quản trị hệ thống)
    /// </summary>
    public class EmployeeAccountRepository : IUserAccountRepository
    {
        private readonly string _connectionString;

        public EmployeeAccountRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<UserAccount?> AuthorizeAsync(string userName, string password)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            string sql = @"
                SELECT CAST(EmployeeID AS nvarchar(20)) AS UserId,
                       Email                            AS UserName,
                       FullName                         AS DisplayName,
                       ISNULL(Email, N'')               AS Email,
                       N''                              AS Photo,
                       ISNULL(RoleNames, N'')           AS RoleNames
                FROM   Employees
                WHERE  Email = @UserName
                  AND  Password = @Password
                  AND  IsWorking = 1";

            return await connection.QueryFirstOrDefaultAsync<UserAccount>(sql,
                new { UserName = userName, Password = password });
        }

        public async Task<bool> ChangePasswordAsync(string userName, string password)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            string sql = @"
                UPDATE Employees
                SET    Password = @Password
                WHERE  Email = @UserName";

            int rowsAffected = await connection.ExecuteAsync(sql,
                new { UserName = userName, Password = password });

            return rowsAffected > 0;
        }
        public async Task<bool> ChangeRoleAsync(string userName, string[] roles)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            string roleNames = string.Join(",", roles);
            string sql = @"
                UPDATE Employees
                SET    RoleNames = @RoleNames
                WHERE  Email = @UserName";
            int rowsAffected = await connection.ExecuteAsync(sql,
                new { UserName = userName, RoleNames = roleNames });
            return rowsAffected > 0;
        }
        public async Task<string> GetRoleNamesAsync(string userName)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            string sql = @"
        SELECT ISNULL(RoleNames, N'')
        FROM Employees
        WHERE Email = @UserName";

            return await connection.ExecuteScalarAsync<string>(sql, new { UserName = userName }) ?? "";
        }
    }
}