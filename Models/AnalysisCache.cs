namespace SaveCodeClassfication.Models
{
    /// <summary>
    /// 분석 결과 캐시 정보를 나타내는 모델
    /// </summary>
    public class AnalysisCache
    {
        public string FolderPath { get; set; } = string.Empty;
        public DateTime LastAnalyzed { get; set; } = DateTime.Now;
        public Dictionary<string, DateTime> FileHashes { get; set; } = new Dictionary<string, DateTime>();
        public List<SaveCodeInfo> SaveCodes { get; set; } = new List<SaveCodeInfo>();
        public int TotalFiles { get; set; }
        public string Version { get; set; } = "1.0.0";
    }
}