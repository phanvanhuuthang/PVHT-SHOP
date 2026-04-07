
using SV22T1020427.DataLayers.Interfaces;
using SV22T1020427.DataLayers.SQLServer;
using SV22T1020427.Models.Common;
using SV22T1020427.Models.HR;
using SV22T1020427.Models.Sales;

namespace SV22T1020427.BusinessLayers
{
    /// <summary>
    /// Cung cấp các chức năng xử lý dữ liệu liên quan đến bán hàng
    /// bao gồm: đơn hàng (Order) và chi tiết đơn hàng (OrderDetail).
    /// </summary>
    public static class SalesDataService
    {
        private static readonly IOrderRepository orderDB;

        /// <summary>
        /// Constructor
        /// </summary>
        static SalesDataService()
        {
            orderDB = new OrderRepository(Configuration.ConnectionString);
        }

        #region Order

        /// <summary>
        /// Tìm kiếm và lấy danh sách đơn hàng dưới dạng phân trang
        /// </summary>
        public static async Task<PagedResult<OrderViewInfo>> ListOrdersAsync(OrderSearchInput input)
        {
            return await orderDB.ListAsync(input);
        }

        /// <summary>
        /// Lấy thông tin chi tiết của một đơn hàng
        /// </summary>
        public static async Task<OrderViewInfo?> GetOrderAsync(int orderID)
        {
            return await orderDB.GetAsync(orderID);
        }

        /// <summary>
        /// Tạo đơn hàng mới (Bug nguy hiểm)
        /// </summary>
        /// Muốn đúng business thì không nên cho phép khách hàng tự chọn trạng thái đơn hàng khi tạo, mà mặc định là New.
        /// chỉ truyền vào int customerID, deliveryProvince, deliveryAddress, còn lại do hệ thống tự xử lý.
        public static async Task<int> AddOrderAsync(int customerID, string deliveryProvince, string deliveryAddress)
        {

            var data = new Order
            {
                CustomerID = customerID,
                DeliveryProvince = deliveryProvince,
                DeliveryAddress = deliveryAddress
            };
            if (!ValidateOrderData(data, isNew: true))
                return 0;

            data.Status = OrderStatusEnum.New;
            data.OrderTime = DateTime.Now;
            data.AcceptTime = null;
            data.ShippedTime = null;
            data.FinishedTime = null;
            data.EmployeeID = null;
            data.ShipperID = null;

            return await orderDB.AddAsync(data);
        }

        /// <summary>
        /// Cập nhật thông tin đơn hàng (không thay đổi trạng thái).
        /// Sai vì không được phép cập nhật toàn bộ thông tin đơn hàng, mà chỉ được phép cập nhật một số trường nhất định (ví dụ: địa chỉ giao hàng).
        /// Chỉ cho phép cập nhật khi đơn hàng còn ở trạng thái New hoặc Accepted.
        /// </summary>
        public static async Task<bool> UpdateOrderAsync(Order data)
        {
            if (!ValidateOrderData(data, isNew: false))
                return false;

            var existing = await orderDB.GetAsync(data.OrderID);
            if (existing == null)
                return false;

            // Chỉ cho phép sửa đơn khi chưa giao (New hoặc Accepted)
            if (existing.Status != OrderStatusEnum.New &&
                existing.Status != OrderStatusEnum.Accepted)
                return false;

            // Không cho phép sửa trực tiếp trạng thái qua hàm này
            data.Status = existing.Status;
            data.OrderTime = existing.OrderTime;
            data.AcceptTime = existing.AcceptTime;
            data.ShippedTime = existing.ShippedTime;
            data.FinishedTime = existing.FinishedTime;
            data.EmployeeID = existing.EmployeeID;
            data.ShipperID = existing.ShipperID;

            return await orderDB.UpdateAsync(data);
        }

