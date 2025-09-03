namespace SaveCodeClassfication.Utils
{
    /// <summary>
    /// 파일 관련 유틸리티 메서드
    /// </summary>
    public static class FileUtils
    {
        /// <summary>
        /// 파일 크기를 사람이 읽기 쉬운 형태로 포맷팅합니다
        /// </summary>
        public static string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            
            return $"{len:0.##} {sizes[order]}";
        }

        /// <summary>
        /// 숫자를 천 단위로 구분하여 포맷팅합니다
        /// </summary>
        public static string FormatNumber(string numberStr)
        {
            if (long.TryParse(numberStr, out long number))
            {
                return number.ToString("#,##0");
            }
            return numberStr;
        }
    }
}