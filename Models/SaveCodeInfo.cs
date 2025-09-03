using System.ComponentModel;

namespace SaveCodeClassfication.Models
{
    /// <summary>
    /// ���̺� �ڵ� ������ ��Ÿ���� ��
    /// </summary>
    public class SaveCodeInfo : INotifyPropertyChanged
    {
        public string CharacterName { get; set; } = string.Empty;
        public string SaveCode { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public DateTime FileDate { get; set; }
        public string FullContent { get; set; } = string.Empty;
        
        // ������ ����
        public List<string> Items { get; set; } = new List<string>();
        public string ItemsDisplayText { get; set; } = string.Empty;
        
        // �⺻ ����
        public string Level { get; set; } = string.Empty;
        public string Gold { get; set; } = string.Empty;
        public string Wood { get; set; } = string.Empty;
        public string Experience { get; set; } = string.Empty;
        
        // ���� �ɷ�ġ
        public string PhysicalPower { get; set; } = string.Empty; // ����
        public string MagicalPower { get; set; } = string.Empty;  // ���
        public string SpiritualPower { get; set; } = string.Empty; // ����

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}