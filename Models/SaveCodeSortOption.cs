namespace SaveCodeClassfication.Models
{
    /// <summary>
    /// ������ ��¥ ���� ����
    /// </summary>
    public enum DateSortDirection
    {
        /// <summary>�ֽż� (��������)</summary>
        Newest,
        /// <summary>�����ȼ� (��������)</summary>
        Oldest
    }

    /// <summary>
    /// ������ ��¥ ���� ����
    /// </summary>
    public class SimpleSortSettings
    {
        public DateSortDirection Direction { get; set; } = DateSortDirection.Newest;
        
        /// <summary>
        /// ���� ������ ǥ�ø��� ��ȯ�մϴ�
        /// </summary>
        public string GetDisplayName()
        {
            return Direction switch
            {
                DateSortDirection.Newest => "�ֽż�",
                DateSortDirection.Oldest => "�����ȼ�",
                _ => "�ֽż�"
            };
        }

        /// <summary>
        /// ���� ������ ����մϴ�
        /// </summary>
        public void ToggleDirection()
        {
            Direction = Direction == DateSortDirection.Newest ? DateSortDirection.Oldest : DateSortDirection.Newest;
        }
    }

    // ���� ������ ���� �ý��� (���Ž� ȣȯ���� ���� ����)
    public enum SortCriteria
    {
        Date,
        FileName,
        Level,
        PhysicalPower,
        MagicalPower,
        SpiritualPower,
        Experience,
        Gold
    }

    public enum SortDirection
    {
        Ascending,
        Descending
    }

    public class SortSettings
    {
        public SortCriteria Criteria { get; set; } = SortCriteria.Date;
        public SortDirection Direction { get; set; } = SortDirection.Descending;
        
        public string GetDisplayName()
        {
            var criteriaName = Criteria switch
            {
                SortCriteria.Date => "��¥",
                SortCriteria.FileName => "���ϸ�",
                SortCriteria.Level => "����",
                SortCriteria.PhysicalPower => "����",
                SortCriteria.MagicalPower => "���",
                SortCriteria.SpiritualPower => "����",
                SortCriteria.Experience => "����ġ",
                SortCriteria.Gold => "��",
                _ => "��¥"
            };

            var directionIcon = Direction == SortDirection.Ascending ? "??" : "??";
            var directionName = Direction == SortDirection.Ascending ? "��������" : "��������";
            
            return $"{directionIcon} {criteriaName} {directionName}";
        }

        public void ToggleDirection()
        {
            Direction = Direction == SortDirection.Ascending ? SortDirection.Descending : SortDirection.Ascending;
        }
    }

    public class SortCriteriaInfo
    {
        public SortCriteria Criteria { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    // ���Ž� ȣȯ��
    public enum SaveCodeSortOption
    {
        DateDescending,
        DateAscending,
        FileNameAscending,
        FileNameDescending,
        LevelDescending,
        LevelAscending,
        PhysicalPowerDescending,
        PhysicalPowerAscending
    }

    public class SortOptionInfo
    {
        public SaveCodeSortOption Option { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
    }
}