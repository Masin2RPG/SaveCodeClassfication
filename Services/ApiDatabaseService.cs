using System.Collections.Generic;
using System.Threading.Tasks;
using SaveCodeClassfication.Models;

namespace SaveCodeClassfication.Services
{
    /// <summary>
    /// API 기반 데이터베이스 서비스
    /// </summary>
    public class ApiDatabaseService : ApiService
    {
        /// <summary>
        /// 사용자 로그인 검증 (상세 정보 포함)
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
                        ErrorMessage = response.ErrorMessage,
                        UserId = response.UserId
                    };
                }

                return new LoginValidationResult
                {
                    IsValid = false,
                    IsAdmin = false,
                    ErrorMessage = "API 서버 응답 오류"
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"API 로그인 검증 오류: {ex.Message}");
                return new LoginValidationResult
                {
                    IsValid = false,
                    IsAdmin = false,
                    ErrorMessage = $"API 연결 오류: {ex.Message}"
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
                    ErrorMessage = "API 서버 응답 오류"
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"API 회원가입 검증 오류: {ex.Message}");
                return new ValidationResult
                {
                    IsValid = false,
                    ErrorMessage = $"API 연결 오류: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 사용자 등록
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
                    ErrorMessage = "API 서버 응답 오류"
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"API 사용자 등록 오류: {ex.Message}");
                return new RegisterResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"API 연결 오류: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 분석 결과를 API를 통해 데이터베이스에 저장
        /// </summary>
        public async Task<bool> SaveCacheAsync(AnalysisCache cache, string userId)
        {
            try
            {
                var request = new ApiSaveCodeRequest
                {
                    FolderPath = cache.FolderPath,
                    SaveCodes = cache.SaveCodes.Select(sc => new ApiSaveCodeInfo
                    {
                        CharacterName = sc.CharacterName,
                        SaveCode = sc.SaveCode,
                        FileName = sc.FileName,
                        FilePath = sc.FilePath,
                        FileDate = sc.FileDate,
                        FullContent = sc.FullContent,
                        Level = sc.Level,
                        Gold = sc.Gold,
                        Wood = sc.Wood,
                        Experience = sc.Experience,
                        PhysicalPower = sc.PhysicalPower,
                        MagicalPower = sc.MagicalPower,
                        SpiritualPower = sc.SpiritualPower,
                        Items = sc.Items,
                        ItemsDisplayText = sc.ItemsDisplayText
                    }).ToList(),
                    UserKey = userId // 실제 사용자 ID 사용
                };

                var response = await PostAsync<ApiSaveCodeRequest, ApiSaveCodeResponse>("savecode/save", request);

                if (response != null)
                {
                    System.Diagnostics.Debug.WriteLine($"API 저장 결과: 성공 {response.SuccessCount}, 실패 {response.ErrorCount}");
                    return response.IsSuccess;
                }

                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"API 저장 오류: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 사용자별 세이브 코드 조회 - 부모 클래스 메서드 사용
        /// </summary>
        public async Task<List<SaveCodeInfo>> LoadUserSaveCodesAsync(string userId)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"=== API 세이브 코드 조회 시작 (사용자: {userId}) ===");
                
                // 부모 클래스의 GetAsync 메서드 사용
                var saveCodes = await GetAsync<List<SaveCodeInfo>>($"savecode/user/{userId}");
                
                if (saveCodes == null)
                {
                    System.Diagnostics.Debug.WriteLine("API 응답이 null입니다.");
                    return new List<SaveCodeInfo>();
                }
                
                System.Diagnostics.Debug.WriteLine($"=== API 세이브 코드 조회 완료: {saveCodes.Count}개 반환 ===");
                
                // 각 세이브 코드 정보 로그
                foreach (var saveCode in saveCodes.Take(5)) // 처음 5개만 로그
                {
                    System.Diagnostics.Debug.WriteLine($"  ?? {saveCode.CharacterName} - {saveCode.FileName}");
                }
                if (saveCodes.Count > 5)
                {
                    System.Diagnostics.Debug.WriteLine($"  ... 및 {saveCodes.Count - 5}개 더");
                }
                
                return saveCodes;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"API 세이브 코드 조회 오류: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"스택 트레이스: {ex.StackTrace}");
                return new List<SaveCodeInfo>();
            }
        }

        /// <summary>
        /// 캐릭터별 세이브 코드 조회
        /// </summary>
        public async Task<List<SaveCodeInfo>> LoadSaveCodesByCharacterAsync(string characterName, string userId)
        {
            try
            {
                var saveCodes = await GetAsync<List<SaveCodeInfo>>($"savecode/character/{Uri.EscapeDataString(characterName)}/user/{userId}");
                return saveCodes ?? new List<SaveCodeInfo>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"API 캐릭터별 세이브 코드 조회 오류: {ex.Message}");
                return new List<SaveCodeInfo>();
            }
        }

        /// <summary>
        /// 데이터베이스 정보 조회
        /// </summary>
        public async Task<string> GetDatabaseInfoAsync()
        {
            try
            {
                var info = await GetAsync<ApiDatabaseInfoResponse>("savecode/database-info");

                if (info != null)
                {
                    var result = $"데이터베이스 (API 기반)\n";
                    result += $"?? 전체 세이브코드: {info.TotalSaves:N0}개\n";
                    result += $"?? 등록된 사용자: {info.TotalUsers}명\n";

                    if (info.LastSaveDate.HasValue)
                    {
                        result += $"?? 최근 저장: {info.LastSaveDate.Value:yyyy-MM-dd HH:mm}";
                    }
                    else
                    {
                        result += "?? 저장된 데이터가 없음";
                    }

                    return result;
                }

                return "데이터베이스 (API 기반)\n? 오류 - 정보를 가져올 수 없음";
            }
            catch (Exception ex)
            {
                return $"데이터베이스 API 연결 오류:\n{ex.Message}";
            }
        }

        /// <summary>
        /// 모든 데이터 삭제
        /// </summary>
        public async Task<bool> ClearAllDataAsync(string userId)
        {
            try
            {
                var result = await DeleteAsync<bool>($"savecode/clear/{userId}");
                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"API 데이터 삭제 오류: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// API 서버 연결 테스트
        /// </summary>
        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                var info = await GetAsync<ApiDatabaseInfoResponse>("savecode/database-info");
                return info != null;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 데이터베이스 초기화 테스트
        /// </summary>
        public async Task<bool> InitializeDatabaseAsync()
        {
            try
            {
                var info = await GetAsync<ApiDatabaseInfoResponse>("savecode/database-info");
                return info != null;
            }
            catch
            {
                return false;
            }
        }
    }
}