using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using SaveCodeClassfication.Models;

namespace SaveCodeClassfication.Services
{
    /// <summary>
    /// ���̺� �ڵ� ������ �Ľ��ϴ� ����
    /// </summary>
    public class SaveCodeParserService
    {
        private readonly CharacterNameMappingService _nameMapping;

        public SaveCodeParserService(CharacterNameMappingService nameMapping)
        {
            _nameMapping = nameMapping;
        }

        /// <summary>
        /// ���̺� �ڵ� ������ �Ľ��մϴ�
        /// </summary>
        public SaveCodeInfo? ParseSaveCodeFile(string content, TxtFileInfo fileInfo)
        {
            try
            {
                // ĳ���͸� ����
                var characterName = ExtractCharacterName(content);
                if (string.IsNullOrEmpty(characterName))
                    return null;

                // ���̺� �ڵ� ����
                var saveCode = ExtractSaveCode(content);
                if (string.IsNullOrEmpty(saveCode))
                    return null;

                // ĳ���� �̸� ���� ����
                var displayName = _nameMapping.GetDisplayCharacterName(characterName);

                // ������ ���� ����
                var items = ExtractItems(content);

                // �⺻ ���� ����
                var stats = ExtractStats(content);

                var fileDate = File.GetLastWriteTime(fileInfo.FilePath);
                var itemsDisplayText = items.Count > 0 ? string.Join(" | ", items) : "������ ���� ����";

                return new SaveCodeInfo
                {
                    CharacterName = displayName,
                    SaveCode = saveCode,
                    FileName = fileInfo.FileName,
                    FilePath = fileInfo.FilePath,
                    FileDate = fileDate,
                    FullContent = content,
                    Items = items,
                    ItemsDisplayText = itemsDisplayText,
                    Level = stats.Level,
                    Gold = stats.Gold,
                    Wood = stats.Wood,
                    PhysicalPower = stats.PhysicalPower,
                    MagicalPower = stats.MagicalPower,
                    SpiritualPower = stats.SpiritualPower,
                    Experience = stats.Experience
                };
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// ĳ���͸��� �����մϴ�
        /// </summary>
        private string ExtractCharacterName(string content)
        {
            var characterMatch = Regex.Match(content, @"call\s+Preload\(\s*[""']ĳ����:\s*([^""']*?)[""']\s*\)", RegexOptions.IgnoreCase);
            if (!characterMatch.Success)
            {
                characterMatch = Regex.Match(content, @"ĳ����:\s*(.+?)(?:\s*[""']|\r|\n|$)", RegexOptions.IgnoreCase);
            }
            
            if (!characterMatch.Success)
                return string.Empty;

            var characterName = characterMatch.Groups[1].Value.Trim();
            
            // ���� �ڵ� �� Ư�� ���� ����
            characterName = Regex.Replace(characterName, @"\|c[0-9a-fA-F]{8}|\|[rR]|\|", "");
            return characterName.Trim();
        }

        /// <summary>
        /// ���̺� �ڵ带 �����մϴ�
        /// </summary>
        private string ExtractSaveCode(string content)
        {
            var codeMatch = Regex.Match(content, @"call\s+Preload\(\s*[""']Code:\s*([A-Z0-9\-\s]+?)[""']\s*\)", RegexOptions.IgnoreCase);
            if (!codeMatch.Success)
            {
                codeMatch = Regex.Match(content, @"Code:\s*([A-Z0-9\-\s]+)", RegexOptions.IgnoreCase);
            }
            
            return codeMatch.Success ? codeMatch.Groups[1].Value.Trim() : string.Empty;
        }

        /// <summary>
        /// ������ ������ �����մϴ�
        /// </summary>
        private List<string> ExtractItems(string content)
        {
            var items = new List<string>();
            
            for (int i = 1; i <= 6; i++)
            {
                var itemMatch = Regex.Match(content, $@"call\s+Preload\(\s*[""']������{i}:\s*['""]([^'""]*?)['""][""']\s*\)", RegexOptions.IgnoreCase);
                
                if (itemMatch.Success)
                {
                    var itemName = itemMatch.Groups[1].Value.Trim();
                    itemName = Regex.Replace(itemName, @"\|c[0-9a-fA-F]{8}|\|[rR]|\|", "");
                    itemName = itemName.Trim('\'', '"');
                    
                    if (!string.IsNullOrEmpty(itemName))
                    {
                        items.Add($"������{i}: {itemName}");
                    }
                }
            }
            
            return items;
        }

        /// <summary>
        /// �⺻ ������ �����մϴ�
        /// </summary>
        private (string Level, string Gold, string Wood, string PhysicalPower, 
                string MagicalPower, string SpiritualPower, string Experience) ExtractStats(string content)
        {
            var levelMatch = Regex.Match(content, @"call\s+Preload\(\s*[""']����:\s*(\d+)[""']\s*\)", RegexOptions.IgnoreCase);
            var level = levelMatch.Success ? levelMatch.Groups[1].Value : "���� ����";

            var goldMatch = Regex.Match(content, @"call\s+Preload\(\s*[""']��:\s*(\d+)[""']\s*\)", RegexOptions.IgnoreCase);
            var gold = goldMatch.Success ? FormatNumber(goldMatch.Groups[1].Value) : "���� ����";

            var woodMatch = Regex.Match(content, @"call\s+Preload\(\s*[""']����:\s*(\d+)[""']\s*\)", RegexOptions.IgnoreCase);
            var wood = woodMatch.Success ? FormatNumber(woodMatch.Groups[1].Value) : "���� ����";

            var physicalPowerMatch = Regex.Match(content, @"call\s+Preload\(\s*[""']����:\s*(\d+)[""']\s*\)", RegexOptions.IgnoreCase);
            var physicalPower = physicalPowerMatch.Success ? FormatNumber(physicalPowerMatch.Groups[1].Value) : "���� ����";

            var magicalPowerMatch = Regex.Match(content, @"call\s+Preload\(\s*[""']���:\s*(\d+)[""']\s*\)", RegexOptions.IgnoreCase);
            var magicalPower = magicalPowerMatch.Success ? FormatNumber(magicalPowerMatch.Groups[1].Value) : "���� ����";

            var spiritualPowerMatch = Regex.Match(content, @"call\s+Preload\(\s*[""']����:\s*(\d+)[""']\s*\)", RegexOptions.IgnoreCase);
            var spiritualPower = spiritualPowerMatch.Success ? FormatNumber(spiritualPowerMatch.Groups[1].Value) : "���� ����";

            var experienceMatch = Regex.Match(content, @"call\s+Preload\(\s*[""']����ġ:\s*(\d+)[""']\s*\)", RegexOptions.IgnoreCase);
            var experience = experienceMatch.Success ? FormatNumber(experienceMatch.Groups[1].Value) : "���� ����";

            return (level, gold, wood, physicalPower, magicalPower, spiritualPower, experience);
        }

        /// <summary>
        /// ���ڸ� �������մϴ�
        /// </summary>
        private static string FormatNumber(string numberStr)
        {
            if (long.TryParse(numberStr, out long number))
            {
                return number.ToString("#,##0");
            }
            return numberStr;
        }
    }
}