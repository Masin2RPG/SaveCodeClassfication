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

                // 직업 정보 추출
                var jobInfo = ExtractJobClass(content);

                // 아이템 정보 추출
                var items = ExtractItems(content);

                // 기본 정보 추출
                var stats = ExtractStats(content);

                var fileDate = File.GetLastWriteTime(fileInfo.FilePath);
                var itemsDisplayText = items.Count > 0 ? string.Join(" | ", items) : "장착된 아이템 없음";

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
                    Experience = stats.Experience,
                    JobClass = jobInfo.JobClass,
                    JobDisplayText = jobInfo.JobDisplayText
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
        /// 직업 정보를 추출합니다
        /// </summary>
        private (string JobClass, string JobDisplayText) ExtractJobClass(string content)
        {
            // 직업 정보를 추출하는 여러 패턴 시도
            var jobPatterns = new[]
            {
                @"call\s+Preload\(\s*[""']직업:\s*([^""']*?)[""']\s*\)",
                @"직업:\s*(.+?)(?:\s*[""']|\r|\n|$)",
                @"class:\s*(.+?)(?:\s*[""']|\r|\n|$)",
                @"job:\s*(.+?)(?:\s*[""']|\r|\n|$)"
            };

            foreach (var pattern in jobPatterns)
            {
                var jobMatch = Regex.Match(content, pattern, RegexOptions.IgnoreCase);
                if (jobMatch.Success)
                {
                    var jobClass = jobMatch.Groups[1].Value.Trim();
                    // 색상 코드 및 특수 문자 제거
                    jobClass = Regex.Replace(jobClass, @"\|c[0-9a-fA-F]{8}|\|[rR]|\|", "");
                    jobClass = jobClass.Trim();
                    
                    if (!string.IsNullOrEmpty(jobClass))
                    {
                        return (jobClass, GetJobDisplayName(jobClass));
                    }
                }
            }

            // 직업 정보가 없는 경우 능력치를 기반으로 추측
            var jobFromStats = InferJobFromStats(content);
            return (jobFromStats, GetJobDisplayName(jobFromStats));
        }

        /// <summary>
        /// 능력치를 기반으로 직업을 추측합니다
        /// </summary>
        private string InferJobFromStats(string content)
        {
            var stats = ExtractStats(content);
            
            // 능력치가 숫자로 파싱 가능한지 확인
            if (int.TryParse(stats.PhysicalPower.Replace(",", ""), out int physical) &&
                int.TryParse(stats.MagicalPower.Replace(",", ""), out int magical) &&
                int.TryParse(stats.SpiritualPower.Replace(",", ""), out int spiritual))
            {
                // 가장 높은 능력치를 기반으로 직업 추측
                if (physical >= magical && physical >= spiritual)
                {
                    return "무사"; // 물리 계열
                }
                else if (magical >= physical && magical >= spiritual)
                {
                    return "도사"; // 마법 계열
                }
                else if (spiritual >= physical && spiritual >= magical)
                {
                    return "선인"; // 영력 계열
                }
            }

            return "미분류"; // 직업을 알 수 없는 경우
        }

        /// <summary>
        /// 직업의 표시명을 반환합니다
        /// </summary>
        private string GetJobDisplayName(string jobClass)
        {
            return jobClass switch
            {
                "무사" => "?? 무사",
                "도사" => "?? 도사", 
                "선인" => "? 선인",
                "미분류" => "? 미분류",
                _ => $"?? {jobClass}"
            };
        }

        /// <summary>
        /// 아이템 정보를 추출합니다
        /// </summary>
        private List<string> ExtractItems(string content)
        {
            var items = new List<string>();
            
            for (int i = 1; i <= 6; i++)
            {
                var itemMatch = Regex.Match(content, $@"call\s+Preload\(\s*[""']장착품{i}:\s*['""]([^'""]*?)['""][""']\s*\)", RegexOptions.IgnoreCase);
                
                if (itemMatch.Success)
                {
                    var itemName = itemMatch.Groups[1].Value.Trim();
                    itemName = Regex.Replace(itemName, @"\|c[0-9a-fA-F]{8}|\|[rR]|\|", "");
                    itemName = itemName.Trim('\'', '"');
                    
                    if (!string.IsNullOrEmpty(itemName))
                    {
                        items.Add($"장착품{i}: {itemName}");
                    }
                }
            }
            
            return items;
        }

        /// <summary>
        /// 기본 정보를 추출합니다
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