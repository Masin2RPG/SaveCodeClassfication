namespace SaveCodeClassfication.Models
{
    /// <summary>
    /// 토큰 수정 결과를 나타내는 클래스
    /// </summary>
    public class TokenUpdateResult
    {
        /// <summary>
        /// 수정 성공 여부
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// 오류 메시지 (실패 시)
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;
    }
}