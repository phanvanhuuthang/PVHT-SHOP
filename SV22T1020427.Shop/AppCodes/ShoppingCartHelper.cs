using SV22T1020427.Models.Sales;

namespace SV22T1020427.Shop
{
    public static class ShoppingCartHelper
    {
        private const string CART = "ShoppingCart";

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

        public static int GetCartItemCount()
        {
            return GetShoppingCart().Sum(i => i.Quantity);
        }

        public static OrderDetailViewInfo? GetCartItem(int productId)
        {
            return GetShoppingCart().Find(m => m.ProductID == productId);
        }

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

        public static void ClearCart()
        {
            ApplicationContext.SetSessionData(CART, new List<OrderDetailViewInfo>());
        }
    }
}
