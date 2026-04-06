using SV22T1020427.DataLayers.Interfaces;
using SV22T1020427.DataLayers.SQLServer;
using SV22T1020427.Models.Common;
using SV22T1020427.Models.HR;

namespace SV22T1020427.BusinessLayers
{
    /// <summary>
    /// Cung cấp các chức năng xử lý dữ liệu liên quan đến nhân sự của hệ thống    
    /// </summary>
    public static class HRDataService
    {
        private static readonly IEmployeeRepository employeeDB;

        /// <summary>
        /// Constructor
        /// </summary>
        static HRDataService()
        {
            employeeDB = new EmployeeRepository(Configuration.ConnectionString);
        }

        #region Employee

        /// <summary>
        /// Tìm kiếm và lấy danh sách nhân viên dưới dạng phân trang.
        /// </summary>
        /// <param name="input">
        /// Thông tin tìm kiếm và phân trang (từ khóa tìm kiếm, trang cần hiển thị, số dòng mỗi trang).
        /// </param>
        /// <returns>
        /// Kết quả tìm kiếm dưới dạng danh sách nhân viên có phân trang.
        /// </returns>
        public static async Task<PagedResult<Employee>> ListEmployeesAsync(PaginationSearchInput input)
        {
            return await employeeDB.ListAsync(input);
        }

        /// <summary>
        /// Lấy thông tin chi tiết của một nhân viên dựa vào mã nhân viên.
        /// </summary>
        /// <param name="employeeID">Mã nhân viên cần tìm.</param>
        /// <returns>
        /// Đối tượng Employee nếu tìm thấy, ngược lại trả về null.
        /// </returns>
        public static async Task<Employee?> GetEmployeeAsync(int employeeID)
        {
            return await employeeDB.GetAsync(employeeID);
        }

        /// <summary>
        /// Bổ sung một nhân viên mới vào hệ thống.
        /// </summary>
        /// <param name="data">Thông tin nhân viên cần bổ sung.</param>
        /// <returns>Mã nhân viên được tạo mới.</returns>
        public static async Task<int> AddEmployeeAsync(Employee data)
        {
            if (!ValidateEmployeeData(data, true))
                return 0;
            if (!await ValidateEmployeeEmailAsync(data.Email))
                return 0;

            return await employeeDB.AddAsync(data);
        }

        /// <summary>
        /// Cập nhật thông tin của một nhân viên.
        /// </summary>
        /// <param name="data">Thông tin nhân viên cần cập nhật.</param>
        /// <returns>
        /// True nếu cập nhật thành công, ngược lại False.
        /// </returns>
        public static async Task<bool> UpdateEmployeeAsync(Employee data)
        {
            if (!ValidateEmployeeData(data, false))
                return false;
           // 
            if (!await ValidateEmployeeEmailAsync(data.Email, data.EmployeeID))
                return false;
            return await employeeDB.UpdateAsync(data);
        }

        /// <summary>
        /// Xóa một nhân viên dựa vào mã nhân viên.
        /// </summary>
        /// <param name="employeeID">Mã nhân viên cần xóa.</param>
        /// <returns>
        /// True nếu xóa thành công, False nếu nhân viên đang được sử dụng
        /// hoặc việc xóa không thực hiện được.
        /// </returns>
        public static async Task<bool> DeleteEmployeeAsync(int employeeID,string webRootPath)
        {
            if (await employeeDB.IsUsedAsync(employeeID))
                return false;

            var item = await employeeDB.GetAsync(employeeID);
            var photo = item?.Photo;

            var result = await employeeDB.DeleteAsync(employeeID);
            if (!result)
                return false;

            if (!string.IsNullOrWhiteSpace(photo) && !string.Equals(photo, "nophoto.png", StringComparison.OrdinalIgnoreCase))
            {
           
            
                try
                {
                    var filePath = Path.Combine(webRootPath, "images","employees", photo);
                    if (File.Exists(filePath))
                        File.Delete(filePath);
                }
                catch
                {
                    // Không làm gì nếu xóa file ảnh thất bại
                }
            }

            return true;
        }

        /// <summary>
        /// Kiểm tra xem một nhân viên có đang được sử dụng trong dữ liệu hay không.
        /// </summary>
        /// <param name="employeeID">Mã nhân viên cần kiểm tra.</param>
        /// <returns>
        /// True nếu nhân viên đang được sử dụng, ngược lại False.
        /// </returns>
        public static async Task<bool> IsUsedEmployeeAsync(int employeeID)
        {
            return await employeeDB.IsUsedAsync(employeeID);
        }

        /// <summary>
        /// Kiểm tra xem email của nhân viên có hợp lệ không
        /// (không bị trùng với email của nhân viên khác).
        /// </summary>
        /// <param name="email">Địa chỉ email cần kiểm tra.</param>
        /// <param name="employeeID">
        /// Nếu employeeID = 0: kiểm tra email đối với nhân viên mới.
        /// Nếu employeeID khác 0: kiểm tra email của nhân viên có mã là employeeID.
        /// </param>
        /// <returns>
        /// True nếu email hợp lệ (không trùng), ngược lại False.
        /// </returns>
        public static async Task<bool> ValidateEmployeeEmailAsync(string email, int employeeID = 0)
        {
            return await employeeDB.ValidateEmailAsync(email, employeeID);
        }

        /// <summary>
        /// Kiểm tra dữ liệu nhân viên ở tầng nghiệp vụ.
        /// Chỉ kiểm tra các rule cơ bản, các rule nâng cao có thể bổ sung thêm sau.
        /// </summary>
        /// <param name="data">Nhân viên cần kiểm tra</param>
        /// <param name="isNew">True nếu là thêm mới, False nếu cập nhật</param>
        /// <returns>True nếu dữ liệu hợp lệ, ngược lại False</returns>
        private static bool ValidateEmployeeData(Employee data, bool isNew)
        {
            if (data == null)
                return false;

            // Họ tên bắt buộc
            if (string.IsNullOrWhiteSpace(data.FullName))
                return false;

            // Email bắt buộc
            if (string.IsNullOrWhiteSpace(data.Email))
                return false;

            data.Email = data.Email.Trim();

            // Nếu cập nhật thì ID phải hợp lệ
            if (!isNew && data.EmployeeID <= 0)
                return false;

            // 

            // Ngày sinh: nếu có thì không được lớn hơn ngày hiện tại
            if (data.BirthDate.HasValue && data.BirthDate.Value.Date > DateTime.Today)
                return false;

           
            // Address / Photo / Phone không bắt buộc, chỉ cần trim
            data.Address = data.Address?.Trim();
            data.Photo = data.Photo?.Trim();
            data.Phone = data.Phone?.Trim();

            return true;
        }


        #endregion
    }
}