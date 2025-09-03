using System.IO;

namespace SaveCodeClassfication.Utils
{
    /// <summary>
    /// 애플리케이션 경로 관련 상수
    /// </summary>
    public static class PathConstants
    {
        /// <summary>
        /// 설정 파일 경로
        /// </summary>
        public static readonly string ConfigFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SaveCodeClassification",
            "config.json"
        );

        /// <summary>
        /// 캐시 파일 경로
        /// </summary>
        public static readonly string CacheFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SaveCodeClassification",
            "analysis_cache.json"
        );

        /// <summary>
        /// 캐릭터 이름 매핑 파일 경로
        /// </summary>
        public static readonly string CharNameMappingPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "charName.json"
        );
    }
}