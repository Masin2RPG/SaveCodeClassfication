using System.IO;

namespace SaveCodeClassfication.Utils
{
    /// <summary>
    /// ���ø����̼� ��� ���� ���
    /// </summary>
    public static class PathConstants
    {
        /// <summary>
        /// ���� ���� ���
        /// </summary>
        public static readonly string ConfigFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SaveCodeClassification",
            "config.json"
        );

        /// <summary>
        /// ĳ�� ���� ���
        /// </summary>
        public static readonly string CacheFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SaveCodeClassification",
            "analysis_cache.json"
        );

        /// <summary>
        /// ĳ���� �̸� ���� ���� ���
        /// </summary>
        public static readonly string CharNameMappingPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "charName.json"
        );
    }
}