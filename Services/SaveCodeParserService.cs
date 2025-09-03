using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using SaveCodeClassfication.Models;

namespace SaveCodeClassfication.Services
{
    /// <summary>
    /// 세이브 코드 파일을 파싱하는 서비스
    /// </summary>
    public class SaveCodeParserService
    {
        private readonly CharacterNameMappingService _nameMapping;

        public SaveCodeParserService(CharacterNameMappingService nameMapping)
        {
            _nameMapping = nameMapping;
        }

        /// <summary>
        /// 세이브 코드 파일을 파싱합니다
        /// </summary>
        public SaveCodeInfo? ParseSaveCodeFile(string content, TxtFileInfo fileInfo)
        {
            try
            {
                // 캐릭터명 추출
                var characterName = ExtractCharacterName(content);
                if (string.IsNullOrEmpty(characterName))
                    return null;

                // 세이브 코드 추출
                var saveCode = ExtractSaveCode(content);
                if (string.IsNullOrEmpty(saveCode))
                    return null;

                // 캐릭터 이름 매핑 적용
                var displayName = _nameMapping.GetDisplayCharacterName(characterName);

                // 아이템 정보 추출
                var items = ExtractItems(content);

                // 기본 스탯 추출
                var stats = ExtractStats(content);

                var fileDate = File.GetLastWriteTime(fileInfo.FilePath);
                var itemsDisplayText = items.Count > 0 ? string.Join(" | ", items) : "아이템 정보 없음";

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
        /// 캐릭터명을 추출합니다
        /// </summary>
        private string ExtractCharacterName(string content)
        {
            var characterMatch = Regex.Match(content, @"call\s+Preload\(\s*[""']캐릭터:\s*([^""']*?)[""']\s*\)", RegexOptions.IgnoreCase);
            if (!characterMatch.Success)
            {
                characterMatch = Regex.Match(content, @"캐릭터:\s*(.+?)(?:\s*[""']|\r|\n|$)", RegexOptions.IgnoreCase);
            }
            
            if (!characterMatch.Success)
                return string.Empty;

            var characterName = characterMatch.Groups[1].Value.Trim();
            
            // 색상 코드 및 특수 문자 제거
            characterName = Regex.Replace(characterName, @"\|c[0-9a-fA-F]{8}|\|[rR]|\|", "");
            return characterName.Trim();
        }

        /// <summary>
        /// 세이브 코드를 추출합니다
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
        /// 아이템 정보를 추출합니다
        /// </summary>
        private List<string> ExtractItems(string content)
        {
            var items = new List<string>();
            
            for (int i = 1; i <= 6; i++)
            {
                var itemMatch = Regex.Match(content, $@"call\s+Preload\(\s*[""']아이템{i}:\s*['""]([^'""]*?)['""][""']\s*\)", RegexOptions.IgnoreCase);
                
                if (itemMatch.Success)
                {
                    var itemName = itemMatch.Groups[1].Value.Trim();
                    itemName = Regex.Replace(itemName, @"\|c[0-9a-fA-F]{8}|\|[rR]|\|", "");
                    itemName = itemName.Trim('\'', '"');
                    
                    if (!string.IsNullOrEmpty(itemName))
                    {
                        items.Add($"아이템{i}: {itemName}");
                    }
                }
            }
            
            return items;
        }

        /// <summary>
        /// 기본 스탯을 추출합니다
        /// </summary>
        private (string Level, string Gold, string Wood, string PhysicalPower, 
                string MagicalPower, string SpiritualPower, string Experience) ExtractStats(string content)
        {
            var levelMatch = Regex.Match(content, @"call\s+Preload\(\s*[""']레벨:\s*(\d+)[""']\s*\)", RegexOptions.IgnoreCase);
            var level = levelMatch.Success ? levelMatch.Groups[1].Value : "정보 없음";

            var goldMatch = Regex.Match(content, @"call\s+Preload\(\s*[""']금:\s*(\d+)[""']\s*\)", RegexOptions.IgnoreCase);
            var gold = goldMatch.Success ? FormatNumber(goldMatch.Groups[1].Value) : "정보 없음";

            var woodMatch = Regex.Match(content, @"call\s+Preload\(\s*[""']나무:\s*(\d+)[""']\s*\)", RegexOptions.IgnoreCase);
            var wood = woodMatch.Success ? FormatNumber(woodMatch.Groups[1].Value) : "정보 없음";

            var physicalPowerMatch = Regex.Match(content, @"call\s+Preload\(\s*[""']무력:\s*(\d+)[""']\s*\)", RegexOptions.IgnoreCase);
            var physicalPower = physicalPowerMatch.Success ? FormatNumber(physicalPowerMatch.Groups[1].Value) : "정보 없음";

            var magicalPowerMatch = Regex.Match(content, @"call\s+Preload\(\s*[""']요력:\s*(\d+)[""']\s*\)", RegexOptions.IgnoreCase);
            var magicalPower = magicalPowerMatch.Success ? FormatNumber(magicalPowerMatch.Groups[1].Value) : "정보 없음";

            var spiritualPowerMatch = Regex.Match(content, @"call\s+Preload\(\s*[""']영력:\s*(\d+)[""']\s*\)", RegexOptions.IgnoreCase);
            var spiritualPower = spiritualPowerMatch.Success ? FormatNumber(spiritualPowerMatch.Groups[1].Value) : "정보 없음";

            var experienceMatch = Regex.Match(content, @"call\s+Preload\(\s*[""']경험치:\s*(\d+)[""']\s*\)", RegexOptions.IgnoreCase);
            var experience = experienceMatch.Success ? FormatNumber(experienceMatch.Groups[1].Value) : "정보 없음";

            return (level, gold, wood, physicalPower, magicalPower, spiritualPower, experience);
        }

        /// <summary>
        /// 숫자를 포맷팅합니다
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