namespace SaveCodeClassfication.Models
{
    /// <summary>
    /// ȸ������ ����� ��Ÿ���� Ŭ����
    /// </summary>
    public class RegisterResult
    {
        /// <summary>
        /// ȸ������ ���� ����
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// ���� �޽��� (���� ��)
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;
    }
}