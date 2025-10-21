using System.Collections.ObjectModel;
using SaveCodeClassfication.Models;

namespace SaveCodeClassfication.Services
{
    /// <summary>
    /// 직업별 그룹화 기능을 제공하는 서비스
    /// </summary>
    public static class JobGroupService
    {
        /// <summary>
        /// 캐릭터들을 직업별로 그룹화합니다
        /// </summary>
        public static List<JobGroupInfo> GroupCharactersByJob(IEnumerable<CharacterInfo> characters)
        {
            var jobGroups = new List<JobGroupInfo>();

            // 캐릭터들을 직업별로 그룹화
            var charactersByJob = characters
                .Where(c => c.SaveCodes.Any()) // 세이브 코드가 있는 캐릭터만
                .GroupBy(c => GetCharacterJobClass(c))
                .OrderBy(g => GetJobSortOrder(g.Key));

            foreach (var jobGroup in charactersByJob)
            {
                var jobClass = jobGroup.Key;
                var charactersInJob = jobGroup.OrderBy(c => c.CharacterName).ToList();
                
                // 총 세이브 코드 수 계산
                var totalSaveCodeCount = charactersInJob.Sum(c => c.SaveCodes.Count);
                
                // 최근 수정 시간 계산
                var latestModified = charactersInJob
                    .SelectMany(c => c.SaveCodes)
                    .Max(s => s.FileDate);

                var jobGroupInfo = new JobGroupInfo
                {
                    JobClass = jobClass,
                    JobDisplayName = GetJobDisplayName(jobClass),
                    Characters = new ObservableCollection<CharacterInfo>(charactersInJob),
                    CharacterCount = $"{charactersInJob.Count}명",
                    TotalSaveCodeCount = $"총 {totalSaveCodeCount}개 세이브",
                    LastModified = latestModified.ToString("yyyy-MM-dd HH:mm"),
                    IsExpanded = false
                };

                jobGroups.Add(jobGroupInfo);
            }

            return jobGroups;
        }

        /// <summary>
        /// 캐릭터의 직업을 추출합니다
        /// </summary>
        private static string GetCharacterJobClass(CharacterInfo character)
        {
            // 첫 번째 세이브 코드의 직업 정보를 사용
            var firstSaveCode = character.SaveCodes.FirstOrDefault();
            if (firstSaveCode != null && !string.IsNullOrEmpty(firstSaveCode.JobClass))
            {
                return firstSaveCode.JobClass;
            }

            // 직업 정보가 없는 경우 능력치를 기반으로 추측
            return InferJobFromCharacterStats(character);
        }

        /// <summary>
        /// 캐릭터의 능력치를 기반으로 직업을 추측합니다
        /// </summary>
        private static string InferJobFromCharacterStats(CharacterInfo character)
        {
            var latestSaveCode = character.SaveCodes
                .OrderByDescending(s => s.FileDate)
                .FirstOrDefault();

            if (latestSaveCode == null)
                return "미분류";

            // 능력치 파싱 시도
            if (int.TryParse(latestSaveCode.PhysicalPower.Replace(",", "").Replace("정보 없음", "0"), out int physical) &&
                int.TryParse(latestSaveCode.MagicalPower.Replace(",", "").Replace("정보 없음", "0"), out int magical) &&
                int.TryParse(latestSaveCode.SpiritualPower.Replace(",", "").Replace("정보 없음", "0"), out int spiritual))
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
        private static string GetJobDisplayName(string jobClass)
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
        /// 직업 정렬 순서를 반환합니다
        /// </summary>
        private static int GetJobSortOrder(string jobClass)
        {
            return jobClass switch
            {
                "무사" => 1,
                "도사" => 2,
                "선인" => 3,
                "미분류" => 999,
                _ => 500
            };
        }

        /// <summary>
        /// 직업 그룹을 검색합니다
        /// </summary>
        public static List<JobGroupInfo> FilterJobGroups(List<JobGroupInfo> jobGroups, string searchText)
        {
            if (string.IsNullOrEmpty(searchText))
                return jobGroups;

            var filteredGroups = new List<JobGroupInfo>();

            foreach (var jobGroup in jobGroups)
            {
                // 직업명으로 검색
                if (jobGroup.JobClass.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                    jobGroup.JobDisplayName.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                {
                    filteredGroups.Add(jobGroup);
                }
                else
                {
                    // 그룹 내 캐릭터명으로 검색
                    var matchingCharacters = jobGroup.Characters.Where(c =>
                        c.CharacterName.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                        c.OriginalCharacterName.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    if (matchingCharacters.Any())
                    {
                        // 매칭되는 캐릭터만 포함하는 새 그룹 생성
                        var filteredGroup = new JobGroupInfo
                        {
                            JobClass = jobGroup.JobClass,
                            JobDisplayName = jobGroup.JobDisplayName,
                            Characters = new ObservableCollection<CharacterInfo>(matchingCharacters),
                            CharacterCount = $"{matchingCharacters.Count}명",
                            TotalSaveCodeCount = $"총 {matchingCharacters.Sum(c => c.SaveCodes.Count)}개 세이브",
                            LastModified = jobGroup.LastModified,
                            IsExpanded = true // 검색 결과는 자동으로 확장
                        };
                        filteredGroups.Add(filteredGroup);
                    }
                }
            }

            return filteredGroups;
        }
    }
}