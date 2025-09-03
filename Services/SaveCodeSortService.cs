using SaveCodeClassfication.Models;

namespace SaveCodeClassfication.Services
{
    /// <summary>
    /// 간단한 날짜 정렬 서비스
    /// </summary>
    public class SimpleSortService
    {
        /// <summary>
        /// 날짜 기준으로 세이브 코드 목록을 정렬합니다
        /// </summary>
        public static IEnumerable<SaveCodeInfo> SortByDate(IEnumerable<SaveCodeInfo> saveCodes, DateSortDirection direction)
        {
            return direction switch
            {
                DateSortDirection.Newest => saveCodes.OrderByDescending(x => x.FileDate),
                DateSortDirection.Oldest => saveCodes.OrderBy(x => x.FileDate),
                _ => saveCodes.OrderByDescending(x => x.FileDate)
            };
        }

        /// <summary>
        /// 간단한 정렬 설정으로 세이브 코드를 정렬합니다
        /// </summary>
        public static IEnumerable<SaveCodeInfo> SortSaveCodes(IEnumerable<SaveCodeInfo> saveCodes, SimpleSortSettings settings)
        {
            return SortByDate(saveCodes, settings.Direction);
        }
    }

    /// <summary>
    /// 기존 복잡한 세이브 코드 정렬 서비스 (레거시 호환성)
    /// </summary>
    public class SaveCodeSortService
    {
        /// <summary>
        /// 사용 가능한 정렬 기준 목록을 반환합니다
        /// </summary>
        public static List<SortCriteriaInfo> GetAvailableSortCriteria()
        {
            return new List<SortCriteriaInfo>
            {
                new() { Criteria = SortCriteria.Date, DisplayName = "날짜", Icon = "??", Description = "파일 수정 날짜 기준" },
                new() { Criteria = SortCriteria.FileName, DisplayName = "파일명", Icon = "??", Description = "파일명 알파벳 순서" },
                new() { Criteria = SortCriteria.Level, DisplayName = "레벨", Icon = "??", Description = "캐릭터 레벨 기준" },
                new() { Criteria = SortCriteria.PhysicalPower, DisplayName = "무력", Icon = "??", Description = "무력 스탯 기준" },
                new() { Criteria = SortCriteria.MagicalPower, DisplayName = "요력", Icon = "??", Description = "요력 스탯 기준" },
                new() { Criteria = SortCriteria.SpiritualPower, DisplayName = "영력", Icon = "?", Description = "영력 스탯 기준" },
                new() { Criteria = SortCriteria.Experience, DisplayName = "경험치", Icon = "?", Description = "경험치 기준" },
                new() { Criteria = SortCriteria.Gold, DisplayName = "금", Icon = "??", Description = "보유 금액 기준" }
            };
        }

        /// <summary>
        /// 지정된 정렬 설정에 따라 세이브 코드 목록을 정렬합니다
        /// </summary>
        public static IEnumerable<SaveCodeInfo> SortSaveCodes(IEnumerable<SaveCodeInfo> saveCodes, SortSettings sortSettings)
        {
            var query = sortSettings.Criteria switch
            {
                SortCriteria.Date => sortSettings.Direction == SortDirection.Ascending 
                    ? saveCodes.OrderBy(x => x.FileDate) 
                    : saveCodes.OrderByDescending(x => x.FileDate),
                
                SortCriteria.FileName => sortSettings.Direction == SortDirection.Ascending 
                    ? saveCodes.OrderBy(x => x.FileName) 
                    : saveCodes.OrderByDescending(x => x.FileName),
                
                SortCriteria.Level => sortSettings.Direction == SortDirection.Ascending 
                    ? saveCodes.OrderBy(x => ParseNumber(x.Level)) 
                    : saveCodes.OrderByDescending(x => ParseNumber(x.Level)),
                
                SortCriteria.PhysicalPower => sortSettings.Direction == SortDirection.Ascending 
                    ? saveCodes.OrderBy(x => ParseNumber(x.PhysicalPower)) 
                    : saveCodes.OrderByDescending(x => ParseNumber(x.PhysicalPower)),
                
                SortCriteria.MagicalPower => sortSettings.Direction == SortDirection.Ascending 
                    ? saveCodes.OrderBy(x => ParseNumber(x.MagicalPower)) 
                    : saveCodes.OrderByDescending(x => ParseNumber(x.MagicalPower)),
                
                SortCriteria.SpiritualPower => sortSettings.Direction == SortDirection.Ascending 
                    ? saveCodes.OrderBy(x => ParseNumber(x.SpiritualPower)) 
                    : saveCodes.OrderByDescending(x => ParseNumber(x.SpiritualPower)),
                
                SortCriteria.Experience => sortSettings.Direction == SortDirection.Ascending 
                    ? saveCodes.OrderBy(x => ParseNumber(x.Experience)) 
                    : saveCodes.OrderByDescending(x => ParseNumber(x.Experience)),
                
                SortCriteria.Gold => sortSettings.Direction == SortDirection.Ascending 
                    ? saveCodes.OrderBy(x => ParseNumber(x.Gold)) 
                    : saveCodes.OrderByDescending(x => ParseNumber(x.Gold)),
                
                _ => saveCodes.OrderByDescending(x => x.FileDate)
            };

            return query;
        }

