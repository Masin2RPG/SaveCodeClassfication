using System.IO;
using System.Text;
using System.Text.Json;
using SaveCodeClassfication.Models;

namespace SaveCodeClassfication.Services
{
    /// <summary>
    /// 캐릭터 이름 매핑을 관리하는 서비스
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
        /// 캐릭터 이름 매핑을 로드합니다
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
                        if (mapping.TryGetValue("마신", out var masinObj) && 
                            mapping.TryGetValue("이름", out var nameObj))
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
        /// 이름 매핑을 처리합니다 (배열, 문자열, 쉼표 구분 모두 지원)
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
        /// JsonElement 타입의 이름을 처리합니다
        /// </summary>
        private void ProcessJsonElement(JsonElement nameElement, string masin)
        {
            if (nameElement.ValueKind == JsonValueKind.Array)
            {
                // 배열 형태인 경우
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
                // 문자열 형태인 경우
                var nameStr = nameElement.GetString();
                ProcessStringName(nameStr, masin);
            }
        }

        /// <summary>
        /// 문자열 타입의 이름을 처리합니다
        /// </summary>
        private void ProcessStringName(string? nameStr, string masin)
        {
            if (string.IsNullOrEmpty(nameStr)) return;

            if (nameStr.Contains(','))
            {
                // 쉼표로 구분된 여러 이름
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
                // 단일 이름
                _characterNameMappings[nameStr] = masin;
            }
        }

        /// <summary>
        /// 원본 캐릭터명을 표시용 이름으로 변환합니다
        /// </summary>
        public string GetDisplayCharacterName(string originalName)
        {
            return _characterNameMappings.TryGetValue(originalName, out var mappedName) 
                ? mappedName 
                : originalName;
        }

        /// <summary>
        /// 로드된 매핑의 개수를 반환합니다
        /// </summary>
        public int GetMappingCount() => _characterNameMappings.Count;
    }
}