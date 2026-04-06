using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020427.DataLayers.Interfaces;
using SV22T1020427.Models.DataDictionary;

namespace SV22T1020427.DataLayers.SQLServer
{
    /// <summary>
    /// Cài đặt các phép xử lý dữ liệu cho tỉnh/thành sử dụng SQL Server
    /// </summary>
    public class ProvinceRepository : IDataDictionaryRepository<Province>
    {
        private readonly string _connectionString;

        public ProvinceRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Lấy danh sách tất cả tỉnh/thành, sắp xếp theo tên
        /// </summary>
        public async Task<List<Province>> ListAsync()
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            string sql = "SELECT ProvinceName FROM Provinces ORDER BY ProvinceName";
            return (await connection.QueryAsync<Province>(sql)).ToList();
        }
    }
}