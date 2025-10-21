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
    /// �� ��� ����
    /// </summary>
    public enum ViewMode
    {
        /// <summary>ĳ���ͺ� ����</summary>
        ByCharacter,
        /// <summary>������ �׷�ȭ ����</summary>
        ByJobGroup
    }

    /// <summary>
    /// ������ ��¥ ���� ����
    /// </summary>
    public class SimpleSortSettings
    {
        public DateSortDirection Direction { get; set; } = DateSortDirection.Newest;
        public ViewMode ViewMode { get; set; } = ViewMode.ByCharacter;
        
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
        /// �� ����� ǥ�ø��� ��ȯ�մϴ�
        /// </summary>
        public string GetViewModeDisplayName()
        {
            return ViewMode switch
            {
                ViewMode.ByCharacter => "ĳ���ͺ�",
                ViewMode.ByJobGroup => "������ �׷�",
                _ => "ĳ���ͺ�"
            };
        }

        /// <summary>
        /// ���� ������ ����մϴ�
        /// </summary>
        public void ToggleDirection()
        {
            Direction = Direction == DateSortDirection.Newest ? DateSortDirection.Oldest : DateSortDirection.Newest;
        }

        /// <summary>
        /// �� ��带 ����մϴ�
        /// </summary>
        public void ToggleViewMode()
        {
            ViewMode = ViewMode == ViewMode.ByCharacter ? ViewMode.ByJobGroup : ViewMode.ByCharacter;
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

            var directionIcon = Direction == SortDirection.Ascending ? "��" : "��";
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