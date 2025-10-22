namespace SaveCodeClassfication.Models
{
    // API 요청/응답 모델들을 정의합니다

    /// <summary>
    /// 로그인 요청 모델
    /// </summary>
    public class ApiLoginRequest
    {
        public string UserId { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    /// <summary>
    /// 로그인 응답 모델
    /// </summary>
    public class ApiLoginResponse
    {
        public bool IsValid { get; set; }
        public bool IsAdmin { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
    }

    /// <summary>
    /// 회원가입 요청 모델
    /// </summary>
    public class ApiRegisterRequest
    {
        public string UserId { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string AuthKey { get; set; } = string.Empty;
    }

    /// <summary>
    /// 회원가입 응답 모델
    /// </summary>
    public class ApiRegisterResponse
    {
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }

    /// <summary>
    /// 검증 요청 모델
    /// </summary>
    public class ApiValidationRequest
    {
        public string UserId { get; set; } = string.Empty;
        public string AuthKey { get; set; } = string.Empty;
    }

    /// <summary>
    /// 검증 응답 모델
    /// </summary>
    public class ApiValidationResponse
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }

    /// <summary>
    /// 토큰 생성 요청 모델
    /// </summary>
    public class ApiTokenCreateRequest
    {
        public DateTime EffectiveDate { get; set; }
        public string Token { get; set; } = string.Empty;
    }

    /// <summary>
    /// 토큰 생성 응답 모델
    /// </summary>
    public class ApiTokenCreateResponse
    {
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public string GeneratedToken { get; set; } = string.Empty;
    }

    /// <summary>
    /// 토큰 수정 요청 모델
    /// </summary>
    public class ApiTokenUpdateRequest
    {
        public string AuthToken { get; set; } = string.Empty;
        public DateTime NewEffectiveDate { get; set; }
    }

    /// <summary>
    /// 토큰 수정 응답 모델
    /// </summary>
    public class ApiTokenUpdateResponse
    {
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }

    /// <summary>
    /// API 토큰 정보 모델
    /// </summary>
    public class ApiAuthToken
    {
        public string AuthTokens { get; set; } = string.Empty;
        public DateTime EffectiveDate { get; set; }
        public string UseYn { get; set; } = "N";
        public DateTime CreateDtm { get; set; }
        
        /// <summary>
        /// 사용 여부 (Boolean)
        /// </summary>
        public bool IsUsed => UseYn?.ToUpper() == "Y";

        /// <summary>
        /// 유효성 여부
        /// </summary>
        public bool IsValid => DateTime.Now.Date <= EffectiveDate.Date;

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

    /// <summary>
    /// API용 세이브 코드 정보 모델
    /// </summary>
    public class ApiSaveCodeInfo
    {
        public string CharacterName { get; set; } = string.Empty;
        public string SaveCode { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public DateTime FileDate { get; set; }
        public string FullContent { get; set; } = string.Empty;
        public string Level { get; set; } = string.Empty;
        public string Gold { get; set; } = string.Empty;
        public string Wood { get; set; } = string.Empty;
        public string Experience { get; set; } = string.Empty;
        public string PhysicalPower { get; set; } = string.Empty;
        public string MagicalPower { get; set; } = string.Empty;
        public string SpiritualPower { get; set; } = string.Empty;
        public List<string> Items { get; set; } = new();
        public string ItemsDisplayText { get; set; } = string.Empty;
    }

    /// <summary>
    /// 세이브 코드 저장 요청 모델
    /// </summary>
    public class ApiSaveCodeRequest
    {
        public string FolderPath { get; set; } = string.Empty;
        public List<ApiSaveCodeInfo> SaveCodes { get; set; } = new();
        public string UserKey { get; set; } = "default_user";
    }

    /// <summary>
    /// 세이브 코드 저장 응답 모델
    /// </summary>
    public class ApiSaveCodeResponse
    {
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public int SuccessCount { get; set; }
        public int ErrorCount { get; set; }
    }

    /// <summary>
    /// 데이터베이스 정보 응답 모델
    /// </summary>
    public class ApiDatabaseInfoResponse
    {
        public int TotalSaves { get; set; }
        public int TotalUsers { get; set; }
        public DateTime? LastSaveDate { get; set; }
        public DateTime? FirstSaveDate { get; set; }
        public string DatabaseInfo { get; set; } = string.Empty;
    }
}