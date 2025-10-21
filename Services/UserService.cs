using MySql.Data.MySqlClient;
using SaveCodeClassfication.Models;
using System.Data;
using System.Linq;

namespace SaveCodeClassfication.Services
{
    /// <summary>
    /// ����� ���� �� ȸ�������� ����ϴ� ����
    /// </summary>
    public class UserService
    {
        private readonly string _connectionString;

        public UserService(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// ����� ����
        /// </summary>
        public async Task<bool> ValidateUserAsync(string userId, string password)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();
                
                // ����� ���� ���ο� ��ȿ�Ⱓ�� �Բ� Ȯ��
                string query = @"SELECT COUNT(*) 
                               FROM user_info 
                               WHERE user_id = @userId 
                               AND user_pw = @password 
                               AND (effective_date IS NULL OR effective_date >= CURDATE())";
                
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@userId", userId);
                command.Parameters.AddWithValue("@password", password);
                
                var count = Convert.ToInt32(await command.ExecuteScalarAsync());
                return count > 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"����� ���� ����: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// ���̵� �ߺ� üũ
        /// </summary>
        public async Task<bool> CheckUserIdExistsAsync(string userId)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();
                
                string query = "SELECT COUNT(*) FROM user_info WHERE user_id = @userId";
                
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@userId", userId);
                
                var count = Convert.ToInt32(await command.ExecuteScalarAsync());
                return count > 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"���̵� �ߺ� üũ ����: {ex.Message}");
                return true; // ���� �� �����ϰ� �ߺ����� ó��
            }
        }

        /// <summary>
        /// ����Ű ���� ���� üũ
        /// </summary>
        public async Task<bool> CheckAuthKeyExistsAsync(string authKey)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();
                
                string query = "SELECT COUNT(*) FROM user_auth_tokens WHERE Auth_tokens = @authKey";
                
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@authKey", authKey);
                
                var count = Convert.ToInt32(await command.ExecuteScalarAsync());
                return count > 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"����Ű ���� üũ ����: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// ����Ű ��� ���� üũ
        /// </summary>
        public async Task<bool> CheckAuthKeyInUseAsync(string authKey)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();
                
                string query = "SELECT Use_Yn FROM user_auth_tokens WHERE Auth_tokens = @authKey";
                
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@authKey", authKey);
                
                var result = await command.ExecuteScalarAsync();
                
                if (result != null)
                {
                    var usedYn = result.ToString();
                    return usedYn?.ToUpper() == "Y" || usedYn?.ToUpper() == "TRUE" || usedYn == "1";
                }
                
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"����Ű ��� ���� üũ ����: {ex.Message}");
                return true; // ���� �� �����ϰ� ��������� ó��
            }
        }

        /// <summary>
        /// ȸ������ �� ���� ����
        /// </summary>
        public async Task<ValidationResult> ValidateRegistrationAsync(string userId, string authKey)
        {
            try
            {
                // 1. ���̵� �ߺ� üũ
                bool userIdExists = await CheckUserIdExistsAsync(userId);
                if (userIdExists)
                {
                    return new ValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = "�̹� ������� ���̵��Դϴ�."
                    };
                }

                // 2. ����Ű ���� ���� üũ
                bool authKeyExists = await CheckAuthKeyExistsAsync(authKey);
                if (!authKeyExists)
                {
                    return new ValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = "��ȿ���� ���� ����Ű�Դϴ�."
                    };
                }

                // 3. ����Ű ��� ���� üũ
                bool authKeyInUse = await CheckAuthKeyInUseAsync(authKey);
                if (authKeyInUse)
                {
                    return new ValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = "�̹� ���� ����Ű�Դϴ�."
                    };
                }

                return new ValidationResult
                {
                    IsValid = true,
                    ErrorMessage = string.Empty
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ȸ������ ���� ����: {ex.Message}");
                return new ValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "���� �� ������ �߻��߽��ϴ�. �ٽ� �õ����ּ���."
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
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();
                
                using var command = new MySqlCommand("ms_userinfosave", connection);
                command.CommandType = CommandType.StoredProcedure;
                
                // ���ν����� ��ġ�ϴ� �Ű������� ���
                command.Parameters.AddWithValue("@p_user_id", userId);
                command.Parameters.AddWithValue("@p_user_pw", password);
                command.Parameters.AddWithValue("@p_user_key", authKey); // p_auth_key -> p_user_key�� ����
                
                System.Diagnostics.Debug.WriteLine($"ȸ������ ���ν��� ȣ��: userId={userId}, authKey={authKey}");
                
                // ���ν����� ��� �Ű������� ��ȯ���� �����Ƿ� ���� ����
                await command.ExecuteNonQueryAsync();
                
                System.Diagnostics.Debug.WriteLine("ȸ������ ����");
                
                return new RegisterResult
                {
                    IsSuccess = true,
                    ErrorMessage = string.Empty
                };
            }
            catch (MySqlException mysqlEx)
            {
                System.Diagnostics.Debug.WriteLine($"MySQL ȸ������ ����: {mysqlEx.Message}");
                System.Diagnostics.Debug.WriteLine($"MySQL ���� �ڵ�: {mysqlEx.Number}");
                
                string errorMessage;
                if (mysqlEx.Message.Contains("Transaction failed during Save Operation"))
                {
                    errorMessage = "ȸ������ �� �����ͺ��̽� Ʈ����� ������ �߻��߽��ϴ�.";
                }
                else if (mysqlEx.Message.Contains("Duplicate entry"))
                {
                    errorMessage = "�̹� �����ϴ� ����� ID�Դϴ�.";
                }
                else if (mysqlEx.Message.Contains("foreign key constraint"))
                {
                    errorMessage = "��ȿ���� ���� ����Ű�Դϴ�.";
                }
                else
                {
                    errorMessage = $"�����ͺ��̽� ����: {mysqlEx.Message}";
                }
                
                return new RegisterResult
                {
                    IsSuccess = false,
                    ErrorMessage = errorMessage
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ȸ������ ����: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"���� Ʈ���̽�: {ex.StackTrace}");
                return new RegisterResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"ȸ������ ó�� �� ������ �߻��߽��ϴ�: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// ����� ��ȿ�Ⱓ üũ
        /// </summary>
        public async Task<bool> CheckUserEffectiveDateAsync(string userId)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();
                
                string query = "SELECT effective_date FROM user_info WHERE user_id = @userId";
                
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@userId", userId);
                
                var result = await command.ExecuteScalarAsync();
                
                if (result == null || result == DBNull.Value)
                {
                    // effective_date�� null�̸� ��ȿ�Ⱓ ���� ����
                    return true;
                }
                
                if (DateTime.TryParse(result.ToString(), out DateTime effectiveDate))
                {
                    // ���� ��¥�� �� (���ñ����� ��ȿ)
                    return DateTime.Now.Date <= effectiveDate.Date;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"��ȿ�Ⱓ üũ ����: {ex.Message}");
                return false; // ���� �� �����ϰ� ��ȿ�� ó��
            }
        }

        /// <summary>
        /// ����� ������ ��ȿ�Ⱓ�� �и��Ͽ� üũ
        /// </summary>
        public async Task<LoginValidationResult> ValidateUserWithDetailsAsync(string userId, string password)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();
                
                // ����� ID, ��й�ȣ, Admin_Yn�� �Բ� Ȯ��
                string userQuery = @"SELECT COUNT(*) as user_exists, 
                                           COALESCE(Admin_Yn, 'N') as admin_yn 
                                    FROM user_info 
                                    WHERE user_id = @userId AND user_pw = @password";
                
                using var userCommand = new MySqlCommand(userQuery, connection);
                userCommand.Parameters.AddWithValue("@userId", userId);
                userCommand.Parameters.AddWithValue("@password", password);
                
                using var reader = await userCommand.ExecuteReaderAsync();
                
                bool userExists = false;
                bool isAdmin = false;
                
                if (await reader.ReadAsync())
                {
                    userExists = reader.GetInt32("user_exists") > 0;
                    var adminYn = reader.GetString("admin_yn");
                    isAdmin = adminYn?.ToUpper() == "Y";
                }
                
                reader.Close();
                
                if (!userExists)
                {
                    return new LoginValidationResult
                    {
                        IsValid = false,
                        IsAdmin = false,
                        ErrorMessage = "���̵� �Ǵ� ��й�ȣ�� �ùٸ��� �ʽ��ϴ�."
                    };
                }
                
                // ��ȿ�Ⱓ üũ
                bool isEffectiveDateValid = await CheckUserEffectiveDateAsync(userId);
                if (!isEffectiveDateValid)
                {
                    return new LoginValidationResult
                    {
                        IsValid = false,
                        IsAdmin = isAdmin,
                        ErrorMessage = "������ ��ȿ�Ⱓ�� ����Ǿ����ϴ�."
                    };
                }
                
                return new LoginValidationResult
                {
                    IsValid = true,
                    IsAdmin = isAdmin,
                    ErrorMessage = string.Empty
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"�α��� ���� ����: {ex.Message}");
                return new LoginValidationResult
                {
                    IsValid = false,
                    IsAdmin = false,
                    ErrorMessage = "�α��� ó�� �� ������ �߻��߽��ϴ�."
                };
            }
        }

        /// <summary>
        /// ���� ��ū ����
        /// </summary>
        public string GenerateRandomToken(int length = 16)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        /// <summary>
        /// ���� ��ū ���� (DB ����)
        /// </summary>
        public async Task<TokenCreationResult> CreateAuthTokenAsync(DateTime effectiveDate, string token)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();
                
                using var command = new MySqlCommand("ms_createauthtoken", connection);
                command.CommandType = CommandType.StoredProcedure;
                
                // DateTime �Ű������� ��������� ���� (���ν����� ��ġ�ϴ� �̸� ���)
                var effectiveDateParam = new MySqlParameter("@p_effectiveDate", MySqlDbType.DateTime)
                {
                    Value = effectiveDate.Date
                };
                command.Parameters.Add(effectiveDateParam);
                
                // ��ū �Ű����� (���ν����� ��ġ�ϴ� �̸� ���)
                var tokenParam = new MySqlParameter("@p_token", MySqlDbType.VarChar, 400)
                {
                    Value = token
                };
                command.Parameters.Add(tokenParam);
                
                System.Diagnostics.Debug.WriteLine($"��ū ���� ���ν��� ȣ��: effectiveDate={effectiveDate:yyyy-MM-dd}, token={token}");
                
                // ���ν����� ��� �Ű������� ��ȯ���� �����Ƿ� ���� ����
                await command.ExecuteNonQueryAsync();
                
                System.Diagnostics.Debug.WriteLine("��ū ���� ����");
                
                return new TokenCreationResult
                {
                    IsSuccess = true,
                    ErrorMessage = string.Empty,
                    GeneratedToken = token
                };
            }
            catch (MySqlException mysqlEx)
            {
                System.Diagnostics.Debug.WriteLine($"MySQL ��ū ���� ����: {mysqlEx.Message}");
                System.Diagnostics.Debug.WriteLine($"MySQL ���� �ڵ�: {mysqlEx.Number}");
                
                string errorMessage;
                if (mysqlEx.Message.Contains("Transaction failed during Save Operation"))
                {
                    errorMessage = "��ū ���� �� �����ͺ��̽� Ʈ����� ������ �߻��߽��ϴ�.";
                }
                else if (mysqlEx.Message.Contains("Duplicate entry"))
                {
                    errorMessage = "�̹� �����ϴ� ��ū�Դϴ�. �ٸ� ��ū�� �������ּ���.";
                }
                else
                {
                    errorMessage = $"�����ͺ��̽� ����: {mysqlEx.Message}";
                }
                
                return new TokenCreationResult
                {
                    IsSuccess = false,
                    ErrorMessage = errorMessage,
                    GeneratedToken = token
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"��ū ���� ����: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"���� Ʈ���̽�: {ex.StackTrace}");
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
                System.Diagnostics.Debug.WriteLine("=== ��ū ��� ��ȸ ���� ===");
                
                using var connection = new MySqlConnection(_connectionString);
                System.Diagnostics.Debug.WriteLine("�����ͺ��̽� ���� ����");
                
                await connection.OpenAsync();
                System.Diagnostics.Debug.WriteLine("�����ͺ��̽� ���� ����");
                
                string query = @"SELECT Auth_tokens, Effective_Date, Use_Yn 
                               FROM user_auth_tokens 
                               ORDER BY Auth_tokens";
                
                System.Diagnostics.Debug.WriteLine($"������ ����: {query}");
                
                using var command = new MySqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();
                
                var tokens = new List<TokenInfo>();
                int rowCount = 0;
                
                while (await reader.ReadAsync())
                {
                    rowCount++;
                    try
                    {
                        var authTokens = reader.GetString("Auth_tokens");
                        var effectiveDate = reader.GetDateTime("Effective_Date");
                        var useYn = reader.GetString("Use_Yn") ?? "N";
                        
                        System.Diagnostics.Debug.WriteLine($"Row {rowCount}: Token={authTokens}, EffectiveDate={effectiveDate}, UseYn={useYn}");
                        
                        var tokenInfo = new TokenInfo
                        {
                            Auth_tokens = authTokens,
                            Effective_Date = effectiveDate,
                            Use_Yn = useYn,
                            Create_DTM = DateTime.Now // Create_DTM �÷��� �����Ƿ� ���� ��¥�� ����
                        };
                        
                        tokens.Add(tokenInfo);
                        System.Diagnostics.Debug.WriteLine($"��ū ���� �߰���: {tokenInfo.StatusText}");
                    }
                    catch (Exception rowEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"Row {rowCount} ó�� ����: {rowEx.Message}");
                    }
                }
                
                System.Diagnostics.Debug.WriteLine($"�� {tokens.Count}���� ��ū�� ��ȸ�߽��ϴ�.");
                
                return tokens;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"��ū ��� ��ȸ ����: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"���� Ʈ���̽�: {ex.StackTrace}");
                
                // MySqlException�� ��� �� �ڼ��� ����
                if (ex is MySqlException mysqlEx)
                {
                    System.Diagnostics.Debug.WriteLine($"MySQL ���� �ڵ�: {mysqlEx.Number}");
                    System.Diagnostics.Debug.WriteLine($"MySQL SQL ����: {mysqlEx.SqlState}");
                }
                
                return new List<TokenInfo>();
            }
        }

        /// <summary>
        /// ��ū�� ��ȿ ��¥ ����
        /// </summary>
        public async Task<TokenUpdateResult> UpdateTokenEffectiveDateAsync(string authToken, DateTime newEffectiveDate)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();
                
                string query = @"UPDATE user_auth_tokens 
                               SET Effective_Date = @newEffectiveDate 
                               WHERE Auth_tokens = @authToken";
                
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@authToken", authToken);
                command.Parameters.AddWithValue("@newEffectiveDate", newEffectiveDate.Date);
                
                int rowsAffected = await command.ExecuteNonQueryAsync();
                
                if (rowsAffected > 0)
                {
                    return new TokenUpdateResult
                    {
                        IsSuccess = true,
                        ErrorMessage = string.Empty
                    };
                }
                else
                {
                    return new TokenUpdateResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "�ش� ��ū�� ã�� �� �����ϴ�."
                    };
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"��ū ���� ����: {ex.Message}");
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
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();
                
                string query = "DELETE FROM user_auth_tokens WHERE Auth_tokens = @authToken";
                
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@authToken", authToken);
                
                int rowsAffected = await command.ExecuteNonQueryAsync();
                
                if (rowsAffected > 0)
                {
                    return new TokenUpdateResult
                    {
                        IsSuccess = true,
                        ErrorMessage = string.Empty
                    };
                }
                else
                {
                    return new TokenUpdateResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "�ش� ��ū�� ã�� �� �����ϴ�."
                    };
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"��ū ���� ����: {ex.Message}");
                return new TokenUpdateResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"��ū ���� �� ������ �߻��߽��ϴ�: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// �����ͺ��̽� ���̺� ���� ���� Ȯ��
        /// </summary>
        public async Task<bool> CheckTokenTableExistsAsync()
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();
                
                // ���̺� ���� ���� Ȯ��
                string tableExistsQuery = @"SELECT COUNT(*) 
                                           FROM information_schema.tables 
                                           WHERE table_schema = DATABASE() 
                                           AND table_name = 'user_auth_tokens'";
                
                using var command = new MySqlCommand(tableExistsQuery, connection);
                var tableExists = Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
                
                System.Diagnostics.Debug.WriteLine($"user_auth_tokens ���̺� ���� ����: {tableExists}");
                
                if (tableExists)
                {
                    // ���̺� ���� Ȯ��
                    string columnsQuery = @"SELECT COLUMN_NAME, DATA_TYPE 
                                          FROM information_schema.columns 
                                          WHERE table_schema = DATABASE() 
                                          AND table_name = 'user_auth_tokens'
                                          ORDER BY ORDINAL_POSITION";
                    
                    using var columnsCommand = new MySqlCommand(columnsQuery, connection);
                    using var reader = await columnsCommand.ExecuteReaderAsync();
                    
                    System.Diagnostics.Debug.WriteLine("=== user_auth_tokens ���̺� ���� ===");
                    while (await reader.ReadAsync())
                    {
                        var columnName = reader.GetString("COLUMN_NAME");
                        var dataType = reader.GetString("DATA_TYPE");
                        System.Diagnostics.Debug.WriteLine($"�÷�: {columnName} - Ÿ��: {dataType}");
                    }
                    
                    reader.Close();
                    
                    // ���̺� ���ڵ� �� Ȯ��
                    string countQuery = "SELECT COUNT(*) FROM user_auth_tokens";
                    using var countCommand = new MySqlCommand(countQuery, connection);
                    var recordCount = Convert.ToInt32(await countCommand.ExecuteScalarAsync());
                    
                    System.Diagnostics.Debug.WriteLine($"user_auth_tokens ���̺� ���ڵ� ��: {recordCount}");
                    
                    // ���� ������ Ȯ�� (Create_DTM �÷� ����)
                    if (recordCount > 0)
                    {
                        string sampleQuery = @"SELECT Auth_tokens, Effective_Date, Use_Yn 
                                             FROM user_auth_tokens 
                                             LIMIT 3";
                        
                        using var sampleCommand = new MySqlCommand(sampleQuery, connection);
                        using var sampleReader = await sampleCommand.ExecuteReaderAsync();
                        
                        System.Diagnostics.Debug.WriteLine("=== ���� ������ (�ִ� 3��) ===");
                        int sampleCount = 0;
                        while (await sampleReader.ReadAsync())
                        {
                            sampleCount++;
                            var token = sampleReader.GetString("Auth_tokens");
                            var effectiveDate = sampleReader.GetDateTime("Effective_Date");
                            var useYn = sampleReader.GetString("Use_Yn");
                            
                            System.Diagnostics.Debug.WriteLine($"Sample {sampleCount}: {token} | {effectiveDate} | {useYn}");
                        }
                    }
                }
                
                return tableExists;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"���̺� Ȯ�� ����: {ex.Message}");
                return false;
            }
        }
    }
}