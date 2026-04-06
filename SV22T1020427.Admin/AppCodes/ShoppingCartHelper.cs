using SV22T1020427.Models.Sales;

namespace SV22T1020427.Admin
{
    /// <summary>
    /// Lớp cung cấp các chức năng xử lý trên giỏ hàng
    /// Giỏ hàng được lưu trong session
    /// </summary>
    /// 
    public static class ShoppingCartHelper
    {
        private const string CART = "ShoppingCart";
        /// <summary>
        /// Lấy giỏ hàng từ session, nếu chưa có thì tạo mới
        /// </summary>
        /// <returns></returns>
        public static List<OrderDetailViewInfo> GetShoppingCart()
        {
            var cart = ApplicationContext.GetSessionData<List<OrderDetailViewInfo>>(CART);
            if (cart == null)
            {
                cart = new List<OrderDetailViewInfo>();
                ApplicationContext.SetSessionData(CART, cart);
            }
            return cart;
        }
        /// <summary>
        /// Lấy thông tin 1 mặt hàng từ giỏ hàng
        /// </summary>
        /// <param name="productId"></param>
        /// <returns></returns>
        public static OrderDetailViewInfo? GetCartItem(int productId)
        {
            var cart = GetShoppingCart();
            var item = cart.Find(m => m.ProductID == productId);
            return item;
        }

        /// <summary>
        /// Thêm mặt hàng vào giỏ hàng
        /// </summary>
        /// <param name="item"></param>
        public static void AddItemToCart(OrderDetailViewInfo item)
        {
            var cart = GetShoppingCart();
            var existingItem = cart.Find(m => m.ProductID == item.ProductID);
            if (existingItem == null)
            {
                cart.Add(item);
            }
            else
            {
                existingItem.Quantity += item.Quantity;
                existingItem.SalePrice = item.SalePrice;
            }
            ApplicationContext.SetSessionData(CART, cart);
        }
        /// <summary>
        /// Cập nhật giá và số lượng mặt hàng trong giỏ
        /// </summary>
        /// <param name="productID"></param>
        /// <param name="quantity"></param>
        /// <param name="salePrice"></param>
        public static void UpdateItemInCart(int productID, int quantity, decimal salePrice)
        {
           var cart = GetShoppingCart();
            var item = cart.Find(m => m.ProductID == productID);
            if (item != null)
            {
                item.Quantity = quantity;
                item.SalePrice = salePrice;
                ApplicationContext.SetSessionData(CART, cart);
            }
        }
        /// <summary>
        /// Xóa mặt hàng ra khỏi giỏ
        /// </summary>
        /// <param name="productID"></param>
        public static void RemoveItemFromCart(int productID)
        {
            var cart = GetShoppingCart();
            int index = cart.FindIndex(m => m.ProductID == productID);
            if (index >= 0)
            {
                cart.RemoveAt(index);
                ApplicationContext.SetSessionData(CART, cart);
            }
        }
        // Xóa giỏ hàng 
        public static void ClearCart()
        {
            var newCart = new List<OrderDetailViewInfo>();
            ApplicationContext.SetSessionData(CART, newCart);
        }
    }
}
