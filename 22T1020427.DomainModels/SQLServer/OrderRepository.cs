using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020427.DataLayers.Interfaces;
using SV22T1020427.Models.Common;
using SV22T1020427.Models.Sales;

namespace SV22T1020427.DataLayers.SQLServer
{
    /// <summary>
    /// Cài đặt các phép xử lý dữ liệu cho đơn hàng sử dụng SQL Server
    /// </summary>
    public class OrderRepository : IOrderRepository
    {
        private readonly string _connectionString;

        public OrderRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<PagedResult<OrderViewInfo>> ListAsync(OrderSearchInput input)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var searchParam = string.IsNullOrWhiteSpace(input.SearchValue)
                ? ""
                : $"%{input.SearchValue}%";

            string countSql = @"
                SELECT COUNT(*)
                FROM Orders o
                LEFT JOIN Customers c  ON o.CustomerID  = c.CustomerID
                LEFT JOIN Employees e  ON o.EmployeeID  = e.EmployeeID
                WHERE (@SearchValue = N'' OR c.CustomerName LIKE @SearchValue OR c.Phone LIKE @SearchValue)
                  AND (@Status IN (0, 5) OR o.Status = @Status)
                  AND (@DateFrom IS NULL  OR o.OrderTime >= @DateFrom)
                  AND (@DateTo   IS NULL  OR o.OrderTime <= @DateTo)";

            string dataSql = @"
                SELECT o.*,
                       ISNULL(c.CustomerName, N'') AS CustomerName,
                       ISNULL(c.Phone, N'')        AS CustomerPhone,
                       ISNULL(e.FullName, N'')     AS EmployeeName,
                       ISNULL((SELECT SUM(od.Quantity * od.SalePrice)
                                FROM OrderDetails od
                                WHERE od.OrderID = o.OrderID), 0) AS SumOfPrice
                FROM Orders o
                LEFT JOIN Customers c  ON o.CustomerID  = c.CustomerID
                LEFT JOIN Employees e  ON o.EmployeeID  = e.EmployeeID
                WHERE (@SearchValue = N'' OR c.CustomerName LIKE @SearchValue OR c.Phone LIKE @SearchValue)
                  AND (@Status IN (0, 5) OR o.Status = @Status)
                  AND (@DateFrom IS NULL  OR o.OrderTime >= @DateFrom)
                  AND (@DateTo   IS NULL  OR o.OrderTime <= @DateTo)
                ORDER BY o.OrderTime DESC
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

            var param = new
            {
                SearchValue = searchParam,
                Status = (int)input.Status,
                input.DateFrom,
                input.DateTo,
                Offset = input.Offset,
                input.PageSize
            };

            int rowCount = await connection.ExecuteScalarAsync<int>(countSql, param);
            var dataItems = (await connection.QueryAsync<OrderViewInfo>(dataSql, param)).ToList();

            return new PagedResult<OrderViewInfo>
            {
                Page = input.Page,
                PageSize = input.PageSize,
                RowCount = rowCount,
                DataItems = dataItems
            };
        }

        public async Task<OrderViewInfo?> GetAsync(int orderID)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            string sql = @"
                SELECT o.*,
                       ISNULL(e.FullName, N'')       AS EmployeeName,
                       ISNULL(c.CustomerName, N'')   AS CustomerName,
                       ISNULL(c.ContactName, N'')    AS CustomerContactName,
                       ISNULL(c.Email, N'')          AS CustomerEmail,
                       ISNULL(c.Phone, N'')          AS CustomerPhone,
                       ISNULL(c.Address, N'')        AS CustomerAddress,
                       ISNULL(s.ShipperName, N'')    AS ShipperName,
                       ISNULL(s.Phone, N'')          AS ShipperPhone,
                       ISNULL((SELECT SUM(od.Quantity * od.SalePrice)
                               FROM OrderDetails od
                               WHERE od.OrderID = o.OrderID), 0) AS SumOfPrice
                FROM   Orders o
                LEFT JOIN Customers c ON o.CustomerID = c.CustomerID
                LEFT JOIN Employees e ON o.EmployeeID = e.EmployeeID
                LEFT JOIN Shippers  s ON o.ShipperID  = s.ShipperID
                WHERE  o.OrderID = @OrderID";

