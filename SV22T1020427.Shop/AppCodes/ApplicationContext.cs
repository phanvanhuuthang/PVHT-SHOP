using Newtonsoft.Json;

namespace SV22T1020427.Shop
{
    public static class ApplicationContext
    {
        private static IHttpContextAccessor? _httpContextAccessor;
        private static IWebHostEnvironment? _webHostEnvironment;
        private static IConfiguration? _configuration;

        public static void Configure(IHttpContextAccessor httpContextAccessor, IWebHostEnvironment webHostEnvironment, IConfiguration configuration)
        {
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _webHostEnvironment = webHostEnvironment ?? throw new ArgumentNullException(nameof(webHostEnvironment));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public static HttpContext? HttpContext => _httpContextAccessor?.HttpContext;
        public static IWebHostEnvironment? WebHostEnvironment => _webHostEnvironment;
        public static IConfiguration? Configuration => _configuration;

        public static string BaseURL => $"{HttpContext?.Request.Scheme}://{HttpContext?.Request.Host}/";
        public static string WWWRootPath => _webHostEnvironment?.WebRootPath ?? string.Empty;
        public static string ApplicationRootPath => _webHostEnvironment?.ContentRootPath ?? string.Empty;

        public static void SetSessionData(string key, object value)
        {
            try
            {
                string sValue = JsonConvert.SerializeObject(value);
                if (!string.IsNullOrEmpty(sValue))
                {
                    _httpContextAccessor?.HttpContext?.Session.SetString(key, sValue);
                }
            }
            catch { }
        }

        public static T? GetSessionData<T>(string key) where T : class
        {
            try
            {
                string sValue = _httpContextAccessor?.HttpContext?.Session.GetString(key) ?? "";
                if (!string.IsNullOrEmpty(sValue))
                {
                    return JsonConvert.DeserializeObject<T>(sValue);
                }
            }
            catch { }
            return null;
        }

        public static string GetConfigValue(string name)
        {
            return _configuration?[name] ?? "";
        }

        public static int PageSize => Convert.ToInt32(GetConfigValue("PageSize") is string s && s != "" ? s : "12");
    }
}
