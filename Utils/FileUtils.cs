namespace SaveCodeClassfication.Utils
{
    /// <summary>
    /// ���� ���� ��ƿ��Ƽ �޼���
    /// </summary>
    public static class FileUtils
    {
        /// <summary>
        /// ���� ũ�⸦ ����� �б� ���� ���·� �������մϴ�
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
        /// ���ڸ� õ ������ �����Ͽ� �������մϴ�
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