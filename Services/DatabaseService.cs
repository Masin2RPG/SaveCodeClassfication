using MySql.Data.MySqlClient;
using SaveCodeClassfication.Models;
using System.Data;
using System.Text.Json;
using System.IO;

namespace SaveCodeClassfication.Services
{
    /// <summary>
    /// 데이터베이스 연결 및 작업을 관리하는 서비스
    /// </summary>
    public class DatabaseService
    {
        private readonly DatabaseSettings _dbSettings;
        private readonly string _connectionString;
        private readonly UserService _userService;

        public DatabaseService(DatabaseSettings dbSettings)
        {
            _dbSettings = dbSettings;
            _connectionString = dbSettings.GetConnectionString();
            _userService = new UserService(_connectionString);
        }

        /// <summary>
        /// 연결 문자열을 가져옵니다
        /// </summary>
        public string GetConnectionString()
        {
            return _connectionString;
        }

        // UserService 델리게이트 메서드들
        public async Task<bool> ValidateUserAsync(string userId, string password) 
            => await _userService.ValidateUserAsync(userId, password);

        public async Task<ValidationResult> ValidateRegistrationAsync(string userId, string authKey) 
            => await _userService.ValidateRegistrationAsync(userId, authKey);

        public async Task<RegisterResult> RegisterUserAsync(string userId, string password, string authKey) 
            => await _userService.RegisterUserAsync(userId, password, authKey);

        /// <summary>
        /// 데이터베이스 연결을 테스트합니다
        /// </summary>
        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"=== 데이터베이스 연결 테스트 ===");
                System.Diagnostics.Debug.WriteLine($"호스트: {_dbSettings.Host}:{_dbSettings.Port}");
                System.Diagnostics.Debug.WriteLine($"데이터베이스: {_dbSettings.Database}");
                System.Diagnostics.Debug.WriteLine($"사용자: {_dbSettings.UserId}");
                System.Diagnostics.Debug.WriteLine($"SSL: {_dbSettings.UseSSL}");
                System.Diagnostics.Debug.WriteLine($"연결 문자열: {_connectionString}");

                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();
                
                var isOpen = connection.State == ConnectionState.Open;
                System.Diagnostics.Debug.WriteLine($"연결 상태: {connection.State}");
                System.Diagnostics.Debug.WriteLine($"서버 버전: {connection.ServerVersion}");
                
