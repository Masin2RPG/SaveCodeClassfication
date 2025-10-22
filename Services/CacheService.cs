using SaveCodeClassfication.Models;
using SaveCodeClassfication.Services;
using SaveCodeClassfication.Utils;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SaveCodeClassfication.Services
{
    /// <summary>
    /// API ��� ĳ�� ���� - ���̺� �ڵ� �������� ���� �� �ε带 ���
    /// </summary>
    public class CacheService
    {
        private readonly ApiDatabaseService _apiDatabaseService;
        private readonly SettingsService _settingsService;

        public CacheService(ApiDatabaseService apiDatabaseService, SettingsService settingsService)
        {
            _apiDatabaseService = apiDatabaseService ?? throw new ArgumentNullException(nameof(apiDatabaseService));
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        }

        /// <summary>
        /// API�� ���� ĳ�� �����͸� �����մϴ�
        /// </summary>
        public async Task<bool> SaveCacheAsync(AnalysisCache cache, string userId)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"=== CacheService: API ��� ���� ���� (�����: {userId}) ===");
                System.Diagnostics.Debug.WriteLine($"������ ���̺� �ڵ� ��: {cache.SaveCodes.Count}");

                var result = await _apiDatabaseService.SaveCacheAsync(cache, userId);
                
                System.Diagnostics.Debug.WriteLine($"API ���� ���: {result}");
                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CacheService SaveCacheAsync ����: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// API�� ���� ĳ�� �����͸� �ε��մϴ�
        /// </summary>
        public async Task<AnalysisCache?> LoadCacheAsync(string folderPath, string userId)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"=== CacheService: API ��� ĳ�� �ε� (�����: {userId}) ===");
                
                var saveCodes = await _apiDatabaseService.LoadUserSaveCodesAsync(userId);
                
                if (saveCodes.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("API���� ���̺� �ڵ带 ã�� �� ����");
                    return null;
                }

                System.Diagnostics.Debug.WriteLine($"API���� {saveCodes.Count}�� ���̺� �ڵ� �ε� �Ϸ�");
                
                return new AnalysisCache
                {
                    FolderPath = folderPath,
                    LastAnalyzed = DateTime.Now,
                    FileHashes = new Dictionary<string, DateTime>(),
                    SaveCodes = saveCodes,
                    TotalFiles = saveCodes.Count,
                    Version = "2.0.0"
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CacheService LoadCacheAsync ����: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// ĳ���ͺ� ���̺� �ڵ� �ε�
        /// </summary>
        public async Task<List<SaveCodeInfo>> LoadCharacterSaveCodesAsync(string userId)
        {
            try
            {
                return await _apiDatabaseService.LoadUserSaveCodesAsync(userId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadCharacterSaveCodesAsync ����: {ex.Message}");
                return new List<SaveCodeInfo>();
            }
        }

        /// <summary>
        /// API ���� ������ �׽�Ʈ�մϴ�
        /// </summary>
        public async Task<bool> TestDatabaseConnectionAsync()
        {
            try
            {
                return await _apiDatabaseService.TestConnectionAsync();
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// API ���� �ʱ�ȭ�� �׽�Ʈ�մϴ�
        /// </summary>
        public async Task<bool> InitializeDatabaseAsync()
        {
            try
            {
                return await _apiDatabaseService.InitializeDatabaseAsync();
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// API�� ���� �����ͺ��̽� ������ �����ɴϴ�
        /// </summary>
        public async Task<string> GetCacheInfoAsync()
        {
            try
            {
                return await _apiDatabaseService.GetDatabaseInfoAsync();
            }
            catch (Exception ex)
            {
                return $"API ��� �����ͺ��̽� ���� ��ȸ ����:\n{ex.Message}";
            }
        }

        /// <summary>
        /// API�� ���� ��� ����� �����͸� �����մϴ�
        /// </summary>
        public async Task<bool> ClearAllDataAsync(string userId)
        {
            try
            {
                return await _apiDatabaseService.ClearAllDataAsync(userId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ClearAllDataAsync ����: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// ĳ�� ��ȿ���� �˻��մϴ�
        /// </summary>
        public bool IsCacheValid(AnalysisCache cache, string folderPath, IEnumerable<TxtFileInfo> currentFiles)
        {
            // API ��ݿ����� �׻� �ֽ� �����͸� �����ϹǷ� ������ ������ ����
            return cache != null && 
                   !string.IsNullOrEmpty(cache.FolderPath) && 
                   cache.SaveCodes.Count > 0;
        }
    }
}