using System.Collections.ObjectModel;
using System.ComponentModel;

namespace SaveCodeClassfication.Models
{
    /// <summary>
    /// 직업별 그룹 정보를 담는 클래스
    /// </summary>
    public class JobGroupInfo : INotifyPropertyChanged
    {
        public string JobClass { get; set; } = string.Empty;
        public string JobDisplayName { get; set; } = string.Empty;
        public ObservableCollection<CharacterInfo> Characters { get; set; } = new();
        public string CharacterCount { get; set; } = string.Empty;
        public string TotalSaveCodeCount { get; set; } = string.Empty;
        public string LastModified { get; set; } = string.Empty;
        public bool IsExpanded { get; set; } = false; // 그룹 확장/축소 상태

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}