                return isOpen;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"=== 연결 테스트 실패 ===");
                System.Diagnostics.Debug.WriteLine($"오류 메시지: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"오류 타입: {ex.GetType().Name}");
                
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"내부 예외: {ex.InnerException.Message}");
                }
                
                return false;
            }
        }

        /// <summary>
        /// 데이터베이스 초기화
        /// </summary>
        public async Task<bool> InitializeDatabaseAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"=== 데이터베이스 초기화 ===");
                
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();
                
                // 데이터베이스 선택
                System.Diagnostics.Debug.WriteLine($"데이터베이스 '{_dbSettings.Database}' 선택 중...");
                await connection.ChangeDatabaseAsync(_dbSettings.Database);
                
                // 프로시저 존재 확인
                var checkProcedureCmd = new MySqlCommand(@"
                    SELECT COUNT(*) 
                    FROM information_schema.ROUTINES 
                    WHERE ROUTINE_SCHEMA = @database 
                    AND ROUTINE_NAME = 'ms_usersavecode'", connection);
                checkProcedureCmd.Parameters.AddWithValue("@database", _dbSettings.Database);
                
                var procedureExists = Convert.ToInt32(await checkProcedureCmd.ExecuteScalarAsync()) > 0;
                System.Diagnostics.Debug.WriteLine($"프로시저 'ms_usersavecode' 존재 여부: {procedureExists}");
                
                // 테이블 존재 확인
                var checkTableCmd = new MySqlCommand(@"
                    SELECT COUNT(*) 
                    FROM information_schema.TABLES 
                    WHERE TABLE_SCHEMA = @database 
                    AND TABLE_NAME = 'player_save'", connection);
                checkTableCmd.Parameters.AddWithValue("@database", _dbSettings.Database);
                
                var tableExists = Convert.ToInt32(await checkTableCmd.ExecuteScalarAsync()) > 0;
                System.Diagnostics.Debug.WriteLine($"테이블 'player_save' 존재 여부: {tableExists}");
                
                if (!procedureExists || !tableExists)
                {
                    System.Diagnostics.Debug.WriteLine("필수 프로시저 또는 테이블이 존재하지 않습니다!");
                    return false;
                }
                
                System.Diagnostics.Debug.WriteLine("데이터베이스 초기화 완료");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"=== 데이터베이스 초기화 실패 ===");
                System.Diagnostics.Debug.WriteLine($"오류: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 세이브 코드 데이터를 저장합니다 (프로시저 사용)
        /// </summary>
        public async Task<bool> SaveAnalysisAsync(string folderPath, List<SaveCodeInfo> saveCodes, Dictionary<string, DateTime> fileHashes, string userKey = "default_user")
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"=== 데이터베이스 저장 시작 ===");
                System.Diagnostics.Debug.WriteLine($"저장할 세이브 코드 수: {saveCodes.Count}");
                System.Diagnostics.Debug.WriteLine($"사용자 키: {userKey}");

                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();
                System.Diagnostics.Debug.WriteLine("데이터베이스 연결 성공");

                // 기존 데이터 삭제
                System.Diagnostics.Debug.WriteLine("기존 데이터 삭제 중...");
                using var deleteInventoryCmd = new MySqlCommand(@"
                    DELETE pi FROM player_inventory pi
                    INNER JOIN player_save ps ON pi.SAVE_ID = ps.SAVE_ID
                    WHERE ps.USER_ID = @userKey", connection);
                deleteInventoryCmd.Parameters.AddWithValue("@userKey", userKey);
                await deleteInventoryCmd.ExecuteNonQueryAsync();

                using var deleteCmd = new MySqlCommand("DELETE FROM player_save WHERE USER_ID = @userKey", connection);
                deleteCmd.Parameters.AddWithValue("@userKey", userKey);
                var deletedCount = await deleteCmd.ExecuteNonQueryAsync();
                System.Diagnostics.Debug.WriteLine($"기존 데이터 {deletedCount}개 삭제됨");

                int successCount = 0;
                int errorCount = 0;
                long currentSaveId = DateTimeOffset.Now.ToUnixTimeMilliseconds();

                // 각 세이브 코드를 프로시저로 저장
                foreach (var saveCode in saveCodes)
                {
                    try
                    {
                        var characterName = RemoveColorCodes(saveCode.CharacterName);
                        var str = ParseIntSafely(saveCode.PhysicalPower);
                        var agi = ParseIntSafely(saveCode.MagicalPower);
                        var intel = ParseIntSafely(saveCode.SpiritualPower);
                        var level = ParseIntSafely(saveCode.Level);
                        var gold = ParseBigIntSafely(saveCode.Gold);
                        var wood = ParseBigIntSafely(saveCode.Wood);

                        if (string.IsNullOrEmpty(saveCode.SaveCode))
                        {
                            System.Diagnostics.Debug.WriteLine($"경고: 빈 세이브 코드 건너뛰기");
                            continue;
                        }

                        // 아이템 1~6번 준비 (비어있으면 "Empty"로 채움)
                        var items = PrepareItemsForStorage(saveCode.Items);

                        // 프로시저 호출
                        using var cmd = new MySqlCommand("ms_usersavecode", connection);
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.CommandTimeout = 60;

                        // 기본 매개변수
                        cmd.Parameters.AddWithValue("p_save_id", currentSaveId);
                        cmd.Parameters.AddWithValue("p_user_key", userKey);
                        cmd.Parameters.AddWithValue("p_save_code", saveCode.SaveCode);
                        cmd.Parameters.AddWithValue("p_level", level);
                        cmd.Parameters.AddWithValue("p_class_code", characterName); // 색깔 코드 제거된 캐릭터명 전달
                        cmd.Parameters.AddWithValue("p_str", str);
                        cmd.Parameters.AddWithValue("p_agi", agi);
                        cmd.Parameters.AddWithValue("p_int", intel);
                        cmd.Parameters.AddWithValue("p_gold", gold);
                        cmd.Parameters.AddWithValue("p_wood", wood);

                        // 아이템 매개변수 이름을 프로시저에 맞게 수정
                        cmd.Parameters.AddWithValue("p_item_code1", items[0]);
                        cmd.Parameters.AddWithValue("p_item_code2", items[1]);
                        cmd.Parameters.AddWithValue("p_item_code3", items[2]);
                        cmd.Parameters.AddWithValue("p_item_code4", items[3]);
                        cmd.Parameters.AddWithValue("p_item_code5", items[4]);
                        cmd.Parameters.AddWithValue("p_item_code6", items[5]);

                        System.Diagnostics.Debug.WriteLine($"프로시저 호출: {characterName} (ID: {currentSaveId})");
                        System.Diagnostics.Debug.WriteLine($"매개변수 상세:");
                        System.Diagnostics.Debug.WriteLine($"  p_save_id: {currentSaveId}");
                        System.Diagnostics.Debug.WriteLine($"  p_user_key: {userKey}");
                        System.Diagnostics.Debug.WriteLine($"  p_class_code: {characterName}");
                        System.Diagnostics.Debug.WriteLine($"  p_level: {level}");
                        System.Diagnostics.Debug.WriteLine($"  능력치: STR={str}, AGI={agi}, INT={intel}");
                        System.Diagnostics.Debug.WriteLine($"  자원: GOLD={gold}, WOOD={wood}");
                        System.Diagnostics.Debug.WriteLine($"  아이템: [{string.Join(", ", items)}]");

                        var cmdResult = await cmd.ExecuteNonQueryAsync();
                        
                        if (cmdResult > 0)
                        {
                            System.Diagnostics.Debug.WriteLine($"? 저장 성공: {characterName} (ID: {currentSaveId})");
                            successCount++;
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"?? 프로시저 실행되었지만 영향받은 행이 0개: {characterName}");
                            System.Diagnostics.Debug.WriteLine($"   - class_master 테이블에 '{characterName}' 캐릭터가 없을 수 있음");
                            errorCount++;
                        }
                        
                        currentSaveId++;
                    }
                    catch (MySqlException mysqlEx)
                    {
                        errorCount++;
                        System.Diagnostics.Debug.WriteLine($"MySQL 오류 (캐릭터: {saveCode.CharacterName}):");
                        System.Diagnostics.Debug.WriteLine($"  오류 번호: {mysqlEx.Number}");
                        System.Diagnostics.Debug.WriteLine($"  오류 메시지: {mysqlEx.Message}");
                        System.Diagnostics.Debug.WriteLine($"  SQL State: {mysqlEx.SqlState}");
                        
                        // 심각한 오류가 아니면 계속 진행
                        if (mysqlEx.Message.Contains("Connection") || mysqlEx.Message.Contains("timeout"))
                        {
                            throw;
                        }
                        continue;
                    }
                    catch (Exception ex)
                    {
                        errorCount++;
                        System.Diagnostics.Debug.WriteLine($"개별 저장 오류: {ex.Message}");
                        continue;
                    }
                }

                System.Diagnostics.Debug.WriteLine($"=== 저장 완료: 성공 {successCount}, 실패 {errorCount} ===");
                return successCount > 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"전체 저장 실패: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 아이템 리스트를 1~6번 슬롯용으로 준비합니다 (빈 슬롯은 "Empty"로 채움)
        /// </summary>
        private string[] PrepareItemsForStorage(List<string> items)
        {
            var preparedItems = new string[6];
            
            // 기본값으로 "Empty" 채우기
            for (int i = 0; i < 6; i++)
            {
                preparedItems[i] = "Empty";
            }
            
            // 실제 아이템이 있으면 해당 슬롯에 배치
            if (items != null && items.Count > 0)
            {
                for (int i = 0; i < Math.Min(items.Count, 6); i++)
                {
                    var item = items[i]?.Trim();
                    if (!string.IsNullOrWhiteSpace(item))
                    {
                        // 색깔 코드 제거
                        item = RemoveColorCodes(item);
                        
                        // 아이템명이 너무 길면 잘라내기 (DB 제한에 맞게)
                        if (item.Length > 95) // 100자 제한에서 여유분 5자 남김
                        {
                            item = item.Substring(0, 95) + "...";
                        }
                        
                        // 빈 문자열이나 공백만 있는 경우 "Empty" 처리
                        if (string.IsNullOrWhiteSpace(item))
                        {
                            item = "Empty";
                        }
                        
                        preparedItems[i] = item;
                    }
                }
            }
            
            System.Diagnostics.Debug.WriteLine($"아이템 준비 완료:");
            for (int i = 0; i < 6; i++)
            {
                System.Diagnostics.Debug.WriteLine($"  슬롯{i + 1}: {preparedItems[i]}");
            }
            
            return preparedItems;
        }

        /// <summary>
        /// 텍스트에서 워크래프트 색깔 코드 제거
        /// </summary>
        private string RemoveColorCodes(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            text = System.Text.RegularExpressions.Regex.Replace(text, @"\|cff[a-fA-F0-9]{6}", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            text = System.Text.RegularExpressions.Regex.Replace(text, @"\|[rR]", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            text = text.Replace("|R", "");
            
            return text.Trim();
        }

        /// <summary>
        /// 문자열을 정수로 안전하게 파싱
        /// </summary>
        private int ParseIntSafely(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return 0;
            
            var cleanValue = new string(value.Where(c => char.IsDigit(c)).ToArray());
            return int.TryParse(cleanValue, out var result) ? result : 0;
        }

        /// <summary>
        /// 문자열을 BIGINT로 안전하게 파싱
        /// </summary>
        private long ParseBigIntSafely(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return 0;
            
            var cleanValue = new string(value.Where(c => char.IsDigit(c)).ToArray());
            return long.TryParse(cleanValue, out var result) ? result : 0;
        }

        /// <summary>
        /// 사용자별 세이브 코드 목록을 조회합니다
        /// </summary>
        public async Task<List<SaveCodeInfo>> LoadUserSaveCodesAsync(string userKey = "default_user")
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                // player_inventory와 JOIN하여 아이템 정보도 함께 조회
                var query = @"
                    SELECT ps.SAVE_ID, ps.USER_ID, ps.SAVE_CODE, ps.P_LEVEL, ps.P_CLASS, 
                           ps.STAT_STR, ps.STAT_AGI, ps.STAT_INT, ps.GOLD, ps.WOOD,
                           ps.CREATE_DTM, ps.UPDATE_DTM, cm.CLASS_NAME,
                           GROUP_CONCAT(pi.ITEM_CODE ORDER BY pi.INVENTORY_ID SEPARATOR '|') as ITEMS
                    FROM player_save ps
                    LEFT JOIN class_master cm ON ps.P_CLASS = cm.CLASS_CODE
                    LEFT JOIN player_inventory pi ON ps.SAVE_ID = pi.SAVE_ID
                    WHERE ps.USER_ID = @userKey
                    GROUP BY ps.SAVE_ID, ps.USER_ID, ps.SAVE_CODE, ps.P_LEVEL, ps.P_CLASS, 
                             ps.STAT_STR, ps.STAT_AGI, ps.STAT_INT, ps.GOLD, ps.WOOD,
                             ps.CREATE_DTM, ps.UPDATE_DTM, cm.CLASS_NAME
                    ORDER BY ps.CREATE_DTM DESC";

                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@userKey", userKey);

                using var reader = await cmd.ExecuteReaderAsync();
                var saveCodes = new List<SaveCodeInfo>();

                while (await reader.ReadAsync())
                {
                    try
                    {
                        var saveId = reader.GetInt64("SAVE_ID");
                        
                        // 아이템 처리
                        var itemsString = reader.IsDBNull("ITEMS") ? "" : reader.GetString("ITEMS");
                        var items = new List<string>();
                        
                        if (!string.IsNullOrWhiteSpace(itemsString))
                        {
                            items = itemsString.Split('|')
                                              .Where(item => !string.IsNullOrWhiteSpace(item) && 
                                                           !item.Equals("Empty", StringComparison.OrdinalIgnoreCase))
                                              .ToList();
                        }
                        
                        var saveCode = new SaveCodeInfo
                        {
                            CharacterName = reader.IsDBNull("CLASS_NAME") ? "알 수 없음" : reader.GetString("CLASS_NAME"),
                            SaveCode = reader.GetString("SAVE_CODE"),
                            FileName = $"세이브_{saveId}.txt",
                            FilePath = "",
                            FileDate = reader.GetDateTime("CREATE_DTM"), // DB에 저장된 시간 사용
                            FullContent = "",
                            Level = reader.GetInt32("P_LEVEL").ToString(),
                            Gold = reader.GetInt64("GOLD").ToString("N0"),
                            Wood = reader.GetInt64("WOOD").ToString("N0"),
                            Experience = "0", // DB에 없는 필드
                            PhysicalPower = reader.GetInt32("STAT_STR").ToString(),
                            MagicalPower = reader.GetInt32("STAT_AGI").ToString(),
                            SpiritualPower = reader.GetInt32("STAT_INT").ToString(),
                            Items = items,
                            ItemsDisplayText = string.Join(", ", items)
                        };

                        saveCodes.Add(saveCode);
                        
                        System.Diagnostics.Debug.WriteLine($"?? 세이브 로드: {saveCode.CharacterName} (ID: {saveId})");
                        System.Diagnostics.Debug.WriteLine($"   날짜: {saveCode.FileDate:yyyy-MM-dd HH:mm:ss} (DB CREATE_DTM)");
                        System.Diagnostics.Debug.WriteLine($"   레벨: {saveCode.Level}, 아이템: {items.Count}개");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"레코드 처리 오류: {ex.Message}");
                        continue;
                    }
                }

                System.Diagnostics.Debug.WriteLine($"사용자 '{userKey}'의 세이브 코드 {saveCodes.Count}개 로드 완료");
                return saveCodes;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"사용자 세이브 코드 조회 오류: {ex.Message}");
                return new List<SaveCodeInfo>();
            }
        }

        /// <summary>
        /// 특정 캐릭터의 세이브 코드만 조회합니다
        /// </summary>
        public async Task<List<SaveCodeInfo>> LoadSaveCodesByCharacterAsync(string characterName, string userKey = "default_user")
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                // player_inventory와 JOIN하여 아이템 정보도 함께 조회
                var query = @"
                    SELECT ps.SAVE_ID, ps.USER_ID, ps.SAVE_CODE, ps.P_LEVEL, ps.P_CLASS, 
                           ps.STAT_STR, ps.STAT_AGI, ps.STAT_INT, ps.GOLD, ps.WOOD,
                           ps.CREATE_DTM, ps.UPDATE_DTM, cm.CLASS_NAME,
                           GROUP_CONCAT(pi.ITEM_CODE ORDER BY pi.INVENTORY_ID SEPARATOR '|') as ITEMS
                    FROM player_save ps
                    LEFT JOIN class_master cm ON ps.P_CLASS = cm.CLASS_CODE
                    LEFT JOIN player_inventory pi ON ps.SAVE_ID = pi.SAVE_ID
                    WHERE ps.USER_ID = @userKey AND cm.CLASS_NAME = @characterName
                    GROUP BY ps.SAVE_ID, ps.USER_ID, ps.SAVE_CODE, ps.P_LEVEL, ps.P_CLASS, 
                             ps.STAT_STR, ps.STAT_AGI, ps.STAT_INT, ps.GOLD, ps.WOOD,
                             ps.CREATE_DTM, ps.UPDATE_DTM, cm.CLASS_NAME
                    ORDER BY ps.CREATE_DTM DESC";

                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@userKey", userKey);
                cmd.Parameters.AddWithValue("@characterName", characterName);

                using var reader = await cmd.ExecuteReaderAsync();
                var saveCodes = new List<SaveCodeInfo>();

                while (await reader.ReadAsync())
                {
                    try
                    {
                        var saveId = reader.GetInt64("SAVE_ID");
                        
                        // 아이템 처리
                        var itemsString = reader.IsDBNull("ITEMS") ? "" : reader.GetString("ITEMS");
                        var items = new List<string>();
                        
                        if (!string.IsNullOrWhiteSpace(itemsString))
                        {
                            items = itemsString.Split('|')
                                              .Where(item => !string.IsNullOrWhiteSpace(item) && 
                                                           !item.Equals("Empty", StringComparison.OrdinalIgnoreCase))
                                              .ToList();
                        }
                        
                        var saveCode = new SaveCodeInfo
                        {
                            CharacterName = reader.IsDBNull("CLASS_NAME") ? "알 수 없음" : reader.GetString("CLASS_NAME"),
                            SaveCode = reader.GetString("SAVE_CODE"),
                            FileName = $"세이브_{saveId}.txt",
                            FilePath = "",
                            FileDate = reader.GetDateTime("CREATE_DTM"), // DB에 저장된 시간 사용
                            FullContent = "",
                            Level = reader.GetInt32("P_LEVEL").ToString(),
                            Gold = reader.GetInt64("GOLD").ToString("N0"),
                            Wood = reader.GetInt64("WOOD").ToString("N0"),
                            Experience = "0",
                            PhysicalPower = reader.GetInt32("STAT_STR").ToString(),
                            MagicalPower = reader.GetInt32("STAT_AGI").ToString(),
                            SpiritualPower = reader.GetInt32("STAT_INT").ToString(),
                            Items = items,
                            ItemsDisplayText = string.Join(", ", items)
                        };

                        saveCodes.Add(saveCode);
                        
                        System.Diagnostics.Debug.WriteLine($"캐릭터 세이브 로드: {saveCode.CharacterName} (ID: {saveId}) - 날짜: {saveCode.FileDate:yyyy-MM-dd HH:mm:ss} - 아이템 {items.Count}개");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"레코드 처리 오류: {ex.Message}");
                        continue;
                    }
                }

                System.Diagnostics.Debug.WriteLine($"캐릭터 '{characterName}'의 세이브 코드 {saveCodes.Count}개 로드 완료");
                return saveCodes;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"캐릭터별 세이브 코드 조회 오류: {ex.Message}");
                return new List<SaveCodeInfo>();
            }
        }

        /// <summary>
        /// 폴더 분석 결과를 로드합니다
        /// </summary>
        public async Task<AnalysisCache?> LoadAnalysisAsync(string folderPath)
        {
            var userSaveCodes = await LoadUserSaveCodesAsync();
            
            if (userSaveCodes.Count == 0)
                return null;

            return new AnalysisCache
            {
                FolderPath = folderPath,
                LastAnalyzed = DateTime.Now,
                FileHashes = new Dictionary<string, DateTime>(),
                SaveCodes = userSaveCodes,
                TotalFiles = userSaveCodes.Count,
                Version = "1.0.0"
            };
        }

        /// <summary>
        /// 캐시 유효성 검사
        /// </summary>
        public bool IsCacheValid(AnalysisCache cache, string folderPath, IEnumerable<TxtFileInfo> currentFiles)
        {
            return false;
        }

        /// <summary>
        /// 데이터베이스 정보를 가져옵니다
        /// </summary>
        public async Task<string> GetDatabaseInfoAsync()
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                var infoCmd = new MySqlCommand(@"
                    SELECT 
                        COUNT(*) as total_saves,
                        COUNT(DISTINCT USER_ID) as total_users,
                        MAX(CREATE_DTM) as last_save_date,
                        MIN(CREATE_DTM) as first_save_date
                    FROM player_save", connection);

                using var reader = await infoCmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    var totalSaves = reader.GetInt32("total_saves");
                    var totalUsers = reader.GetInt32("total_users");
                    var lastSaveDate = reader.IsDBNull("last_save_date") ? (DateTime?)null : reader.GetDateTime("last_save_date");

                    var result = $"데이터베이스 ({_dbSettings.Host}:{_dbSettings.Port}/{_dbSettings.Database})\n";
                    result += $"? 전체 세이브코드: {totalSaves:N0}개\n";
                    result += $"? 등록된 사용자: {totalUsers}명\n";
                    
                    if (lastSaveDate.HasValue)
                    {
                        result += $"? 최근 저장: {lastSaveDate.Value:yyyy-MM-dd HH:mm}";
                    }
                    else
                    {
                        result += "? 저장된 데이터 없음";
                    }

                    return result;
                }

                return $"데이터베이스 ({_dbSettings.Host}:{_dbSettings.Port}/{_dbSettings.Database})\n연결됨 - 데이터 없음";
            }
            catch (Exception ex)
            {
                return $"데이터베이스 연결 실패:\n{ex.Message}";
            }
        }

        /// <summary>
        /// 사용자의 모든 세이브 코드를 삭제합니다
        /// </summary>
        public async Task<bool> ClearAllDataAsync(string userKey = "default_user")
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                // 관련 인벤토리 데이터도 함께 삭제 (참조 무결성)
                var deleteInventoryCmd = new MySqlCommand(@"
                    DELETE pi FROM player_inventory pi
                    INNER JOIN player_save ps ON pi.SAVE_ID = ps.SAVE_ID
                    WHERE ps.USER_ID = @userKey", connection);
                deleteInventoryCmd.Parameters.AddWithValue("@userKey", userKey);
                var deletedInventoryCount = await deleteInventoryCmd.ExecuteNonQueryAsync();
                
                // 세이브 데이터 삭제
                var deleteCmd = new MySqlCommand("DELETE FROM player_save WHERE USER_ID = @userKey", connection);
                deleteCmd.Parameters.AddWithValue("@userKey", userKey);
                var deletedSaveCount = await deleteCmd.ExecuteNonQueryAsync();

                System.Diagnostics.Debug.WriteLine($"삭제 완료 - 세이브: {deletedSaveCount}개, 인벤토리: {deletedInventoryCount}개");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"데이터 삭제 오류: {ex.Message}");
                return false;
            }
        }
    }
}