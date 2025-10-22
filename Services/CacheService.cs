using SaveCodeClassfication.Models;
using SaveCodeClassfication.Services;
using SaveCodeClassfication.Utils;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SaveCodeClassfication.Services
{
    /// <summary>
    /// API 기반 캐시 서비스 - 세이브 코드 데이터의 저장 및 로드를 담당
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
        /// API를 통해 캐시 데이터를 저장합니다
        /// </summary>
        public async Task<bool> SaveCacheAsync(AnalysisCache cache, string userId)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"=== CacheService: API 기반 저장 시작 (사용자: {userId}) ===");
                System.Diagnostics.Debug.WriteLine($"저장할 세이브 코드 수: {cache.SaveCodes.Count}");

                var result = await _apiDatabaseService.SaveCacheAsync(cache, userId);
                
                System.Diagnostics.Debug.WriteLine($"API 저장 결과: {result}");
                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CacheService SaveCacheAsync 오류: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// API를 통해 캐시 데이터를 로드합니다
        /// </summary>
        public async Task<AnalysisCache?> LoadCacheAsync(string folderPath, string userId)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"=== CacheService: API 기반 캐시 로드 (사용자: {userId}) ===");
                
                var saveCodes = await _apiDatabaseService.LoadUserSaveCodesAsync(userId);
                
                if (saveCodes.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("API에서 세이브 코드를 찾을 수 없음");
                    return null;
                }

                System.Diagnostics.Debug.WriteLine($"API에서 {saveCodes.Count}개 세이브 코드 로드 완료");
                
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
                System.Diagnostics.Debug.WriteLine($"CacheService LoadCacheAsync 오류: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 캐릭터별 세이브 코드 로드
        /// </summary>
        public async Task<List<SaveCodeInfo>> LoadCharacterSaveCodesAsync(string userId)
        {
            try
            {
                return await _apiDatabaseService.LoadUserSaveCodesAsync(userId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadCharacterSaveCodesAsync 오류: {ex.Message}");
                return new List<SaveCodeInfo>();
            }
        }

        /// <summary>
        /// API 서버 연결을 테스트합니다
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
        /// API 서버 초기화를 테스트합니다
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
        /// API를 통해 데이터베이스 정보를 가져옵니다
        /// </summary>
        public async Task<string> GetCacheInfoAsync()
        {
            try
            {
                return await _apiDatabaseService.GetDatabaseInfoAsync();
            }
            catch (Exception ex)
            {
                return $"API 기반 데이터베이스 정보 조회 실패:\n{ex.Message}";
            }
        }

        /// <summary>
        /// API를 통해 모든 사용자 데이터를 삭제합니다
        /// </summary>
        public async Task<bool> ClearAllDataAsync(string userId)
        {
            try
            {
                return await _apiDatabaseService.ClearAllDataAsync(userId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ClearAllDataAsync 오류: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 캐시 유효성을 검사합니다
        /// </summary>
        public bool IsCacheValid(AnalysisCache cache, string folderPath, IEnumerable<TxtFileInfo> currentFiles)
        {
            // API 기반에서는 항상 최신 데이터를 제공하므로 간단한 검증만 수행
            return cache != null && 
                   !string.IsNullOrEmpty(cache.FolderPath) && 
                   cache.SaveCodes.Count > 0;
        }
    }
}