        /// <summary>
        /// Xóa đơn hàng (cùng toàn bộ chi tiết).
        /// Chỉ cho phép xóa khi đơn hàng còn trạng thái New (chưa duyệt).
        /// </summary>
        public static async Task<bool> DeleteOrderAsync(int orderID)
        {
            var order = await orderDB.GetAsync(orderID);
            if (order == null)
                return false;

            // Đơn hàng đã duyệt / đang giao / hoàn tất / hủy / từ chối không được xóa
            if (order.Status != OrderStatusEnum.New)
                return false;

            return await orderDB.DeleteAsync(orderID);
        }

        #endregion

        #region Order Status Processing

        /// <summary>
        /// Duyệt đơn hàng (New -> Accepted)
        /// </summary>
        public static async Task<bool> AcceptOrderAsync(int orderID, int employeeID)
        {
            var order = await orderDB.GetAsync(orderID);
            if (order == null)
                return false;

            if (order.Status != OrderStatusEnum.New)
                return false;

            order.EmployeeID = employeeID;
            order.AcceptTime = DateTime.Now;
            order.Status = OrderStatusEnum.Accepted;

            return await orderDB.UpdateAsync(order);
        }

        /// <summary>
        /// Từ chối đơn hàng (New -> Rejected)
        /// </summary>
        public static async Task<bool> RejectOrderAsync(int orderID, int employeeID)
        {
            var order = await orderDB.GetAsync(orderID);
            if (order == null)
                return false;

            if (order.Status != OrderStatusEnum.New)
                return false;

            order.EmployeeID = employeeID;
            order.FinishedTime = DateTime.Now;
            order.Status = OrderStatusEnum.Rejected;

            return await orderDB.UpdateAsync(order);
        }

        /// <summary>
        /// Hủy đơn hàng (New/Accepted/Shipping -> Cancelled)
        /// </summary>
        public static async Task<bool> CancelOrderAsync(int orderID,int employeeID)
        {
            var order = await orderDB.GetAsync(orderID);
            if (order == null)
                return false;

            // Chỉ được hủy khi đơn hàng còn mới hoặc đã duyệt, chưa giao
            if (order.Status != OrderStatusEnum.New &&
                order.Status != OrderStatusEnum.Accepted &&
                order.Status != OrderStatusEnum.Shipping)
                return false;

            order.FinishedTime = DateTime.Now;
            order.Status = OrderStatusEnum.Cancelled;
            order.EmployeeID = employeeID;
            return await orderDB.UpdateAsync(order);
        }

        /// <summary>
        /// Giao đơn hàng cho người giao hàng (Accepted -> Shipping)
        /// </summary>
        public static async Task<bool> ShipOrderAsync(int orderID, int shipperID)
        {
            var order = await orderDB.GetAsync(orderID);
            if (order == null)
                return false;

            if (order.Status != OrderStatusEnum.Accepted)
                return false;

            order.ShipperID = shipperID;
            order.ShippedTime = DateTime.Now;
            order.Status = OrderStatusEnum.Shipping;

            return await orderDB.UpdateAsync(order);
        }

        /// <summary>
        /// Hoàn tất đơn hàng (Accept/Shipping -> Completed)
        /// </summary>
        public static async Task<bool> CompleteOrderAsync(int orderID)
        {
            var order = await orderDB.GetAsync(orderID);
            if (order == null)
                return false;

            if (order.Status != OrderStatusEnum.Shipping &&
                order.Status != OrderStatusEnum.Accepted)
                return false;

            order.FinishedTime = DateTime.Now;
            order.Status = OrderStatusEnum.Completed;

            return await orderDB.UpdateAsync(order);
        }

        #endregion

        #region Order Detail

        public static async Task<List<OrderDetailViewInfo>> ListDetailsAsync(int orderID)
        {
            return await orderDB.ListDetailsAsync(orderID);
        }

        public static async Task<OrderDetailViewInfo?> GetDetailAsync(int orderID, int productID)
        {
            return await orderDB.GetDetailAsync(orderID, productID);
        }

