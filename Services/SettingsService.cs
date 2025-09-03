using System.IO;
using System.Text;
using System.Text.Json;
using SaveCodeClassfication.Models;

namespace SaveCodeClassfication.Services
{
    /// <summary>
    /// 애플리케이션 설정을 관리하는 서비스
    /// </summary>
    public class SettingsService
    {
        private readonly string _configFilePath;

        public SettingsService(string configFilePath)
        {
            _configFilePath = configFilePath;
        }

        /// <summary>
        /// 설정을 로드합니다
        /// </summary>
        public async Task<AppSettings> LoadSettingsAsync()
        {
            try
            {
                if (!File.Exists(_configFilePath))
                {
                    return new AppSettings();
                }

                var jsonString = await File.ReadAllTextAsync(_configFilePath, Encoding.UTF8);
                var settings = JsonSerializer.Deserialize<AppSettings>(jsonString);
                return settings ?? new AppSettings();
            }
            catch
            {
                return new AppSettings();
            }
        }

        /// <summary>
        /// 설정을 저장합니다
        /// </summary>
        public async Task<bool> SaveSettingsAsync(AppSettings settings)
        {
            try
            {
                // 설정 디렉토리가 없으면 생성
                var configDir = Path.GetDirectoryName(_configFilePath);
                if (!Directory.Exists(configDir))
                {
                    Directory.CreateDirectory(configDir!);
                }

                var jsonString = JsonSerializer.Serialize(settings, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });

                await File.WriteAllTextAsync(_configFilePath, jsonString, Encoding.UTF8);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}