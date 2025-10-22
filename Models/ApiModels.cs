namespace SaveCodeClassfication.Models
{
    // API ��û/���� �𵨵��� �����մϴ�

    /// <summary>
    /// �α��� ��û ��
    /// </summary>
    public class ApiLoginRequest
    {
        public string UserId { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    /// <summary>
    /// �α��� ���� ��
    /// </summary>
    public class ApiLoginResponse
    {
        public bool IsValid { get; set; }
        public bool IsAdmin { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
    }

    /// <summary>
    /// ȸ������ ��û ��
    /// </summary>
    public class ApiRegisterRequest
    {
        public string UserId { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string AuthKey { get; set; } = string.Empty;
    }

    /// <summary>
    /// ȸ������ ���� ��
    /// </summary>
    public class ApiRegisterResponse
    {
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }

    /// <summary>
    /// ���� ��û ��
    /// </summary>
    public class ApiValidationRequest
    {
        public string UserId { get; set; } = string.Empty;
        public string AuthKey { get; set; } = string.Empty;
    }

    /// <summary>
    /// ���� ���� ��
    /// </summary>
    public class ApiValidationResponse
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }

    /// <summary>
    /// ��ū ���� ��û ��
    /// </summary>
    public class ApiTokenCreateRequest
    {
        public DateTime EffectiveDate { get; set; }
        public string Token { get; set; } = string.Empty;
    }

    /// <summary>
    /// ��ū ���� ���� ��
    /// </summary>
    public class ApiTokenCreateResponse
    {
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public string GeneratedToken { get; set; } = string.Empty;
    }

    /// <summary>
    /// ��ū ���� ��û ��
    /// </summary>
    public class ApiTokenUpdateRequest
    {
        public string AuthToken { get; set; } = string.Empty;
        public DateTime NewEffectiveDate { get; set; }
    }

    /// <summary>
    /// ��ū ���� ���� ��
    /// </summary>
    public class ApiTokenUpdateResponse
    {
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }

    /// <summary>
    /// API ��ū ���� ��
    /// </summary>
    public class ApiAuthToken
    {
        public string AuthTokens { get; set; } = string.Empty;
        public DateTime EffectiveDate { get; set; }
        public string UseYn { get; set; } = "N";
        public DateTime CreateDtm { get; set; }
        
        /// <summary>
        /// ��� ���� (Boolean)
        /// </summary>
        public bool IsUsed => UseYn?.ToUpper() == "Y";

        /// <summary>
        /// ��ȿ�� ����
        /// </summary>
        public bool IsValid => DateTime.Now.Date <= EffectiveDate.Date;

        /// <summary>
        /// ���� �ؽ�Ʈ
        /// </summary>
        public string StatusText
        {
            get
            {
                if (IsUsed) return "����";
                if (!IsValid) return "�����";
                return "��밡��";
            }
        }
    }

    /// <summary>
    /// API�� ���̺� �ڵ� ���� ��
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
    /// ���̺� �ڵ� ���� ��û ��
    /// </summary>
    public class ApiSaveCodeRequest
    {
        public string FolderPath { get; set; } = string.Empty;
        public List<ApiSaveCodeInfo> SaveCodes { get; set; } = new();
        public string UserKey { get; set; } = "default_user";
    }

    /// <summary>
    /// ���̺� �ڵ� ���� ���� ��
    /// </summary>
    public class ApiSaveCodeResponse
    {
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public int SuccessCount { get; set; }
        public int ErrorCount { get; set; }
    }

    /// <summary>
    /// �����ͺ��̽� ���� ���� ��
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