using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020427.DataLayers.Interfaces;
using SV22T1020427.Models.Common;
using SV22T1020427.Models.Partner;

namespace SV22T1020427.DataLayers.SQLServer
{
    /// <summary>
    /// Cài đặt các phép xử lý dữ liệu cho người giao hàng sử dụng SQL Server
    /// </summary>
    public class ShipperRepository : IGenericRepository<Shipper>
    {
        private readonly string _connectionString;

        public ShipperRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<PagedResult<Shipper>> ListAsync(PaginationSearchInput input)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            string countSql = @"
                SELECT COUNT(*) FROM Shippers
                WHERE (@SearchValue = N'' OR ShipperName LIKE @SearchValue)";

            string dataSql = input.PageSize > 0 
                ? @"
                SELECT * FROM Shippers
                WHERE (@SearchValue = N'' OR ShipperName LIKE @SearchValue)
                ORDER BY ShipperName
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY"
                : @"
                    SELECT * FROM Shippers
                WHERE (@SearchValue = N'' OR ShipperName LIKE @SearchValue)
                ORDER BY ShipperName";

            var searchParam = string.IsNullOrWhiteSpace(input.SearchValue)
                ? ""
                : $" %{input.SearchValue}%";

            var param = new
            {
                SearchValue = searchParam,
                Offset = input.Offset,
                PageSize = input.PageSize
            };

            int rowCount = await connection.ExecuteScalarAsync<int>(countSql, param);
            var dataItems = (await connection.QueryAsync<Shipper>(dataSql, param)).ToList();

            return new PagedResult<Shipper>
            {
                Page = input.Page,
                PageSize = input.PageSize,
                RowCount = rowCount,
                DataItems = dataItems
            };
        }

        public async Task<Shipper?> GetAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            string sql = "SELECT * FROM Shippers WHERE ShipperID = @ShipperID";
            return await connection.QueryFirstOrDefaultAsync<Shipper>(sql, new { ShipperID = id });
        }

        public async Task<int> AddAsync(Shipper data)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            string sql = @"
                INSERT INTO Shippers(ShipperName, Phone)
                VALUES (@ShipperName, @Phone);
                SELECT SCOPE_IDENTITY();";

            return await connection.ExecuteScalarAsync<int>(sql, new
            {
                data.ShipperName,
                data.Phone
            });
        }

        public async Task<bool> UpdateAsync(Shipper data)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            string sql = @"
                UPDATE Shippers
                SET ShipperName = @ShipperName,
                    Phone       = @Phone
                WHERE ShipperID = @ShipperID";

            int rowsAffected = await connection.ExecuteAsync(sql, new
            {
                data.ShipperName,
                data.Phone,
                data.ShipperID
            });

            return rowsAffected > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            string sql = "DELETE FROM Shippers WHERE ShipperID = @ShipperID";
            int rowsAffected = await connection.ExecuteAsync(sql, new { ShipperID = id });
            return rowsAffected > 0;
        }

        public async Task<bool> IsUsedAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            string sql = "SELECT COUNT(*) FROM Orders WHERE ShipperID = @ShipperID";
            int count = await connection.ExecuteScalarAsync<int>(sql, new { ShipperID = id });
            return count > 0;
        }
    }
}