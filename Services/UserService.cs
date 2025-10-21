using MySql.Data.MySqlClient;
using SaveCodeClassfication.Models;
using System.Data;
using System.Linq;

namespace SaveCodeClassfication.Services
{
    /// <summary>
    /// 사용자 인증 및 회원가입을 담당하는 서비스
    /// </summary>
    public class UserService
    {
        private readonly string _connectionString;

        public UserService(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// 사용자 인증
        /// </summary>
        public async Task<bool> ValidateUserAsync(string userId, string password)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();
                
                // 사용자 존재 여부와 유효기간을 함께 확인
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
                System.Diagnostics.Debug.WriteLine($"사용자 인증 오류: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 아이디 중복 체크
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
                System.Diagnostics.Debug.WriteLine($"아이디 중복 체크 오류: {ex.Message}");
                return true; // 오류 시 안전하게 중복으로 처리
            }
        }

        /// <summary>
        /// 인증키 존재 여부 체크
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
                System.Diagnostics.Debug.WriteLine($"인증키 존재 체크 오류: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 인증키 사용 여부 체크
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
                System.Diagnostics.Debug.WriteLine($"인증키 사용 여부 체크 오류: {ex.Message}");
                return true; // 오류 시 안전하게 사용중으로 처리
            }
        }

        /// <summary>
        /// 회원가입 전 종합 검증
        /// </summary>
        public async Task<ValidationResult> ValidateRegistrationAsync(string userId, string authKey)
        {
            try
            {
                // 1. 아이디 중복 체크
                bool userIdExists = await CheckUserIdExistsAsync(userId);
                if (userIdExists)
                {
                    return new ValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = "이미 사용중인 아이디입니다."
                    };
                }

                // 2. 인증키 존재 여부 체크
                bool authKeyExists = await CheckAuthKeyExistsAsync(authKey);
                if (!authKeyExists)
                {
                    return new ValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = "유효하지 않은 인증키입니다."
                    };
                }

