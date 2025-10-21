namespace SaveCodeClassfication.Models
{
    /// <summary>
    /// 애플리케이션 설정 정보를 나타내는 모델
    /// </summary>
    public class AppSettings
    {
        public string LastSelectedFolderPath { get; set; } = string.Empty;
        public DateTime LastUpdated { get; set; } = DateTime.Now;
        public bool AutoLoadOnStartup { get; set; } = true;
        public string Version { get; set; } = "1.0.0";
        
        // 간단한 날짜 정렬 설정
        public SimpleSortSettings SimpleSortSettings { get; set; } = new();
        public bool RememberSortOption { get; set; } = true;
        
        // 레거시 호환성
        public SortSettings SortSettings { get; set; } = new();
        public SaveCodeSortOption DefaultSortOption { get; set; } = SaveCodeSortOption.DateDescending;
        
        // 데이터베이스 연결 설정
        public DatabaseSettings DatabaseSettings { get; set; } = new();
        
        // 사용자 정보 (로그인 기능 추가 시 사용)
        public string CurrentUserKey { get; set; } = "default_user";
    }
    
    /// <summary>
    /// 데이터베이스 연결 설정
    /// </summary>
    public class DatabaseSettings
    {
        // 기본값은 로컬 개발 환경용으로 설정
        public string Host { get; set; } = "localhost";
        public int Port { get; set; } = 3306;
        public string Database { get; set; } = "masinsave";
        public string UserId { get; set; } = "root";
        public string Password { get; set; } = "";
        public int ConnectionTimeout { get; set; } = 30;
        public bool UseSSL { get; set; } = false;
        
        /// <summary>
        /// 연결 문자열을 생성합니다
        /// </summary>
        public string GetConnectionString()
        {
            return $"Server={Host};Port={Port};Database={Database};Uid={UserId};Pwd={Password};Connection Timeout={ConnectionTimeout};SslMode={(UseSSL ? "Required" : "None")};";
        }
        
        /// <summary>
        /// 환경변수에서 데이터베이스 설정을 로드합니다
        /// </summary>
        public static DatabaseSettings LoadFromEnvironment()
        {
            return new DatabaseSettings
            {
                Host = Environment.GetEnvironmentVariable("DB_HOST") ?? "localhost",
                Port = int.TryParse(Environment.GetEnvironmentVariable("DB_PORT"), out var port) ? port : 3306,
                Database = Environment.GetEnvironmentVariable("DB_NAME") ?? "masinsave",
                UserId = Environment.GetEnvironmentVariable("DB_USER") ?? "root",
                Password = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "",
                ConnectionTimeout = int.TryParse(Environment.GetEnvironmentVariable("DB_TIMEOUT"), out var timeout) ? timeout : 30,
                UseSSL = bool.TryParse(Environment.GetEnvironmentVariable("DB_USE_SSL"), out var useSSL) && useSSL
            };
        }
    }
}