        /// <summary>
        /// 레거시 호환성: 기존 정렬 옵션으로 정렬합니다
        /// </summary>
        public static IEnumerable<SaveCodeInfo> SortSaveCodes(IEnumerable<SaveCodeInfo> saveCodes, SaveCodeSortOption sortOption)
        {
            var sortSettings = ConvertLegacySortOption(sortOption);
            return SortSaveCodes(saveCodes, sortSettings);
        }

        /// <summary>
        /// 레거시 정렬 옵션을 새로운 정렬 설정으로 변환합니다
        /// </summary>
        public static SortSettings ConvertLegacySortOption(SaveCodeSortOption option)
        {
            return option switch
            {
                SaveCodeSortOption.DateDescending => new SortSettings { Criteria = SortCriteria.Date, Direction = SortDirection.Descending },
                SaveCodeSortOption.DateAscending => new SortSettings { Criteria = SortCriteria.Date, Direction = SortDirection.Ascending },
                SaveCodeSortOption.FileNameAscending => new SortSettings { Criteria = SortCriteria.FileName, Direction = SortDirection.Ascending },
                SaveCodeSortOption.FileNameDescending => new SortSettings { Criteria = SortCriteria.FileName, Direction = SortDirection.Descending },
                SaveCodeSortOption.LevelDescending => new SortSettings { Criteria = SortCriteria.Level, Direction = SortDirection.Descending },
                SaveCodeSortOption.LevelAscending => new SortSettings { Criteria = SortCriteria.Level, Direction = SortDirection.Ascending },
                SaveCodeSortOption.PhysicalPowerDescending => new SortSettings { Criteria = SortCriteria.PhysicalPower, Direction = SortDirection.Descending },
                SaveCodeSortOption.PhysicalPowerAscending => new SortSettings { Criteria = SortCriteria.PhysicalPower, Direction = SortDirection.Ascending },
                _ => new SortSettings { Criteria = SortCriteria.Date, Direction = SortDirection.Descending }
            };
        }

        /// <summary>
        /// 레거시 호환성: 정렬 옵션의 표시명을 반환합니다
        /// </summary>
        public static string GetSortOptionDisplayName(SaveCodeSortOption option)
        {
            var settings = ConvertLegacySortOption(option);
            return settings.GetDisplayName();
        }

        /// <summary>
        /// 사용 가능한 정렬 옵션 목록을 반환합니다 (레거시 호환성)
        /// </summary>
        public static List<SortOptionInfo> GetAvailableSortOptions()
        {
            return new List<SortOptionInfo>
            {
                new() { Option = SaveCodeSortOption.DateDescending, DisplayName = "?? 최신순", Description = "파일 수정일 기준 최신순", Icon = "??" },
                new() { Option = SaveCodeSortOption.DateAscending, DisplayName = "?? 오래된순", Description = "파일 수정일 기준 오래된순", Icon = "??" },
                new() { Option = SaveCodeSortOption.FileNameAscending, DisplayName = "?? 파일명 A-Z", Description = "파일명 알파벳 순서", Icon = "??" },
                new() { Option = SaveCodeSortOption.FileNameDescending, DisplayName = "?? 파일명 Z-A", Description = "파일명 역순", Icon = "??" },
                new() { Option = SaveCodeSortOption.LevelDescending, DisplayName = "?? 레벨 높은순", Description = "캐릭터 레벨 높은순", Icon = "??" },
                new() { Option = SaveCodeSortOption.LevelAscending, DisplayName = "?? 레벨 낮은순", Description = "캐릭터 레벨 낮은순", Icon = "??" },
                new() { Option = SaveCodeSortOption.PhysicalPowerDescending, DisplayName = "?? 무력 높은순", Description = "무력 스탯 높은순", Icon = "??" },
                new() { Option = SaveCodeSortOption.PhysicalPowerAscending, DisplayName = "?? 무력 낮은순", Description = "무력 스탯 낮은순", Icon = "??" }
            };
        }

        /// <summary>
        /// 문자열을 숫자로 파싱합니다 (정렬용)
        /// </summary>
        private static long ParseNumber(string value)
        {
            if (string.IsNullOrEmpty(value) || value == "정보 없음")
                return 0;

            // 쉼표 제거 후 숫자만 추출
            var numericString = new string(value.Where(char.IsDigit).ToArray());
            return long.TryParse(numericString, out var result) ? result : 0;
        }
    }
}