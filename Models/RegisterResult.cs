namespace SaveCodeClassfication.Models
{
    /// <summary>
    /// 회원가입 결과를 나타내는 클래스
    /// </summary>
    public class RegisterResult
    {
        /// <summary>
        /// 회원가입 성공 여부
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// 오류 메시지 (실패 시)
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;
    }
}