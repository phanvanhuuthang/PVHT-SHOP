namespace SV22T1020427.Admin
{
    /// <summary>
    /// Lớp biểu diễn kết quả khi gọi API
    /// </summary>
    public class ApiResult
    {
        public ApiResult(int code,string message) {
            Code = code;
            Message = message;
        }
        /// <summary>
        /// 0:Lỗi hoặc không thành công, lớn hơn 0: thành công
        /// </summary>
        public int Code { get; set; }
        /// <summary>
        /// Thông báo lỗi(Nếu có)
        /// </summary>
        public string Message { get; set; } = "";
    }
}
