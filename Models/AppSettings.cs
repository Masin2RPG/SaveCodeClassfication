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
    }
}