using System;

namespace SaveCodeClassfication.Models
{
    /// <summary>
    /// 토큰 정보 모델
    /// </summary>
    public class TokenInfo
    {
        /// <summary>
        /// 인증 토큰
        /// </summary>
        public string Auth_tokens { get; set; } = string.Empty;

        /// <summary>
        /// 유효 날짜
        /// </summary>
        public DateTime Effective_Date { get; set; }

        /// <summary>
        /// 사용 여부 (Y/N)
        /// </summary>
        public string Use_Yn { get; set; } = "N";

        /// <summary>
        /// 생성 날짜 (DB에 컬럼이 없으면 현재 날짜로 설정)
        /// </summary>
        public DateTime Create_DTM { get; set; } = DateTime.Now;

        /// <summary>
        /// 사용 여부 (Boolean)
        /// </summary>
        public bool IsUsed => Use_Yn?.ToUpper() == "Y";

        /// <summary>
        /// 유효성 여부
        /// </summary>
        public bool IsValid => DateTime.Now.Date <= Effective_Date.Date;

        /// <summary>
        /// 상태 텍스트
        /// </summary>
        public string StatusText
        {
            get
            {
                if (IsUsed) return "사용됨";
                if (!IsValid) return "만료됨";
                return "사용가능";
            }
        }
    }
}