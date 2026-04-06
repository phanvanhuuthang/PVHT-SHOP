using SV22T1020427.DataLayers.Interfaces;
using SV22T1020427.DataLayers.SQLServer;
using SV22T1020427.Models.Security;

namespace SV22T1020427.BusinessLayers
{
    /// <summary>
    /// Cung cấp các chức năng liên quan đến bảo mật và tài khoản người dùng
    /// </summary>
    public static class SecurityDataService
    {
        private static readonly IUserAccountRepository employeeAccountDB;
        private static readonly IUserAccountRepository customerAccountDB;

        static SecurityDataService()
        {
            employeeAccountDB = new EmployeeAccountRepository(Configuration.ConnectionString);
            customerAccountDB = new CustomerAccountRepository(Configuration.ConnectionString);
        }

        /// <summary>
        /// Kiểm tra tài khoản nhân viên (admin)
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="password"></param>
        #region Tài khoản nhân viên
        public static async Task<UserAccount?> EmployeeAuthorizeAsync(string userName, string password)
        {
            if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
                return null;

            return await employeeAccountDB.AuthorizeAsync(userName, password);
        }

        /// <summary>
        /// Đổi mật khẩu tài khoản nhân viên
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="password"></param>
        public static async Task<bool> ChangeEmployeePasswordAsync(string userName, string password)
        {
            if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
                return false;

            return await employeeAccountDB.ChangePasswordAsync(userName, password);
        }
        /// <summary>
        /// Đổi role của tài khoản nhân viên
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="roles"></param>
        /// <returns></returns>
        public static async Task<bool> ChangeEmployeeRoleAsync(string userName, string[] roles)
        {
            if (string.IsNullOrWhiteSpace(userName) || roles == null || roles.Length == 0)
                return false;
            return await employeeAccountDB.ChangeRoleAsync(userName, roles);
        }
        public static async Task<HashSet<string>> GetEmployeeRolesAsync(string userName)
        {
            if (string.IsNullOrWhiteSpace(userName))
                return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var roleNames = await employeeAccountDB.GetRoleNamesAsync(userName);

            return roleNames.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }
        #endregion
        #region Tài khoản khách hàng
        public static async Task<UserAccount?> CustomerAuthorizeAsync(string userName, string password)
        {
            if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
                return null;
            return await customerAccountDB.AuthorizeAsync(userName, password);
        }
        public static async Task<bool> ChangeCustomerPasswordAsync(string userName, string password)
        {
            if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
                return false;
            return await customerAccountDB.ChangePasswordAsync(userName, password);
        }
        #endregion
    }
}