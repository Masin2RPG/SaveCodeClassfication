using SaveCodeClassfication.Models;
using System.Linq;
using System.Text.Json;
using System.Net.Http;

namespace SaveCodeClassfication.Services
{
    /// <summary>
    /// API ��� ����� ����
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
        /// HTTP Ŭ���̾�Ʈ ����
        /// </summary>
        private HttpClient CreateHttpClient()
        {
            var client = new HttpClient();
            client.BaseAddress = new Uri("http://211.202.189.93:5036"); // API ���� �⺻ �ּ�
            client.Timeout = TimeSpan.FromSeconds(30);
            return client;
        }

        /// <summary>
        /// ����� �α��� ����
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
                    ErrorMessage = "���� ��� ������ �߻��߽��ϴ�."
                };
            }
            catch (Exception ex)
            {
                return new LoginValidationResult
                {
                    IsValid = false,
                    IsAdmin = false,
                    ErrorMessage = $"�α��� ó�� �� ������ �߻��߽��ϴ�: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// ȸ������ ����
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
                    ErrorMessage = "���� ��� ������ �߻��߽��ϴ�."
                };
            }
            catch (Exception ex)
            {
                return new ValidationResult
                {
                    IsValid = false,
                    ErrorMessage = $"���� �� ������ �߻��߽��ϴ�: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// ����� ȸ������
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
                    ErrorMessage = "���� ��� ������ �߻��߽��ϴ�."
                };
            }
            catch (Exception ex)
            {
                return new RegisterResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"ȸ������ ó�� �� ������ �߻��߽��ϴ�: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// ���� ��ū ����
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
                // API ȣ�� ���� �� ���ÿ��� ����
                return GenerateRandomTokenLocal(length);
            }
        }

        /// <summary>
        /// ��ū ���� (DB ����)
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
                    ErrorMessage = "���� ��� ������ �߻��߽��ϴ�.",
                    GeneratedToken = token
                };
            }
            catch (Exception ex)
            {
                return new TokenCreationResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"��ū ���� �� ������ �߻��߽��ϴ�: {ex.Message}",
                    GeneratedToken = token
                };
            }
        }

        /// <summary>
        /// ��� ��ū ��� ��ȸ
        /// </summary>
        public async Task<List<TokenInfo>> GetAllTokensAsync()
        {
            try
            {
                Console.WriteLine("?? [ApiUserService] ��ū ��� ��ȸ ����");
                
                using var client = CreateHttpClient();
                var response = await client.GetAsync("/api/token/list");

                if (response.IsSuccessStatusCode)
                {
                    var jsonContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"?? API ���� ����: {jsonContent}");
                    
                    // API���� AuthToken ���·� �޾Ƽ� TokenInfo�� ��ȯ
                    var apiTokens = JsonSerializer.Deserialize<List<AuthTokenApiResponse>>(jsonContent, _jsonOptions);
                    
                    if (apiTokens != null)
                    {
                        var tokenInfos = apiTokens.Select(token => new TokenInfo
                        {
                            Auth_tokens = token.Auth_tokens ?? "",
                            Effective_Date = token.Effective_Date,
                            Use_Yn = token.Use_Yn ?? "N"
                            // StatusText�� TokenInfo�� ���� �Ӽ����� �ڵ� ������
                        }).ToList();
                        
                        Console.WriteLine($"? ��ū ��� ��ȸ ����: {tokenInfos.Count}��");
                        foreach (var token in tokenInfos)
                        {
                            Console.WriteLine($"   ?? ��ū: {token.Auth_tokens}, ��ȿ��¥: {token.Effective_Date:yyyy-MM-dd}, ����: {token.StatusText}");
                        }
                        
                        return tokenInfos;
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"? ��ū ��� ��ȸ ����: {response.StatusCode}");
                    Console.WriteLine($"?? ���� ����: {errorContent}");
                }

                return new List<TokenInfo>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"? ��ū ��� ��ȸ �� ���� �߻�: {ex.Message}");
                Console.WriteLine($"?? �� ����: {ex.StackTrace}");
                return new List<TokenInfo>();
            }
        }
        
        /// <summary>
        /// API ����� AuthToken ��
        /// </summary>
        private class AuthTokenApiResponse
        {
            public string Auth_tokens { get; set; } = string.Empty;
            public DateTime Effective_Date { get; set; }
            public string Use_Yn { get; set; } = "N";
            public string StatusText { get; set; } = string.Empty; // API���� ���� ��
        }

        /// <summary>
        /// ��ū ��ȿ��¥ ����
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
                    ErrorMessage = "���� ��� ������ �߻��߽��ϴ�."
                };
            }
            catch (Exception ex)
            {
                return new TokenUpdateResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"��ū ���� �� ������ �߻��߽��ϴ�: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// ��ū ����
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
                    ErrorMessage = "���� ��� ������ �߻��߽��ϴ�."
                };
            }
            catch (Exception ex)
            {
                return new TokenUpdateResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"��ū ���� �� ������ �߻��߽��ϴ�: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// ���̺� ���� ���� Ȯ�� (API�� ���� �޼���)
        /// </summary>
        public async Task<bool> CheckTokenTableExistsAsync()
        {
            try
            {
                // API�� ���� ��ū ����� ��ȸ�غ��� �����ϸ� ���̺��� �����Ѵٰ� �Ǵ�
                var tokens = await GetAllTokensAsync();
                return true; // API ȣ���� �����ϸ� ���̺��� �����Ѵٰ� ����
            }
            catch (Exception)
            {
                return false;
            }
        }

        #region Private Methods

        /// <summary>
        /// ���� ��ū ���� (�����)
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