            return await connection.QueryFirstOrDefaultAsync<OrderViewInfo>(sql, new { OrderID = orderID });
        }

        public async Task<int> AddAsync(Order data)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            string sql = @"
                INSERT INTO Orders(CustomerID, OrderTime, DeliveryProvince, DeliveryAddress,
                                   EmployeeID, AcceptTime, ShipperID, ShippedTime, FinishedTime, Status)
                VALUES (@CustomerID, @OrderTime, @DeliveryProvince, @DeliveryAddress,
                        @EmployeeID, @AcceptTime, @ShipperID, @ShippedTime, @FinishedTime, @Status);
                SELECT SCOPE_IDENTITY();";

            return await connection.ExecuteScalarAsync<int>(sql, new
            {
                data.CustomerID,
                data.OrderTime,
                data.DeliveryProvince,
                data.DeliveryAddress,
                data.EmployeeID,
                data.AcceptTime,
                data.ShipperID,
                data.ShippedTime,
                data.FinishedTime,
                Status = (int)data.Status
            });
        }

        public async Task<bool> UpdateAsync(Order data)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            string sql = @"
                UPDATE Orders
                SET CustomerID       = @CustomerID,
                    OrderTime        = @OrderTime,
                    DeliveryProvince = @DeliveryProvince,
                    DeliveryAddress  = @DeliveryAddress,
                    EmployeeID       = @EmployeeID,
                    AcceptTime       = @AcceptTime,
                    ShipperID        = @ShipperID,
                    ShippedTime      = @ShippedTime,
                    FinishedTime     = @FinishedTime,
                    Status           = @Status
                WHERE OrderID = @OrderID";

            int rowsAffected = await connection.ExecuteAsync(sql, new
            {
                data.CustomerID,
                data.OrderTime,
                data.DeliveryProvince,
                data.DeliveryAddress,
                data.EmployeeID,
                data.AcceptTime,
                data.ShipperID,
                data.ShippedTime,
                data.FinishedTime,
                Status = (int)data.Status,
                data.OrderID
            });

            return rowsAffected > 0;
        }

        public async Task<bool> DeleteAsync(int orderID)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            // Xóa chi tiết đơn hàng trước, sau đó xóa đơn hàng
            string sql = @"
                DELETE FROM OrderDetails WHERE OrderID = @OrderID;
                DELETE FROM Orders        WHERE OrderID = @OrderID;";

            int rowsAffected = await connection.ExecuteAsync(sql, new { OrderID = orderID });
            return rowsAffected > 0;
        }

        // ── Order Details ─────────────────────────────────────────────────────────

        public async Task<List<OrderDetailViewInfo>> ListDetailsAsync(int orderID)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            string sql = @"
                SELECT od.*,
                       ISNULL(p.ProductName, N'') AS ProductName,
                       ISNULL(p.Unit, N'')        AS Unit,
                       ISNULL(p.Photo, N'')       AS Photo
                FROM   OrderDetails od
                LEFT JOIN Products p ON od.ProductID = p.ProductID
                WHERE  od.OrderID = @OrderID
                ORDER BY p.ProductName";

            return (await connection.QueryAsync<OrderDetailViewInfo>(sql, new { OrderID = orderID })).ToList();
        }

        public async Task<OrderDetailViewInfo?> GetDetailAsync(int orderID, int productID)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            string sql = @"
                SELECT od.*,
                       ISNULL(p.ProductName, N'') AS ProductName,
                       ISNULL(p.Unit, N'')        AS Unit,
                       ISNULL(p.Photo, N'')       AS Photo
                FROM   OrderDetails od
                LEFT JOIN Products p ON od.ProductID = p.ProductID
                WHERE  od.OrderID = @OrderID AND od.ProductID = @ProductID";

            return await connection.QueryFirstOrDefaultAsync<OrderDetailViewInfo>(sql,
                new { OrderID = orderID, ProductID = productID });
        }

        public async Task<bool> AddDetailAsync(OrderDetail data)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            string sql = @"
                IF EXISTS (SELECT 1 FROM OrderDetails WHERE OrderID = @OrderID AND ProductID = @ProductID)
                    UPDATE OrderDetails
                    SET Quantity  = Quantity + @Quantity,
                        SalePrice = @SalePrice
                    WHERE OrderID = @OrderID AND ProductID = @ProductID
                ELSE
                    INSERT INTO OrderDetails(OrderID, ProductID, Quantity, SalePrice)
                    VALUES (@OrderID, @ProductID, @Quantity, @SalePrice)";

            int rowsAffected = await connection.ExecuteAsync(sql, new
            {
                data.OrderID,
                data.ProductID,
                data.Quantity,
                data.SalePrice
            });

            return rowsAffected > 0;
        }

        public async Task<bool> UpdateDetailAsync(OrderDetail data)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            string sql = @"
                UPDATE OrderDetails
                SET Quantity  = @Quantity,
                    SalePrice = @SalePrice
                WHERE OrderID = @OrderID AND ProductID = @ProductID";

            int rowsAffected = await connection.ExecuteAsync(sql, new
            {
                data.Quantity,
                data.SalePrice,
                data.OrderID,
                data.ProductID
            });

            return rowsAffected > 0;
        }

        public async Task<bool> DeleteDetailAsync(int orderID, int productID)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            string sql = "DELETE FROM OrderDetails WHERE OrderID = @OrderID AND ProductID = @ProductID";
            int rowsAffected = await connection.ExecuteAsync(sql, new { OrderID = orderID, ProductID = productID });
            return rowsAffected > 0;
        }

        public async Task<decimal> GetTodayRevenueAsync()
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            string sql = @"
                SELECT ISNULL(SUM(od.Quantity * od.SalePrice), 0)
                FROM Orders o
                INNER JOIN OrderDetails od ON o.OrderID = od.OrderID
                WHERE o.Status = @CompletedStatus
                  AND o.FinishedTime IS NOT NULL
                  AND CAST(o.FinishedTime AS date) = CAST(GETDATE() AS date)";

            return await connection.ExecuteScalarAsync<decimal>(sql, new
            {
                CompletedStatus = (int)OrderStatusEnum.Completed
            });
        }

        public async Task<List<OrderMonthlyRevenue>> GetMonthlyRevenueAsync(int year)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            string sql = @"
                WITH Months AS
                (
                    SELECT 1 AS [Month] UNION ALL SELECT 2 UNION ALL SELECT 3 UNION ALL
                    SELECT 4 UNION ALL SELECT 5 UNION ALL SELECT 6 UNION ALL
                    SELECT 7 UNION ALL SELECT 8 UNION ALL SELECT 9 UNION ALL
                    SELECT 10 UNION ALL SELECT 11 UNION ALL SELECT 12
                )
                SELECT m.[Month],
                       ISNULL(r.Revenue, 0) AS Revenue
                FROM Months m
                LEFT JOIN
                (
                    SELECT MONTH(o.FinishedTime) AS [Month],
                           SUM(od.Quantity * od.SalePrice) AS Revenue
                    FROM Orders o
                    INNER JOIN OrderDetails od ON o.OrderID = od.OrderID
                    WHERE o.Status = @CompletedStatus
                      AND o.FinishedTime IS NOT NULL
                      AND YEAR(o.FinishedTime) = @Year
                    GROUP BY MONTH(o.FinishedTime)
                ) r ON m.[Month] = r.[Month]
                ORDER BY m.[Month]";

            return (await connection.QueryAsync<OrderMonthlyRevenue>(sql, new
            {
                Year = year,
                CompletedStatus = (int)OrderStatusEnum.Completed
            })).ToList();
        }

        public async Task<List<TopSellingProductInfo>> GetTopSellingProductsAsync(int topN)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            string sql = @"
                SELECT TOP (@TopN)
                       od.ProductID,
                       ISNULL(p.ProductName, N'') AS ProductName,
                       SUM(od.Quantity) AS Quantity
                FROM OrderDetails od
                INNER JOIN Orders o ON o.OrderID = od.OrderID
                LEFT JOIN Products p ON p.ProductID = od.ProductID
                WHERE o.Status NOT IN (-2, -1)
                GROUP BY od.ProductID, p.ProductName
                ORDER BY SUM(od.Quantity) DESC, p.ProductName";

            return (await connection.QueryAsync<TopSellingProductInfo>(sql, new { TopN = topN })).ToList();
        }

        public async Task<List<OrderViewInfo>> ListProcessingOrdersAsync(int topN)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            string sql = @"
                SELECT TOP (@TopN)
                       o.*,
                       ISNULL(c.CustomerName, N'') AS CustomerName,
                       ISNULL(c.Phone, N'')        AS CustomerPhone,
                       ISNULL(e.FullName, N'')     AS EmployeeName,
                       ISNULL((SELECT SUM(od.Quantity * od.SalePrice)
                                FROM OrderDetails od
                                WHERE od.OrderID = o.OrderID), 0) AS SumOfPrice
                FROM Orders o
                LEFT JOIN Customers c ON o.CustomerID = c.CustomerID
                LEFT JOIN Employees e ON o.EmployeeID = e.EmployeeID
                WHERE o.Status IN (1, 2, 3)
                ORDER BY o.OrderTime DESC";

            return (await connection.QueryAsync<OrderViewInfo>(sql, new { TopN = topN })).ToList();
        }

        public Task<List<OrderDetailViewInfo>> ListDetailAsync(int orderID)
        {
            throw new NotImplementedException();
        }
    }
}
