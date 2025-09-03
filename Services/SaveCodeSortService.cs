using SaveCodeClassfication.Models;

namespace SaveCodeClassfication.Services
{
    /// <summary>
    /// ������ ��¥ ���� ����
    /// </summary>
    public class SimpleSortService
    {
        /// <summary>
        /// ��¥ �������� ���̺� �ڵ� ����� �����մϴ�
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
        /// ������ ���� �������� ���̺� �ڵ带 �����մϴ�
        /// </summary>
        public static IEnumerable<SaveCodeInfo> SortSaveCodes(IEnumerable<SaveCodeInfo> saveCodes, SimpleSortSettings settings)
        {
            return SortByDate(saveCodes, settings.Direction);
        }
    }

    /// <summary>
    /// ���� ������ ���̺� �ڵ� ���� ���� (���Ž� ȣȯ��)
    /// </summary>
    public class SaveCodeSortService
    {
        /// <summary>
        /// ��� ������ ���� ���� ����� ��ȯ�մϴ�
        /// </summary>
        public static List<SortCriteriaInfo> GetAvailableSortCriteria()
        {
            return new List<SortCriteriaInfo>
            {
                new() { Criteria = SortCriteria.Date, DisplayName = "��¥", Icon = "??", Description = "���� ���� ��¥ ����" },
                new() { Criteria = SortCriteria.FileName, DisplayName = "���ϸ�", Icon = "??", Description = "���ϸ� ���ĺ� ����" },
                new() { Criteria = SortCriteria.Level, DisplayName = "����", Icon = "??", Description = "ĳ���� ���� ����" },
                new() { Criteria = SortCriteria.PhysicalPower, DisplayName = "����", Icon = "??", Description = "���� ���� ����" },
                new() { Criteria = SortCriteria.MagicalPower, DisplayName = "���", Icon = "??", Description = "��� ���� ����" },
                new() { Criteria = SortCriteria.SpiritualPower, DisplayName = "����", Icon = "?", Description = "���� ���� ����" },
                new() { Criteria = SortCriteria.Experience, DisplayName = "����ġ", Icon = "?", Description = "����ġ ����" },
                new() { Criteria = SortCriteria.Gold, DisplayName = "��", Icon = "??", Description = "���� �ݾ� ����" }
            };
        }

        /// <summary>
        /// ������ ���� ������ ���� ���̺� �ڵ� ����� �����մϴ�
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
        /// ���Ž� ȣȯ��: ���� ���� �ɼ����� �����մϴ�
        /// </summary>
        public static IEnumerable<SaveCodeInfo> SortSaveCodes(IEnumerable<SaveCodeInfo> saveCodes, SaveCodeSortOption sortOption)
        {
            var sortSettings = ConvertLegacySortOption(sortOption);
            return SortSaveCodes(saveCodes, sortSettings);
        }

        /// <summary>
        /// ���Ž� ���� �ɼ��� ���ο� ���� �������� ��ȯ�մϴ�
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
        /// ���Ž� ȣȯ��: ���� �ɼ��� ǥ�ø��� ��ȯ�մϴ�
        /// </summary>
        public static string GetSortOptionDisplayName(SaveCodeSortOption option)
        {
            var settings = ConvertLegacySortOption(option);
            return settings.GetDisplayName();
        }

        /// <summary>
        /// ��� ������ ���� �ɼ� ����� ��ȯ�մϴ� (���Ž� ȣȯ��)
        /// </summary>
        public static List<SortOptionInfo> GetAvailableSortOptions()
        {
            return new List<SortOptionInfo>
            {
                new() { Option = SaveCodeSortOption.DateDescending, DisplayName = "?? �ֽż�", Description = "���� ������ ���� �ֽż�", Icon = "??" },
                new() { Option = SaveCodeSortOption.DateAscending, DisplayName = "?? �����ȼ�", Description = "���� ������ ���� �����ȼ�", Icon = "??" },
                new() { Option = SaveCodeSortOption.FileNameAscending, DisplayName = "?? ���ϸ� A-Z", Description = "���ϸ� ���ĺ� ����", Icon = "??" },
                new() { Option = SaveCodeSortOption.FileNameDescending, DisplayName = "?? ���ϸ� Z-A", Description = "���ϸ� ����", Icon = "??" },
                new() { Option = SaveCodeSortOption.LevelDescending, DisplayName = "?? ���� ������", Description = "ĳ���� ���� ������", Icon = "??" },
                new() { Option = SaveCodeSortOption.LevelAscending, DisplayName = "?? ���� ������", Description = "ĳ���� ���� ������", Icon = "??" },
                new() { Option = SaveCodeSortOption.PhysicalPowerDescending, DisplayName = "?? ���� ������", Description = "���� ���� ������", Icon = "??" },
                new() { Option = SaveCodeSortOption.PhysicalPowerAscending, DisplayName = "?? ���� ������", Description = "���� ���� ������", Icon = "??" }
            };
        }

        /// <summary>
        /// ���ڿ��� ���ڷ� �Ľ��մϴ� (���Ŀ�)
        /// </summary>
        private static long ParseNumber(string value)
        {
            if (string.IsNullOrEmpty(value) || value == "���� ����")
                return 0;

            // ��ǥ ���� �� ���ڸ� ����
            var numericString = new string(value.Where(char.IsDigit).ToArray());
            return long.TryParse(numericString, out var result) ? result : 0;
        }
    }
}