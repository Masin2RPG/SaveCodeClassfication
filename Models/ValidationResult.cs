namespace SaveCodeClassfication.Models
{
    /// <summary>
    /// ���� ����� ��Ÿ���� Ŭ����
    /// </summary>
    public class ValidationResult
    {
        /// <summary>
        /// ���� ���� ����
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// ���� �޽��� (���� ��)
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;
    }
}