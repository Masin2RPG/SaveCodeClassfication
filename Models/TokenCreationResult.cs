namespace SaveCodeClassfication.Models
{
    /// <summary>
    /// ��ū ���� ����� ��Ÿ���� Ŭ����
    /// </summary>
    public class TokenCreationResult
    {
        /// <summary>
        /// ��ū ���� ���� ����
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// ���� �޽��� (���� ��)
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// ������ ��ū
        /// </summary>
        public string GeneratedToken { get; set; } = string.Empty;
    }
}