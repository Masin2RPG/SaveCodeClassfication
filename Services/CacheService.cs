using System.IO;
using System.Text;
using System.Text.Json;
using SaveCodeClassfication.Models;

namespace SaveCodeClassfication.Services
{
    /// <summary>
    /// 분석 결과 캐시를 관리하는 서비스
    /// </summary>
    public class CacheService
    {
        private readonly string _cacheFilePath;

        public CacheService(string cacheFilePath)
        {
            _cacheFilePath = cacheFilePath;
        }

        /// <summary>
        /// 캐시를 로드합니다
        /// </summary>
        public async Task<AnalysisCache?> LoadCacheAsync()
        {
            try
            {
                if (!File.Exists(_cacheFilePath))
                {
                    return null;
                }

                var jsonString = await File.ReadAllTextAsync(_cacheFilePath, Encoding.UTF8);
                var cache = JsonSerializer.Deserialize<AnalysisCache>(jsonString);
                return cache;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 캐시를 저장합니다
        /// </summary>
        public async Task<bool> SaveCacheAsync(AnalysisCache cache)
        {
            try
            {
                // 캐시 디렉토리가 없으면 생성
                var cacheDir = Path.GetDirectoryName(_cacheFilePath);
                if (!Directory.Exists(cacheDir))
                {
                    Directory.CreateDirectory(cacheDir!);
                }

                var jsonString = JsonSerializer.Serialize(cache, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });

                await File.WriteAllTextAsync(_cacheFilePath, jsonString, Encoding.UTF8);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 캐시를 삭제합니다
        /// </summary>
        public bool DeleteCache()
        {
            try
            {
                if (File.Exists(_cacheFilePath))
                {
                    File.Delete(_cacheFilePath);
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 캐시가 유효한지 확인합니다
        /// </summary>
        public bool IsCacheValid(AnalysisCache cache, string folderPath, IEnumerable<TxtFileInfo> currentFiles)
        {
            try
            {
                // 폴더 경로가 다르면 캐시 무효
                if (cache.FolderPath != folderPath)
                    return false;

                // 파일 개수가 다르면 캐시 무효
                if (cache.TotalFiles != currentFiles.Count())
                    return false;

                // 각 파일의 수정 시간 확인
                foreach (var file in currentFiles)
                {
                    var fileName = file.FileName;
                    var lastWriteTime = File.GetLastWriteTime(file.FilePath);

                    if (!cache.FileHashes.ContainsKey(fileName) || 
                        cache.FileHashes[fileName] != lastWriteTime)
                    {
                        return false;
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 캐시 정보를 가져옵니다
        /// </summary>
        public async Task<string> GetCacheInfoAsync()
        {
            try
            {
                if (File.Exists(_cacheFilePath))
                {
                    var cache = await LoadCacheAsync();
                    if (cache != null)
                    {
                        var cacheAge = DateTime.Now - cache.LastAnalyzed;
                        var ageText = cacheAge.TotalDays >= 1 
                            ? $"{(int)cacheAge.TotalDays}일 전" 
                            : cacheAge.TotalHours >= 1 
                                ? $"{(int)cacheAge.TotalHours}시간 전" 
                                : $"{(int)cacheAge.TotalMinutes}분 전";
                        
                        return $"캐시 있음: {cache.SaveCodes.Count}개 세이브코드 ({ageText})";
                    }
                    else
                    {
                        return "캐시 상태: 손상된 캐시 파일";
                    }
                }
                else
                {
                    return "캐시 상태: 캐시 없음";
                }
            }
            catch
            {
                return "캐시 상태: 확인 실패";
            }
        }
    }
}