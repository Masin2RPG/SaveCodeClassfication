namespace SaveCodeClassfication.Models
{
    /// <summary>
    /// ��ū ���� ����� ��Ÿ���� Ŭ����
    /// </summary>
    public class TokenUpdateResult
    {
        /// <summary>
        /// ���� ���� ����
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// ���� �޽��� (���� ��)
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;
    }
}