using System.Collections.ObjectModel;
using System.ComponentModel;

namespace SaveCodeClassfication.Models
{
    /// <summary>
    /// ĳ���� ������ ��Ÿ���� ��
    /// </summary>
    public class CharacterInfo : INotifyPropertyChanged
    {
        public string CharacterName { get; set; } = string.Empty;
        public string OriginalCharacterName { get; set; } = string.Empty;
        public ObservableCollection<SaveCodeInfo> SaveCodes { get; set; } = new();
        public string SaveCodeCount { get; set; } = string.Empty;
        public string LastModified { get; set; } = string.Empty;

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}