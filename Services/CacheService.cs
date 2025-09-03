using System.IO;
using System.Text;
using System.Text.Json;
using SaveCodeClassfication.Models;

namespace SaveCodeClassfication.Services
{
    /// <summary>
    /// �м� ��� ĳ�ø� �����ϴ� ����
    /// </summary>
    public class CacheService
    {
        private readonly string _cacheFilePath;

        public CacheService(string cacheFilePath)
        {
            _cacheFilePath = cacheFilePath;
        }

        /// <summary>
        /// ĳ�ø� �ε��մϴ�
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
        /// ĳ�ø� �����մϴ�
        /// </summary>
        public async Task<bool> SaveCacheAsync(AnalysisCache cache)
        {
            try
            {
                // ĳ�� ���丮�� ������ ����
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
        /// ĳ�ø� �����մϴ�
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
        /// ĳ�ð� ��ȿ���� Ȯ���մϴ�
        /// </summary>
        public bool IsCacheValid(AnalysisCache cache, string folderPath, IEnumerable<TxtFileInfo> currentFiles)
        {
            try
            {
                // ���� ��ΰ� �ٸ��� ĳ�� ��ȿ
                if (cache.FolderPath != folderPath)
                    return false;

                // ���� ������ �ٸ��� ĳ�� ��ȿ
                if (cache.TotalFiles != currentFiles.Count())
                    return false;

                // �� ������ ���� �ð� Ȯ��
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
        /// ĳ�� ������ �����ɴϴ�
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
                            ? $"{(int)cacheAge.TotalDays}�� ��" 
                            : cacheAge.TotalHours >= 1 
                                ? $"{(int)cacheAge.TotalHours}�ð� ��" 
                                : $"{(int)cacheAge.TotalMinutes}�� ��";
                        
                        return $"ĳ�� ����: {cache.SaveCodes.Count}�� ���̺��ڵ� ({ageText})";
                    }
                    else
                    {
                        return "ĳ�� ����: �ջ�� ĳ�� ����";
                    }
                }
                else
                {
                    return "ĳ�� ����: ĳ�� ����";
                }
            }
            catch
            {
                return "ĳ�� ����: Ȯ�� ����";
            }
        }
    }
}