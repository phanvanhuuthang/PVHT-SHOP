using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SV22T1020427.BusinessLayers
{
    /// <summary>
    /// Lớp lưu giữ thông tin cấu hình sử dụng trong Business Layer
    /// </summary>
    public static class Configuration
    {
        private static string _connectionString = "";
        /// <summary>
        ///  Khởi tạo cấu hình cho Business Layer, Gọi hàm trước khi chạy ứng dụng
        /// </summary>
        /// <param name="connectionString"></param>
        public static void Initialize(string connectionString)
        {
            _connectionString = connectionString;
        }
        /// <summary>
        ///  Lấy chuỗi tham số kết nối đến CSDL
        /// </summary>
        public static string ConnectionString => _connectionString;
    }
}
