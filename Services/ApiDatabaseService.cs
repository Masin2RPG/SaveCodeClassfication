using System.Collections.Generic;
using System.Threading.Tasks;
using SaveCodeClassfication.Models;

namespace SaveCodeClassfication.Services
{
    /// <summary>
    /// API ��� �����ͺ��̽� ����
    /// </summary>
    public class ApiDatabaseService : ApiService
    {
        /// <summary>
        /// ����� �α��� ���� (�� ���� ����)
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
                    ErrorMessage = "API ���� ���� ����"
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"API �α��� ���� ����: {ex.Message}");
                return new LoginValidationResult
                {
                    IsValid = false,
                    IsAdmin = false,
                    ErrorMessage = $"API ���� ����: {ex.Message}"
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
                    ErrorMessage = "API ���� ���� ����"
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"API ȸ������ ���� ����: {ex.Message}");
                return new ValidationResult
                {
                    IsValid = false,
                    ErrorMessage = $"API ���� ����: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// ����� ���
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
                    ErrorMessage = "API ���� ���� ����"
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"API ����� ��� ����: {ex.Message}");
                return new RegisterResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"API ���� ����: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// �м� ����� API�� ���� �����ͺ��̽��� ����
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
                    UserKey = userId // ���� ����� ID ���
                };

                var response = await PostAsync<ApiSaveCodeRequest, ApiSaveCodeResponse>("savecode/save", request);

                if (response != null)
                {
                    System.Diagnostics.Debug.WriteLine($"API ���� ���: ���� {response.SuccessCount}, ���� {response.ErrorCount}");
                    return response.IsSuccess;
                }

                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"API ���� ����: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// ����ں� ���̺� �ڵ� ��ȸ - �θ� Ŭ���� �޼��� ���
        /// </summary>
        public async Task<List<SaveCodeInfo>> LoadUserSaveCodesAsync(string userId)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"=== API ���̺� �ڵ� ��ȸ ���� (�����: {userId}) ===");
                
                // �θ� Ŭ������ GetAsync �޼��� ���
                var saveCodes = await GetAsync<List<SaveCodeInfo>>($"savecode/user/{userId}");
                
                if (saveCodes == null)
                {
                    System.Diagnostics.Debug.WriteLine("API ������ null�Դϴ�.");
                    return new List<SaveCodeInfo>();
                }
                
                System.Diagnostics.Debug.WriteLine($"=== API ���̺� �ڵ� ��ȸ �Ϸ�: {saveCodes.Count}�� ��ȯ ===");
                
                // �� ���̺� �ڵ� ���� �α�
                foreach (var saveCode in saveCodes.Take(5)) // ó�� 5���� �α�
                {
                    System.Diagnostics.Debug.WriteLine($"  ?? {saveCode.CharacterName} - {saveCode.FileName}");
                }
                if (saveCodes.Count > 5)
                {
                    System.Diagnostics.Debug.WriteLine($"  ... �� {saveCodes.Count - 5}�� ��");
                }
                
                return saveCodes;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"API ���̺� �ڵ� ��ȸ ����: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"���� Ʈ���̽�: {ex.StackTrace}");
                return new List<SaveCodeInfo>();
            }
        }

        /// <summary>
        /// ĳ���ͺ� ���̺� �ڵ� ��ȸ
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
                System.Diagnostics.Debug.WriteLine($"API ĳ���ͺ� ���̺� �ڵ� ��ȸ ����: {ex.Message}");
                return new List<SaveCodeInfo>();
            }
        }

        /// <summary>
        /// �����ͺ��̽� ���� ��ȸ
        /// </summary>
        public async Task<string> GetDatabaseInfoAsync()
        {
            try
            {
                var info = await GetAsync<ApiDatabaseInfoResponse>("savecode/database-info");

                if (info != null)
                {
                    var result = $"�����ͺ��̽� (API ���)\n";
                    result += $"?? ��ü ���̺��ڵ�: {info.TotalSaves:N0}��\n";
                    result += $"?? ��ϵ� �����: {info.TotalUsers}��\n";

                    if (info.LastSaveDate.HasValue)
                    {
                        result += $"?? �ֱ� ����: {info.LastSaveDate.Value:yyyy-MM-dd HH:mm}";
                    }
                    else
                    {
                        result += "?? ����� �����Ͱ� ����";
                    }

                    return result;
                }

                return "�����ͺ��̽� (API ���)\n? ���� - ������ ������ �� ����";
            }
            catch (Exception ex)
            {
                return $"�����ͺ��̽� API ���� ����:\n{ex.Message}";
            }
        }

        /// <summary>
        /// ��� ������ ����
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
                System.Diagnostics.Debug.WriteLine($"API ������ ���� ����: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// API ���� ���� �׽�Ʈ
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
        /// �����ͺ��̽� �ʱ�ȭ �׽�Ʈ
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