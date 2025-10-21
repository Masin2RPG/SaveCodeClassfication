namespace SaveCodeClassfication.Models
{
    /// <summary>
    /// ���ø����̼� ���� ������ ��Ÿ���� ��
    /// </summary>
    public class AppSettings
    {
        public string LastSelectedFolderPath { get; set; } = string.Empty;
        public DateTime LastUpdated { get; set; } = DateTime.Now;
        public bool AutoLoadOnStartup { get; set; } = true;
        public string Version { get; set; } = "1.0.0";
        
        // ������ ��¥ ���� ����
        public SimpleSortSettings SimpleSortSettings { get; set; } = new();
        public bool RememberSortOption { get; set; } = true;
        
        // ���Ž� ȣȯ��
        public SortSettings SortSettings { get; set; } = new();
        public SaveCodeSortOption DefaultSortOption { get; set; } = SaveCodeSortOption.DateDescending;
        
        // �����ͺ��̽� ���� ����
        public DatabaseSettings DatabaseSettings { get; set; } = new();
        
        // ����� ���� (�α��� ��� �߰� �� ���)
        public string CurrentUserKey { get; set; } = "default_user";
    }
    
    /// <summary>
    /// �����ͺ��̽� ���� ����
    /// </summary>
    public class DatabaseSettings
    {
        // �⺻���� ���� ���� ȯ������� ����
        public string Host { get; set; } = "localhost";
        public int Port { get; set; } = 3306;
        public string Database { get; set; } = "masinsave";
        public string UserId { get; set; } = "root";
        public string Password { get; set; } = "";
        public int ConnectionTimeout { get; set; } = 30;
        public bool UseSSL { get; set; } = false;
        
        /// <summary>
        /// ���� ���ڿ��� �����մϴ�
        /// </summary>
        public string GetConnectionString()
        {
            return $"Server={Host};Port={Port};Database={Database};Uid={UserId};Pwd={Password};Connection Timeout={ConnectionTimeout};SslMode={(UseSSL ? "Required" : "None")};";
        }
        
        /// <summary>
        /// ȯ�溯������ �����ͺ��̽� ������ �ε��մϴ�
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