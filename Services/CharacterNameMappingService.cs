using System.IO;
using System.Text;
using System.Text.Json;
using SaveCodeClassfication.Models;

namespace SaveCodeClassfication.Services
{
    /// <summary>
    /// ĳ���� �̸� ������ �����ϴ� ����
    /// </summary>
    public class CharacterNameMappingService
    {
        private readonly Dictionary<string, string> _characterNameMappings = new();
        private readonly string _mappingFilePath;

        public CharacterNameMappingService(string mappingFilePath)
        {
            _mappingFilePath = mappingFilePath;
        }

        /// <summary>
        /// ĳ���� �̸� ������ �ε��մϴ�
        /// </summary>
        public async Task<bool> LoadMappingsAsync()
        {
            try
            {
                if (!File.Exists(_mappingFilePath))
                {
                    return false;
                }

                var jsonString = await File.ReadAllTextAsync(_mappingFilePath, Encoding.UTF8);
                var mappings = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(jsonString);
                
                _characterNameMappings.Clear();
                
                if (mappings != null)
                {
                    foreach (var mapping in mappings)
                    {
                        if (mapping.TryGetValue("����", out var masinObj) && 
                            mapping.TryGetValue("�̸�", out var nameObj))
                        {
                            var masin = masinObj?.ToString();
                            if (string.IsNullOrEmpty(masin)) continue;

                            await ProcessNameMappingAsync(nameObj, masin);
                        }
                    }
                }
                
                return true;
            }
            catch
            {
                _characterNameMappings.Clear();
                return false;
            }
        }

        /// <summary>
        /// �̸� ������ ó���մϴ� (�迭, ���ڿ�, ��ǥ ���� ��� ����)
        /// </summary>
        private async Task ProcessNameMappingAsync(object nameObj, string masin)
        {
            await Task.Run(() =>
            {
                if (nameObj is JsonElement nameElement)
                {
                    ProcessJsonElement(nameElement, masin);
                }
                else
                {
                    ProcessStringName(nameObj?.ToString(), masin);
                }
            });
        }

        /// <summary>
        /// JsonElement Ÿ���� �̸��� ó���մϴ�
        /// </summary>
        private void ProcessJsonElement(JsonElement nameElement, string masin)
        {
            if (nameElement.ValueKind == JsonValueKind.Array)
            {
                // �迭 ������ ���
                foreach (var item in nameElement.EnumerateArray())
                {
                    var name = item.GetString();
                    if (!string.IsNullOrEmpty(name))
                    {
                        _characterNameMappings[name] = masin;
                    }
                }
            }
            else if (nameElement.ValueKind == JsonValueKind.String)
            {
                // ���ڿ� ������ ���
                var nameStr = nameElement.GetString();
                ProcessStringName(nameStr, masin);
            }
        }

        /// <summary>
        /// ���ڿ� Ÿ���� �̸��� ó���մϴ�
        /// </summary>
        private void ProcessStringName(string? nameStr, string masin)
        {
            if (string.IsNullOrEmpty(nameStr)) return;

            if (nameStr.Contains(','))
            {
                // ��ǥ�� ���е� ���� �̸�
                var names = nameStr.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var name in names)
                {
                    var trimmedName = name.Trim();
                    if (!string.IsNullOrEmpty(trimmedName))
                    {
                        _characterNameMappings[trimmedName] = masin;
                    }
                }
            }
            else
            {
                // ���� �̸�
                _characterNameMappings[nameStr] = masin;
            }
        }

        /// <summary>
        /// ���� ĳ���͸��� ǥ�ÿ� �̸����� ��ȯ�մϴ�
        /// </summary>
        public string GetDisplayCharacterName(string originalName)
        {
            return _characterNameMappings.TryGetValue(originalName, out var mappedName) 
                ? mappedName 
                : originalName;
        }

        /// <summary>
        /// �ε�� ������ ������ ��ȯ�մϴ�
        /// </summary>
        public int GetMappingCount() => _characterNameMappings.Count;
    }
}