                // 3. 인증키 사용 여부 체크
                bool authKeyInUse = await CheckAuthKeyInUseAsync(authKey);
                if (authKeyInUse)
                {
                    return new ValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = "이미 사용된 인증키입니다."
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
                System.Diagnostics.Debug.WriteLine($"회원가입 검증 오류: {ex.Message}");
                return new ValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "검증 중 오류가 발생했습니다. 다시 시도해주세요."
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
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();
                
                using var command = new MySqlCommand("ms_userinfosave", connection);
                command.CommandType = CommandType.StoredProcedure;
                
                // 프로시저와 일치하는 매개변수명 사용
                command.Parameters.AddWithValue("@p_user_id", userId);
                command.Parameters.AddWithValue("@p_user_pw", password);
                command.Parameters.AddWithValue("@p_user_key", authKey); // p_auth_key -> p_user_key로 변경
                
                System.Diagnostics.Debug.WriteLine($"회원가입 프로시저 호출: userId={userId}, authKey={authKey}");
                
                // 프로시저가 출력 매개변수를 반환하지 않으므로 직접 실행
                await command.ExecuteNonQueryAsync();
                
                System.Diagnostics.Debug.WriteLine("회원가입 성공");
                
                return new RegisterResult
                {
                    IsSuccess = true,
                    ErrorMessage = string.Empty
                };
            }
            catch (MySqlException mysqlEx)
            {
                System.Diagnostics.Debug.WriteLine($"MySQL 회원가입 오류: {mysqlEx.Message}");
                System.Diagnostics.Debug.WriteLine($"MySQL 오류 코드: {mysqlEx.Number}");
                
                string errorMessage;
                if (mysqlEx.Message.Contains("Transaction failed during Save Operation"))
                {
                    errorMessage = "회원가입 중 데이터베이스 트랜잭션 오류가 발생했습니다.";
                }
                else if (mysqlEx.Message.Contains("Duplicate entry"))
                {
                    errorMessage = "이미 존재하는 사용자 ID입니다.";
                }
                else if (mysqlEx.Message.Contains("foreign key constraint"))
                {
                    errorMessage = "유효하지 않은 인증키입니다.";
                }
                else
                {
                    errorMessage = $"데이터베이스 오류: {mysqlEx.Message}";
                }
                
                return new RegisterResult
                {
                    IsSuccess = false,
                    ErrorMessage = errorMessage
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"회원가입 오류: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"스택 트레이스: {ex.StackTrace}");
                return new RegisterResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"회원가입 처리 중 오류가 발생했습니다: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 사용자 유효기간 체크
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
                    // effective_date가 null이면 유효기간 제한 없음
                    return true;
                }
                
                if (DateTime.TryParse(result.ToString(), out DateTime effectiveDate))
                {
                    // 현재 날짜와 비교 (오늘까지는 유효)
                    return DateTime.Now.Date <= effectiveDate.Date;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"유효기간 체크 오류: {ex.Message}");
                return false; // 오류 시 안전하게 무효로 처리
            }
        }

        /// <summary>
        /// 사용자 인증과 유효기간을 분리하여 체크
        /// </summary>
        public async Task<LoginValidationResult> ValidateUserWithDetailsAsync(string userId, string password)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();
                
                // 사용자 ID, 비밀번호, Admin_Yn을 함께 확인
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
                        ErrorMessage = "아이디 또는 비밀번호가 올바르지 않습니다."
                    };
                }
                
                // 유효기간 체크
                bool isEffectiveDateValid = await CheckUserEffectiveDateAsync(userId);
                if (!isEffectiveDateValid)
                {
                    return new LoginValidationResult
                    {
                        IsValid = false,
                        IsAdmin = isAdmin,
                        ErrorMessage = "계정의 유효기간이 만료되었습니다."
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
                System.Diagnostics.Debug.WriteLine($"로그인 검증 오류: {ex.Message}");
                return new LoginValidationResult
                {
                    IsValid = false,
                    IsAdmin = false,
                    ErrorMessage = "로그인 처리 중 오류가 발생했습니다."
                };
            }
        }

        /// <summary>
        /// 랜덤 토큰 생성
        /// </summary>
        public string GenerateRandomToken(int length = 16)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        /// <summary>
        /// 인증 토큰 생성 (DB 저장)
        /// </summary>
        public async Task<TokenCreationResult> CreateAuthTokenAsync(DateTime effectiveDate, string token)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();
                
                using var command = new MySqlCommand("ms_createauthtoken", connection);
                command.CommandType = CommandType.StoredProcedure;
                
                // DateTime 매개변수를 명시적으로 설정 (프로시저와 일치하는 이름 사용)
                var effectiveDateParam = new MySqlParameter("@p_effectiveDate", MySqlDbType.DateTime)
                {
                    Value = effectiveDate.Date
                };
                command.Parameters.Add(effectiveDateParam);
                
                // 토큰 매개변수 (프로시저와 일치하는 이름 사용)
                var tokenParam = new MySqlParameter("@p_token", MySqlDbType.VarChar, 400)
                {
                    Value = token
                };
                command.Parameters.Add(tokenParam);
                
                System.Diagnostics.Debug.WriteLine($"토큰 생성 프로시저 호출: effectiveDate={effectiveDate:yyyy-MM-dd}, token={token}");
                
                // 프로시저가 출력 매개변수를 반환하지 않으므로 직접 실행
                await command.ExecuteNonQueryAsync();
                
                System.Diagnostics.Debug.WriteLine("토큰 생성 성공");
                
                return new TokenCreationResult
                {
                    IsSuccess = true,
                    ErrorMessage = string.Empty,
                    GeneratedToken = token
                };
            }
            catch (MySqlException mysqlEx)
            {
                System.Diagnostics.Debug.WriteLine($"MySQL 토큰 생성 오류: {mysqlEx.Message}");
                System.Diagnostics.Debug.WriteLine($"MySQL 오류 코드: {mysqlEx.Number}");
                
                string errorMessage;
                if (mysqlEx.Message.Contains("Transaction failed during Save Operation"))
                {
                    errorMessage = "토큰 저장 중 데이터베이스 트랜잭션 오류가 발생했습니다.";
                }
                else if (mysqlEx.Message.Contains("Duplicate entry"))
                {
                    errorMessage = "이미 존재하는 토큰입니다. 다른 토큰을 생성해주세요.";
                }
                else
                {
                    errorMessage = $"데이터베이스 오류: {mysqlEx.Message}";
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
                System.Diagnostics.Debug.WriteLine($"토큰 생성 오류: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"스택 트레이스: {ex.StackTrace}");
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
                System.Diagnostics.Debug.WriteLine("=== 토큰 목록 조회 시작 ===");
                
                using var connection = new MySqlConnection(_connectionString);
                System.Diagnostics.Debug.WriteLine("데이터베이스 연결 생성");
                
                await connection.OpenAsync();
                System.Diagnostics.Debug.WriteLine("데이터베이스 연결 성공");
                
                string query = @"SELECT Auth_tokens, Effective_Date, Use_Yn 
                               FROM user_auth_tokens 
                               ORDER BY Auth_tokens";
                
                System.Diagnostics.Debug.WriteLine($"실행할 쿼리: {query}");
                
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
                            Create_DTM = DateTime.Now // Create_DTM 컬럼이 없으므로 현재 날짜로 설정
                        };
                        
                        tokens.Add(tokenInfo);
                        System.Diagnostics.Debug.WriteLine($"토큰 정보 추가됨: {tokenInfo.StatusText}");
                    }
                    catch (Exception rowEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"Row {rowCount} 처리 오류: {rowEx.Message}");
                    }
                }
                
                System.Diagnostics.Debug.WriteLine($"총 {tokens.Count}개의 토큰을 조회했습니다.");
                
                return tokens;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"토큰 목록 조회 오류: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"스택 트레이스: {ex.StackTrace}");
                
                // MySqlException인 경우 더 자세한 정보
                if (ex is MySqlException mysqlEx)
                {
                    System.Diagnostics.Debug.WriteLine($"MySQL 오류 코드: {mysqlEx.Number}");
                    System.Diagnostics.Debug.WriteLine($"MySQL SQL 상태: {mysqlEx.SqlState}");
                }
                
                return new List<TokenInfo>();
            }
        }

        /// <summary>
        /// 토큰의 유효 날짜 수정
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
                        ErrorMessage = "해당 토큰을 찾을 수 없습니다."
                    };
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"토큰 수정 오류: {ex.Message}");
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
                        ErrorMessage = "해당 토큰을 찾을 수 없습니다."
                    };
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"토큰 삭제 오류: {ex.Message}");
                return new TokenUpdateResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"토큰 삭제 중 오류가 발생했습니다: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 데이터베이스 테이블 존재 여부 확인
        /// </summary>
        public async Task<bool> CheckTokenTableExistsAsync()
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();
                
                // 테이블 존재 여부 확인
                string tableExistsQuery = @"SELECT COUNT(*) 
                                           FROM information_schema.tables 
                                           WHERE table_schema = DATABASE() 
                                           AND table_name = 'user_auth_tokens'";
                
                using var command = new MySqlCommand(tableExistsQuery, connection);
                var tableExists = Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
                
                System.Diagnostics.Debug.WriteLine($"user_auth_tokens 테이블 존재 여부: {tableExists}");
                
                if (tableExists)
                {
                    // 테이블 구조 확인
                    string columnsQuery = @"SELECT COLUMN_NAME, DATA_TYPE 
                                          FROM information_schema.columns 
                                          WHERE table_schema = DATABASE() 
                                          AND table_name = 'user_auth_tokens'
                                          ORDER BY ORDINAL_POSITION";
                    
                    using var columnsCommand = new MySqlCommand(columnsQuery, connection);
                    using var reader = await columnsCommand.ExecuteReaderAsync();
                    
                    System.Diagnostics.Debug.WriteLine("=== user_auth_tokens 테이블 구조 ===");
                    while (await reader.ReadAsync())
                    {
                        var columnName = reader.GetString("COLUMN_NAME");
                        var dataType = reader.GetString("DATA_TYPE");
                        System.Diagnostics.Debug.WriteLine($"컬럼: {columnName} - 타입: {dataType}");
                    }
                    
                    reader.Close();
                    
                    // 테이블 레코드 수 확인
                    string countQuery = "SELECT COUNT(*) FROM user_auth_tokens";
                    using var countCommand = new MySqlCommand(countQuery, connection);
                    var recordCount = Convert.ToInt32(await countCommand.ExecuteScalarAsync());
                    
                    System.Diagnostics.Debug.WriteLine($"user_auth_tokens 테이블 레코드 수: {recordCount}");
                    
                    // 샘플 데이터 확인 (Create_DTM 컬럼 제거)
                    if (recordCount > 0)
                    {
                        string sampleQuery = @"SELECT Auth_tokens, Effective_Date, Use_Yn 
                                             FROM user_auth_tokens 
                                             LIMIT 3";
                        
                        using var sampleCommand = new MySqlCommand(sampleQuery, connection);
                        using var sampleReader = await sampleCommand.ExecuteReaderAsync();
                        
                        System.Diagnostics.Debug.WriteLine("=== 샘플 데이터 (최대 3개) ===");
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
                System.Diagnostics.Debug.WriteLine($"테이블 확인 오류: {ex.Message}");
                return false;
            }
        }
    }
}