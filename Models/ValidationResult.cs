namespace SaveCodeClassfication.Models
{
    /// <summary>
    /// 검증 결과를 나타내는 클래스
    /// </summary>
    public class ValidationResult
    {
        /// <summary>
        /// 검증 성공 여부
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// 오류 메시지 (실패 시)
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;
    }
}