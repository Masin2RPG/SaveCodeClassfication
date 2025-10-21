using System.Collections.ObjectModel;
using System.ComponentModel;

namespace SaveCodeClassfication.Models
{
    /// <summary>
    /// ������ �׷� ������ ��� Ŭ����
    /// </summary>
    public class JobGroupInfo : INotifyPropertyChanged
    {
        public string JobClass { get; set; } = string.Empty;
        public string JobDisplayName { get; set; } = string.Empty;
        public ObservableCollection<CharacterInfo> Characters { get; set; } = new();
        public string CharacterCount { get; set; } = string.Empty;
        public string TotalSaveCodeCount { get; set; } = string.Empty;
        public string LastModified { get; set; } = string.Empty;
        public bool IsExpanded { get; set; } = false; // �׷� Ȯ��/��� ����

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}