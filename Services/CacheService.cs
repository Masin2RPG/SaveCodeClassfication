using System.Collections.Generic;
using SaveCodeClassfication.Models;

namespace SaveCodeClassfication.Services
{
    /// <summary>
    /// 분석 결과 저장소를 관리하는 서비스 (데이터베이스 기반)
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
        /// 캐시를 로드합니다 (데이터베이스에서)
        /// </summary>
        public Task<AnalysisCache?> LoadCacheAsync()
        {
            // 이 메서드는 폴더 경로가 필요하므로 더 이상 사용하지 않음
            // LoadCacheAsync(string folderPath)를 사용해야 함
            return Task.FromResult<AnalysisCache?>(null);
        }

        /// <summary>
        /// 특정 폴더의 캐시를 로드합니다
        /// </summary>
        public async Task<AnalysisCache?> LoadCacheAsync(string folderPath)
        {
            return await _databaseService.LoadAnalysisAsync(folderPath);
        }

        /// <summary>
        /// 캐시를 저장합니다 (데이터베이스에)
        /// </summary>
        public async Task<bool> SaveCacheAsync(AnalysisCache cache)
        {
            var fileHashes = cache.FileHashes ?? new Dictionary<string, DateTime>();
            
            // 현재 사용자 키 가져오기
            var userKey = await GetCurrentUserKeyAsync();
            
            return await _databaseService.SaveAnalysisAsync(cache.FolderPath, cache.SaveCodes, fileHashes, userKey);
        }

        /// <summary>
        /// 캐시를 삭제합니다 (데이터베이스의 사용자 데이터)
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
        /// 저장된 캐릭터별 세이브코드를 조회합니다
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
                System.Diagnostics.Debug.WriteLine($"캐릭터별 세이브코드 조회 실패: {ex.Message}");
                return new List<SaveCodeInfo>();
            }
        }

        /// <summary>
        /// 특정 캐릭터의 세이브코드만 조회합니다
        /// </summary>
        public async Task<List<SaveCodeInfo>> LoadSaveCodesByCharacterAsync(string characterName)
        {
            try
            {
                var userKey = await GetCurrentUserKeyAsync();
                var allSaveCodes = await _databaseService.LoadUserSaveCodesAsync(userKey);
                
                // 캐릭터명으로 필터링
                return allSaveCodes.Where(sc => sc.CharacterName.Equals(characterName, StringComparison.OrdinalIgnoreCase))
                                  .OrderByDescending(sc => sc.FileDate)
                                  .ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"특정 캐릭터({characterName}) 세이브코드 조회 실패: {ex.Message}");
                return new List<SaveCodeInfo>();
            }
        }

        /// <summary>
        /// 저장된 캐릭터 목록을 조회합니다
        /// </summary>
        public async Task<List<string>> LoadCharacterListAsync()
        {
            try
            {
                var userKey = await GetCurrentUserKeyAsync();
                var allSaveCodes = await _databaseService.LoadUserSaveCodesAsync(userKey);
                
                // 캐릭터명 중복 제거하고 정렬
                return allSaveCodes.Select(sc => sc.CharacterName)
                                  .Distinct()
                                  .OrderBy(name => name)
                                  .ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"캐릭터 목록 조회 실패: {ex.Message}");
                return new List<string>();
            }
        }

        /// <summary>
        /// 캐릭터별 세이브코드 통계를 가져옵니다
        /// </summary>
        public async Task<Dictionary<string, int>> GetCharacterSaveCodeStatsAsync()
        {
            try
            {
                var userKey = await GetCurrentUserKeyAsync();
                var allSaveCodes = await _databaseService.LoadUserSaveCodesAsync(userKey);
                
                // 캐릭터별 세이브코드 개수 계산
                return allSaveCodes.GroupBy(sc => sc.CharacterName)
                                  .ToDictionary(g => g.Key, g => g.Count());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"캐릭터별 통계 조회 실패: {ex.Message}");
                return new Dictionary<string, int>();
            }
        }

        /// <summary>
        /// 캐시가 유효한지 확인합니다
        /// </summary>
        public bool IsCacheValid(AnalysisCache cache, string folderPath, IEnumerable<TxtFileInfo> currentFiles)
        {
            return _databaseService.IsCacheValid(cache, folderPath, currentFiles);
        }

        /// <summary>
        /// 캐시 정보를 반환합니다 (데이터베이스 정보)
        /// </summary>
        public async Task<string> GetCacheInfoAsync()
        {
            return await _databaseService.GetDatabaseInfoAsync();
        }

        /// <summary>
        /// 데이터베이스 연결을 테스트합니다
        /// </summary>
        public async Task<bool> TestDatabaseConnectionAsync()
        {
            return await _databaseService.TestConnectionAsync();
        }

        /// <summary>
        /// 데이터베이스를 초기화합니다
        /// </summary>
        public async Task<bool> InitializeDatabaseAsync()
        {
            return await _databaseService.InitializeDatabaseAsync();
        }

        /// <summary>
        /// 현재 사용자 키를 가져옵니다
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
                    // 설정 로드 실패 시 기본값 사용
                }
            }
            
            return "default_user";
        }
    }
}