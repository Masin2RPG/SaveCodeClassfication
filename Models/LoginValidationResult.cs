namespace SaveCodeClassfication.Models
{
    /// <summary>
    /// �α��� ���� ����� ��Ÿ���� Ŭ����
    /// </summary>
    public class LoginValidationResult
    {
        /// <summary>
        /// �α��� ���� ���� ����
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// ������ ���� ����
        /// </summary>
        public bool IsAdmin { get; set; }

        /// <summary>
        /// ���� �޽��� (���� ��)
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;
    }
}