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
    }
}