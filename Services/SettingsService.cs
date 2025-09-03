using System.IO;
using System.Text;
using System.Text.Json;
using SaveCodeClassfication.Models;

namespace SaveCodeClassfication.Services
{
    /// <summary>
    /// ���ø����̼� ������ �����ϴ� ����
    /// </summary>
    public class SettingsService
    {
        private readonly string _configFilePath;

        public SettingsService(string configFilePath)
        {
            _configFilePath = configFilePath;
        }

        /// <summary>
        /// ������ �ε��մϴ�
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
        /// ������ �����մϴ�
        /// </summary>
        public async Task<bool> SaveSettingsAsync(AppSettings settings)
        {
            try
            {
                // ���� ���丮�� ������ ����
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