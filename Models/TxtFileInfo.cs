using System.ComponentModel;

namespace SaveCodeClassfication.Models
{
    /// <summary>
    /// TXT ���� ������ ��Ÿ���� ��
    /// </summary>
    public class TxtFileInfo : INotifyPropertyChanged
    {
        public string FilePath { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string FileSizeText { get; set; } = string.Empty;
        public string RelativePath { get; set; } = string.Empty;

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}