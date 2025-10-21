namespace SaveCodeClassfication.Models
{
    /// <summary>
    /// 토큰 생성 결과를 나타내는 클래스
    /// </summary>
    public class TokenCreationResult
    {
        /// <summary>
        /// 토큰 생성 성공 여부
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// 오류 메시지 (실패 시)
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// 생성된 토큰
        /// </summary>
        public string GeneratedToken { get; set; } = string.Empty;
    }
}