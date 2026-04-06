using SV22T1020427.BusinessLayers;
using SV22T1020427.Models.Sales;

namespace SV22T1020427.Admin
{
    public static class OrderItemHelper
    {
        public static async Task<bool> UpdateOrderItemAsync(int orderId, int productId, int quantity, decimal salePrice)
        {
            var data = new OrderDetail
            {
                OrderID = orderId,
                ProductID = productId,
                Quantity = quantity,
                SalePrice = salePrice
            };

            return await SalesDataService.UpdateDetailAsync(data);
        }

        public static async Task<bool> DeleteOrderItemAsync(int orderId, int productId)
        {
            return await SalesDataService.DeleteDetailAsync(orderId, productId);
        }
        
    }
}