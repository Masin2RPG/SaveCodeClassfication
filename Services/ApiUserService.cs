using SaveCodeClassfication.Models;
using System.Linq;
using System.Text.Json;
using System.Net.Http;

namespace SaveCodeClassfication.Services
{
    /// <summary>
    /// API 기반 사용자 서비스
    /// </summary>
    public class ApiUserService : ApiService
    {
        private readonly JsonSerializerOptions _jsonOptions;
        
        public ApiUserService()
        {
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true
            };
        }
        
        /// <summary>
        /// HTTP 클라이언트 생성
        /// </summary>
        private HttpClient CreateHttpClient()
        {
            var client = new HttpClient();
            client.BaseAddress = new Uri("http://211.202.189.93:5036"); // API 서버 기본 주소
            client.Timeout = TimeSpan.FromSeconds(30);
            return client;
        }

        /// <summary>
        /// 사용자 로그인 검증
        /// </summary>
        public async Task<LoginValidationResult> ValidateUserWithDetailsAsync(string userId, string password)
        {
            try
            {
                var request = new ApiLoginRequest
                {
                    UserId = userId,
                    Password = password
                };

                var response = await PostAsync<ApiLoginRequest, ApiLoginResponse>("auth/login", request);

                if (response != null)
                {
                    return new LoginValidationResult
                    {
                        IsValid = response.IsValid,
                        IsAdmin = response.IsAdmin,
                        ErrorMessage = response.ErrorMessage
                    };
                }

                return new LoginValidationResult
                {
                    IsValid = false,
                    IsAdmin = false,
                    ErrorMessage = "서버 통신 오류가 발생했습니다."
                };
            }
            catch (Exception ex)
            {
                return new LoginValidationResult
                {
                    IsValid = false,
                    IsAdmin = false,
                    ErrorMessage = $"로그인 처리 중 오류가 발생했습니다: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 회원가입 검증
        /// </summary>
        public async Task<ValidationResult> ValidateRegistrationAsync(string userId, string authKey)
        {
            try
            {
                var request = new ApiValidationRequest
                {
                    UserId = userId,
                    AuthKey = authKey
                };

                var response = await PostAsync<ApiValidationRequest, ApiValidationResponse>("auth/validate", request);

                if (response != null)
                {
                    return new ValidationResult
                    {
                        IsValid = response.IsValid,
                        ErrorMessage = response.ErrorMessage
                    };
                }

                return new ValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "서버 통신 오류가 발생했습니다."
                };
            }
            catch (Exception ex)
            {
                return new ValidationResult
                {
                    IsValid = false,
                    ErrorMessage = $"검증 중 오류가 발생했습니다: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 사용자 회원가입
        /// </summary>
        public async Task<RegisterResult> RegisterUserAsync(string userId, string password, string authKey)
        {
            try
            {
                var request = new ApiRegisterRequest
                {
                    UserId = userId,
                    Password = password,
                    AuthKey = authKey
                };

                var response = await PostAsync<ApiRegisterRequest, ApiRegisterResponse>("auth/register", request);

                if (response != null)
                {
                    return new RegisterResult
                    {
                        IsSuccess = response.IsSuccess,
                        ErrorMessage = response.ErrorMessage
                    };
                }

                return new RegisterResult
                {
                    IsSuccess = false,
                    ErrorMessage = "서버 통신 오류가 발생했습니다."
                };
            }
            catch (Exception ex)
            {
                return new RegisterResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"회원가입 처리 중 오류가 발생했습니다: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 랜덤 토큰 생성
        /// </summary>
        public async Task<string> GenerateRandomTokenAsync(int length = 16)
        {
            try
            {
                var token = await GetAsync<string>($"auth/generate-token?length={length}");
                return token ?? GenerateRandomTokenLocal(length);
            }
            catch (Exception)
            {
                // API 호출 실패 시 로컬에서 생성
                return GenerateRandomTokenLocal(length);
            }
        }

        /// <summary>
        /// 토큰 생성 (DB 저장)
        /// </summary>
        public async Task<TokenCreationResult> CreateAuthTokenAsync(DateTime effectiveDate, string token)
        {
            try
            {
                var request = new ApiTokenCreateRequest
                {
                    EffectiveDate = effectiveDate,
                    Token = token
                };

                var response = await PostAsync<ApiTokenCreateRequest, ApiTokenCreateResponse>("token/create", request);

                if (response != null)
                {
                    return new TokenCreationResult
                    {
                        IsSuccess = response.IsSuccess,
                        ErrorMessage = response.ErrorMessage,
                        GeneratedToken = response.GeneratedToken
                    };
                }

                return new TokenCreationResult
                {
                    IsSuccess = false,
                    ErrorMessage = "서버 통신 오류가 발생했습니다.",
                    GeneratedToken = token
                };
            }
            catch (Exception ex)
            {
                return new TokenCreationResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"토큰 생성 중 오류가 발생했습니다: {ex.Message}",
                    GeneratedToken = token
                };
            }
        }

        /// <summary>
        /// 모든 토큰 목록 조회
        /// </summary>
        public async Task<List<TokenInfo>> GetAllTokensAsync()
        {
            try
            {
                Console.WriteLine("?? [ApiUserService] 토큰 목록 조회 시작");
                
                using var client = CreateHttpClient();
                var response = await client.GetAsync("/api/token/list");

                if (response.IsSuccessStatusCode)
                {
                    var jsonContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"?? API 응답 내용: {jsonContent}");
                    
                    // API에서 AuthToken 형태로 받아서 TokenInfo로 변환
                    var apiTokens = JsonSerializer.Deserialize<List<AuthTokenApiResponse>>(jsonContent, _jsonOptions);
                    
                    if (apiTokens != null)
                    {
                        var tokenInfos = apiTokens.Select(token => new TokenInfo
                        {
                            Auth_tokens = token.Auth_tokens ?? "",
                            Effective_Date = token.Effective_Date,
                            Use_Yn = token.Use_Yn ?? "N"
                            // StatusText는 TokenInfo의 계산된 속성으로 자동 생성됨
                        }).ToList();
                        
                        Console.WriteLine($"? 토큰 목록 조회 성공: {tokenInfos.Count}개");
                        foreach (var token in tokenInfos)
                        {
                            Console.WriteLine($"   ?? 토큰: {token.Auth_tokens}, 유효날짜: {token.Effective_Date:yyyy-MM-dd}, 상태: {token.StatusText}");
                        }
                        
                        return tokenInfos;
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"? 토큰 목록 조회 실패: {response.StatusCode}");
                    Console.WriteLine($"?? 오류 내용: {errorContent}");
                }

                return new List<TokenInfo>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"? 토큰 목록 조회 중 예외 발생: {ex.Message}");
                Console.WriteLine($"?? 상세 오류: {ex.StackTrace}");
                return new List<TokenInfo>();
            }
        }
        
        /// <summary>
        /// API 응답용 AuthToken 모델
        /// </summary>
        private class AuthTokenApiResponse
        {
            public string Auth_tokens { get; set; } = string.Empty;
            public DateTime Effective_Date { get; set; }
            public string Use_Yn { get; set; } = "N";
            public string StatusText { get; set; } = string.Empty; // API에서 계산된 값
        }

        /// <summary>
        /// 토큰 유효날짜 수정
        /// </summary>
        public async Task<TokenUpdateResult> UpdateTokenEffectiveDateAsync(string authToken, DateTime newEffectiveDate)
        {
            try
            {
                var request = new ApiTokenUpdateRequest
                {
                    AuthToken = authToken,
                    NewEffectiveDate = newEffectiveDate
                };

                var response = await PutAsync<ApiTokenUpdateRequest, ApiTokenUpdateResponse>("token/update", request);

                if (response != null)
                {
                    return new TokenUpdateResult
                    {
                        IsSuccess = response.IsSuccess,
                        ErrorMessage = response.ErrorMessage
                    };
                }

                return new TokenUpdateResult
                {
                    IsSuccess = false,
                    ErrorMessage = "서버 통신 오류가 발생했습니다."
                };
            }
            catch (Exception ex)
            {
                return new TokenUpdateResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"토큰 수정 중 오류가 발생했습니다: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 토큰 삭제
        /// </summary>
        public async Task<TokenUpdateResult> DeleteTokenAsync(string authToken)
        {
            try
            {
                var response = await DeleteAsync<ApiTokenUpdateResponse>($"token/{authToken}");

                if (response != null)
                {
                    return new TokenUpdateResult
                    {
                        IsSuccess = response.IsSuccess,
                        ErrorMessage = response.ErrorMessage
                    };
                }

                return new TokenUpdateResult
                {
                    IsSuccess = false,
                    ErrorMessage = "서버 통신 오류가 발생했습니다."
                };
            }
            catch (Exception ex)
            {
                return new TokenUpdateResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"토큰 삭제 중 오류가 발생했습니다: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 테이블 존재 여부 확인 (API용 더미 메서드)
        /// </summary>
        public async Task<bool> CheckTokenTableExistsAsync()
        {
            try
            {
                // API를 통해 토큰 목록을 조회해보고 성공하면 테이블이 존재한다고 판단
                var tokens = await GetAllTokensAsync();
                return true; // API 호출이 성공하면 테이블이 존재한다고 가정
            }
            catch (Exception)
            {
                return false;
            }
        }

        #region Private Methods

        /// <summary>
        /// 로컬 토큰 생성 (백업용)
        /// </summary>
        private string GenerateRandomTokenLocal(int length = 16)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        #endregion
    }
}