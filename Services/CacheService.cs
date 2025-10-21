using System.Collections.Generic;
using SaveCodeClassfication.Models;

namespace SaveCodeClassfication.Services
{
    /// <summary>
    /// �м� ��� ����Ҹ� �����ϴ� ���� (�����ͺ��̽� ���)
    /// </summary>
    public class CacheService
    {
        private readonly DatabaseService _databaseService;
        private readonly SettingsService _settingsService;

        public CacheService(DatabaseService databaseService, SettingsService settingsService = null)
        {
            _databaseService = databaseService;
            _settingsService = settingsService;
        }

        /// <summary>
        /// ĳ�ø� �ε��մϴ� (�����ͺ��̽�����)
        /// </summary>
        public Task<AnalysisCache?> LoadCacheAsync()
        {
            // �� �޼���� ���� ��ΰ� �ʿ��ϹǷ� �� �̻� ������� ����
            // LoadCacheAsync(string folderPath)�� ����ؾ� ��
            return Task.FromResult<AnalysisCache?>(null);
        }

        /// <summary>
        /// Ư�� ������ ĳ�ø� �ε��մϴ�
        /// </summary>
        public async Task<AnalysisCache?> LoadCacheAsync(string folderPath)
        {
            return await _databaseService.LoadAnalysisAsync(folderPath);
        }

        /// <summary>
        /// ĳ�ø� �����մϴ� (�����ͺ��̽���)
        /// </summary>
        public async Task<bool> SaveCacheAsync(AnalysisCache cache)
        {
            var fileHashes = cache.FileHashes ?? new Dictionary<string, DateTime>();
            
            // ���� ����� Ű ��������
            var userKey = await GetCurrentUserKeyAsync();
            
            return await _databaseService.SaveAnalysisAsync(cache.FolderPath, cache.SaveCodes, fileHashes, userKey);
        }

        /// <summary>
        /// ĳ�ø� �����մϴ� (�����ͺ��̽��� ����� ������)
        /// </summary>
        public bool DeleteCache()
        {
            try
            {
                var userKey = GetCurrentUserKeyAsync().Result;
                var task = _databaseService.ClearAllDataAsync(userKey);
                task.Wait();
                return task.Result;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// ����� ĳ���ͺ� ���̺��ڵ带 ��ȸ�մϴ�
        /// </summary>
        public async Task<List<SaveCodeInfo>> LoadCharacterSaveCodesAsync()
        {
            try
            {
                var userKey = await GetCurrentUserKeyAsync();
                return await _databaseService.LoadUserSaveCodesAsync(userKey);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ĳ���ͺ� ���̺��ڵ� ��ȸ ����: {ex.Message}");
                return new List<SaveCodeInfo>();
            }
        }

        /// <summary>
        /// Ư�� ĳ������ ���̺��ڵ常 ��ȸ�մϴ�
        /// </summary>
        public async Task<List<SaveCodeInfo>> LoadSaveCodesByCharacterAsync(string characterName)
        {
            try
            {
                var userKey = await GetCurrentUserKeyAsync();
                var allSaveCodes = await _databaseService.LoadUserSaveCodesAsync(userKey);
                
                // ĳ���͸����� ���͸�
                return allSaveCodes.Where(sc => sc.CharacterName.Equals(characterName, StringComparison.OrdinalIgnoreCase))
                                  .OrderByDescending(sc => sc.FileDate)
                                  .ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ư�� ĳ����({characterName}) ���̺��ڵ� ��ȸ ����: {ex.Message}");
                return new List<SaveCodeInfo>();
            }
        }

        /// <summary>
        /// ����� ĳ���� ����� ��ȸ�մϴ�
        /// </summary>
        public async Task<List<string>> LoadCharacterListAsync()
        {
            try
            {
                var userKey = await GetCurrentUserKeyAsync();
                var allSaveCodes = await _databaseService.LoadUserSaveCodesAsync(userKey);
                
                // ĳ���͸� �ߺ� �����ϰ� ����
                return allSaveCodes.Select(sc => sc.CharacterName)
                                  .Distinct()
                                  .OrderBy(name => name)
                                  .ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ĳ���� ��� ��ȸ ����: {ex.Message}");
                return new List<string>();
            }
        }

        /// <summary>
        /// ĳ���ͺ� ���̺��ڵ� ��踦 �����ɴϴ�
        /// </summary>
        public async Task<Dictionary<string, int>> GetCharacterSaveCodeStatsAsync()
        {
            try
            {
                var userKey = await GetCurrentUserKeyAsync();
                var allSaveCodes = await _databaseService.LoadUserSaveCodesAsync(userKey);
                
                // ĳ���ͺ� ���̺��ڵ� ���� ���
                return allSaveCodes.GroupBy(sc => sc.CharacterName)
                                  .ToDictionary(g => g.Key, g => g.Count());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ĳ���ͺ� ��� ��ȸ ����: {ex.Message}");
                return new Dictionary<string, int>();
            }
        }

        /// <summary>
        /// ĳ�ð� ��ȿ���� Ȯ���մϴ�
        /// </summary>
        public bool IsCacheValid(AnalysisCache cache, string folderPath, IEnumerable<TxtFileInfo> currentFiles)
        {
            return _databaseService.IsCacheValid(cache, folderPath, currentFiles);
        }

        /// <summary>
        /// ĳ�� ������ ��ȯ�մϴ� (�����ͺ��̽� ����)
        /// </summary>
        public async Task<string> GetCacheInfoAsync()
        {
            return await _databaseService.GetDatabaseInfoAsync();
        }

        /// <summary>
        /// �����ͺ��̽� ������ �׽�Ʈ�մϴ�
        /// </summary>
        public async Task<bool> TestDatabaseConnectionAsync()
        {
            return await _databaseService.TestConnectionAsync();
        }

        /// <summary>
        /// �����ͺ��̽��� �ʱ�ȭ�մϴ�
        /// </summary>
        public async Task<bool> InitializeDatabaseAsync()
        {
            return await _databaseService.InitializeDatabaseAsync();
        }

        /// <summary>
        /// ���� ����� Ű�� �����ɴϴ�
        /// </summary>
        private async Task<string> GetCurrentUserKeyAsync()
        {
            if (_settingsService != null)
            {
                try
                {
                    var settings = await _settingsService.LoadSettingsAsync();
                    return settings.CurrentUserKey;
                }
                catch
                {
                    // ���� �ε� ���� �� �⺻�� ���
                }
            }
            
            return "default_user";
        }
    }
}