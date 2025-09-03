using System.ComponentModel;

namespace SaveCodeClassfication.Models
{
    /// <summary>
    /// 세이브 코드 정보를 나타내는 모델
    /// </summary>
    public class SaveCodeInfo : INotifyPropertyChanged
    {
        public string CharacterName { get; set; } = string.Empty;
        public string SaveCode { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public DateTime FileDate { get; set; }
        public string FullContent { get; set; } = string.Empty;
        
        // 아이템 정보
        public List<string> Items { get; set; } = new List<string>();
        public string ItemsDisplayText { get; set; } = string.Empty;
        
        // 기본 정보
        public string Level { get; set; } = string.Empty;
        public string Gold { get; set; } = string.Empty;
        public string Wood { get; set; } = string.Empty;
        public string Experience { get; set; } = string.Empty;
        
        // 전투 능력치
        public string PhysicalPower { get; set; } = string.Empty; // 무력
        public string MagicalPower { get; set; } = string.Empty;  // 요력
        public string SpiritualPower { get; set; } = string.Empty; // 영력

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}