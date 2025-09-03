namespace SaveCodeClassfication.Models
{
    /// <summary>
    /// 간단한 날짜 정렬 방향
    /// </summary>
    public enum DateSortDirection
    {
        /// <summary>최신순 (내림차순)</summary>
        Newest,
        /// <summary>오래된순 (오름차순)</summary>
        Oldest
    }

    /// <summary>
    /// 간단한 날짜 정렬 설정
    /// </summary>
    public class SimpleSortSettings
    {
        public DateSortDirection Direction { get; set; } = DateSortDirection.Newest;
        
        /// <summary>
        /// 정렬 설정의 표시명을 반환합니다
        /// </summary>
        public string GetDisplayName()
        {
            return Direction switch
            {
                DateSortDirection.Newest => "최신순",
                DateSortDirection.Oldest => "오래된순",
                _ => "최신순"
            };
        }

        /// <summary>
        /// 정렬 방향을 토글합니다
        /// </summary>
        public void ToggleDirection()
        {
            Direction = Direction == DateSortDirection.Newest ? DateSortDirection.Oldest : DateSortDirection.Newest;
        }
    }

    // 기존 복잡한 정렬 시스템 (레거시 호환성을 위해 유지)
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
                SortCriteria.Date => "날짜",
                SortCriteria.FileName => "파일명",
                SortCriteria.Level => "레벨",
                SortCriteria.PhysicalPower => "무력",
                SortCriteria.MagicalPower => "요력",
                SortCriteria.SpiritualPower => "영력",
                SortCriteria.Experience => "경험치",
                SortCriteria.Gold => "금",
                _ => "날짜"
            };

            var directionIcon = Direction == SortDirection.Ascending ? "??" : "??";
            var directionName = Direction == SortDirection.Ascending ? "오름차순" : "내림차순";
            
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

    // 레거시 호환성
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