        /// <summary>
        /// Thêm mặt hàng vào đơn hàng.
        /// Chỉ cho phép khi đơn hàng đang ở trạng thái New hoặc Accepted.
        /// </summary>
        public static async Task<bool> AddDetailAsync(OrderDetail data)
        {
            if (!ValidateOrderDetailData(data))
                return false;

            var order = await orderDB.GetAsync(data.OrderID);
            if (order == null)
                return false;

            if (order.Status != OrderStatusEnum.New &&
                order.Status != OrderStatusEnum.Accepted)
                return false;

            return await orderDB.AddDetailAsync(data);
        }

        /// <summary>
        /// Cập nhật mặt hàng trong đơn hàng.
        /// Chỉ cho phép khi đơn hàng đang ở trạng thái New hoặc Accepted.
        /// </summary>
        public static async Task<bool> UpdateDetailAsync(OrderDetail data)
        {
            if (!ValidateOrderDetailData(data))
                return false;

            var order = await orderDB.GetAsync(data.OrderID);
            if (order == null)
                return false;

            if (order.Status != OrderStatusEnum.New &&
                order.Status != OrderStatusEnum.Accepted)
                return false;

            return await orderDB.UpdateDetailAsync(data);
        }

        /// <summary>
        /// Xóa mặt hàng khỏi đơn hàng.
        /// Chỉ cho phép khi đơn hàng đang ở trạng thái New hoặc Accepted.
        /// </summary>
        public static async Task<bool> DeleteDetailAsync(int orderID, int productID)
        {
            var order = await orderDB.GetAsync(orderID);
            if (order == null)
                return false;

            if (order.Status != OrderStatusEnum.New &&
                order.Status != OrderStatusEnum.Accepted)
                return false;

            return await orderDB.DeleteDetailAsync(orderID, productID);
        }

        #endregion

        #region Validation helpers

        /// <summary>
        /// Kiểm tra dữ liệu đơn hàng ở tầng nghiệp vụ.
        /// </summary>
        private static bool ValidateOrderData(Order data, bool isNew)
        {
            if (data == null)
                return false;

            if (!isNew && data.OrderID <= 0)
                return false;

            // Có khách hàng thì CustomerID > 0
            if (data.CustomerID.HasValue && data.CustomerID <= 0)
                return false;
            if (!data.CustomerID.HasValue || data.CustomerID <= 0)
                return false;

            // Địa chỉ giao hàng: nếu có thì trim
            data.DeliveryProvince = data.DeliveryProvince?.Trim();
            if (string.IsNullOrWhiteSpace(data.DeliveryProvince))
                return false;
            data.DeliveryAddress = data.DeliveryAddress?.Trim();

            // Đơn giản: yêu cầu phải có địa chỉ giao hàng (tùy bài toán)
            if (string.IsNullOrWhiteSpace(data.DeliveryAddress))
                return false;

            return true;
        }

        /// <summary>
        /// Kiểm tra dữ liệu chi tiết đơn hàng.
        /// </summary>
        private static bool ValidateOrderDetailData(OrderDetail data)
        {
            if (data == null)
                return false;

            if (data.OrderID <= 0 || data.ProductID <= 0)
                return false;

            if (data.Quantity <= 0)
                return false;

            if (data.SalePrice < 0)
                return false;

            return true;
        }

        public static Task<decimal> GetTodayRevenueAsync()
        {
            return orderDB.GetTodayRevenueAsync();
        }

        public static Task<List<OrderMonthlyRevenue>> GetMonthlyRevenueAsync(int year)
        {
            return orderDB.GetMonthlyRevenueAsync(year);
        }

        public static Task<List<TopSellingProductInfo>> GetTopSellingProductsAsync(int top)
        {
            return orderDB.GetTopSellingProductsAsync(top);
        }

        public static Task<List<OrderViewInfo>> ListProcessingOrdersAsync(int topN)
        {
            return orderDB.ListProcessingOrdersAsync(topN);
        }

        #endregion
    }
}