namespace SV22T1020427.Models.Sales
{
    /// <summary>
    /// Thông tin s?n ph?m bán ch?y.
    /// </summary>
    public class TopSellingProductInfo
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; } = "";
        public int Quantity { get; set; }
    }
}
