using System.Collections.ObjectModel;
using SaveCodeClassfication.Models;

namespace SaveCodeClassfication.Services
{
    /// <summary>
    /// ������ �׷�ȭ ����� �����ϴ� ����
    /// </summary>
    public static class JobGroupService
    {
        /// <summary>
        /// ĳ���͵��� �������� �׷�ȭ�մϴ�
        /// </summary>
        public static List<JobGroupInfo> GroupCharactersByJob(IEnumerable<CharacterInfo> characters)
        {
            var jobGroups = new List<JobGroupInfo>();

            // ĳ���͵��� �������� �׷�ȭ
            var charactersByJob = characters
                .Where(c => c.SaveCodes.Any()) // ���̺� �ڵ尡 �ִ� ĳ���͸�
                .GroupBy(c => GetCharacterJobClass(c))
                .OrderBy(g => GetJobSortOrder(g.Key));

            foreach (var jobGroup in charactersByJob)
            {
                var jobClass = jobGroup.Key;
                var charactersInJob = jobGroup.OrderBy(c => c.CharacterName).ToList();
                
                // �� ���̺� �ڵ� �� ���
                var totalSaveCodeCount = charactersInJob.Sum(c => c.SaveCodes.Count);
                
                // �ֱ� ���� �ð� ���
                var latestModified = charactersInJob
                    .SelectMany(c => c.SaveCodes)
                    .Max(s => s.FileDate);

                var jobGroupInfo = new JobGroupInfo
                {
                    JobClass = jobClass,
                    JobDisplayName = GetJobDisplayName(jobClass),
                    Characters = new ObservableCollection<CharacterInfo>(charactersInJob),
                    CharacterCount = $"{charactersInJob.Count}��",
                    TotalSaveCodeCount = $"�� {totalSaveCodeCount}�� ���̺�",
                    LastModified = latestModified.ToString("yyyy-MM-dd HH:mm"),
                    IsExpanded = false
                };

                jobGroups.Add(jobGroupInfo);
            }

            return jobGroups;
        }

        /// <summary>
        /// ĳ������ ������ �����մϴ�
        /// </summary>
        private static string GetCharacterJobClass(CharacterInfo character)
        {
            // ù ��° ���̺� �ڵ��� ���� ������ ���
            var firstSaveCode = character.SaveCodes.FirstOrDefault();
            if (firstSaveCode != null && !string.IsNullOrEmpty(firstSaveCode.JobClass))
            {
                return firstSaveCode.JobClass;
            }

            // ���� ������ ���� ��� �ɷ�ġ�� ������� ����
            return InferJobFromCharacterStats(character);
        }

        /// <summary>
        /// ĳ������ �ɷ�ġ�� ������� ������ �����մϴ�
        /// </summary>
        private static string InferJobFromCharacterStats(CharacterInfo character)
        {
            var latestSaveCode = character.SaveCodes
                .OrderByDescending(s => s.FileDate)
                .FirstOrDefault();

            if (latestSaveCode == null)
                return "�̺з�";

            // �ɷ�ġ �Ľ� �õ�
            if (int.TryParse(latestSaveCode.PhysicalPower.Replace(",", "").Replace("���� ����", "0"), out int physical) &&
                int.TryParse(latestSaveCode.MagicalPower.Replace(",", "").Replace("���� ����", "0"), out int magical) &&
                int.TryParse(latestSaveCode.SpiritualPower.Replace(",", "").Replace("���� ����", "0"), out int spiritual))
            {
                // ���� ���� �ɷ�ġ�� ������� ���� ����
                if (physical >= magical && physical >= spiritual)
                {
                    return "����"; // ���� �迭
                }
                else if (magical >= physical && magical >= spiritual)
                {
                    return "����"; // ���� �迭
                }
                else if (spiritual >= physical && spiritual >= magical)
                {
                    return "����"; // ���� �迭
                }
            }

            return "�̺з�"; // ������ �� �� ���� ���
        }

        /// <summary>
        /// ������ ǥ�ø��� ��ȯ�մϴ�
        /// </summary>
        private static string GetJobDisplayName(string jobClass)
        {
            return jobClass switch
            {
                "����" => "?? ����",
                "����" => "?? ����",
                "����" => "? ����",
                "�̺з�" => "? �̺з�",
                _ => $"?? {jobClass}"
            };
        }

        /// <summary>
        /// ���� ���� ������ ��ȯ�մϴ�
        /// </summary>
        private static int GetJobSortOrder(string jobClass)
        {
            return jobClass switch
            {
                "����" => 1,
                "����" => 2,
                "����" => 3,
                "�̺з�" => 999,
                _ => 500
            };
        }

        /// <summary>
        /// ���� �׷��� �˻��մϴ�
        /// </summary>
        public static List<JobGroupInfo> FilterJobGroups(List<JobGroupInfo> jobGroups, string searchText)
        {
            if (string.IsNullOrEmpty(searchText))
                return jobGroups;

            var filteredGroups = new List<JobGroupInfo>();

            foreach (var jobGroup in jobGroups)
            {
                // ���������� �˻�
                if (jobGroup.JobClass.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                    jobGroup.JobDisplayName.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                {
                    filteredGroups.Add(jobGroup);
                }
                else
                {
                    // �׷� �� ĳ���͸����� �˻�
                    var matchingCharacters = jobGroup.Characters.Where(c =>
                        c.CharacterName.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                        c.OriginalCharacterName.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    if (matchingCharacters.Any())
                    {
                        // ��Ī�Ǵ� ĳ���͸� �����ϴ� �� �׷� ����
                        var filteredGroup = new JobGroupInfo
                        {
                            JobClass = jobGroup.JobClass,
                            JobDisplayName = jobGroup.JobDisplayName,
                            Characters = new ObservableCollection<CharacterInfo>(matchingCharacters),
                            CharacterCount = $"{matchingCharacters.Count}��",
                            TotalSaveCodeCount = $"�� {matchingCharacters.Sum(c => c.SaveCodes.Count)}�� ���̺�",
                            LastModified = jobGroup.LastModified,
                            IsExpanded = true // �˻� ����� �ڵ����� Ȯ��
                        };
                        filteredGroups.Add(filteredGroup);
                    }
                }
            }

            return filteredGroups;
        }
    }
}