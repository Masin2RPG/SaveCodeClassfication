using MySql.Data.MySqlClient;
using SaveCodeClassfication.Models;
using System.Data;
using System.Text.Json;
using System.IO;

namespace SaveCodeClassfication.Services
{
    /// <summary>
    /// �����ͺ��̽� ���� �� �۾��� �����ϴ� ����
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
        /// ���� ���ڿ��� �����ɴϴ�
        /// </summary>
        public string GetConnectionString()
        {
            return _connectionString;
        }

        // UserService ��������Ʈ �޼����
        public async Task<bool> ValidateUserAsync(string userId, string password) 
            => await _userService.ValidateUserAsync(userId, password);

        public async Task<ValidationResult> ValidateRegistrationAsync(string userId, string authKey) 
            => await _userService.ValidateRegistrationAsync(userId, authKey);

        public async Task<RegisterResult> RegisterUserAsync(string userId, string password, string authKey) 
            => await _userService.RegisterUserAsync(userId, password, authKey);

        /// <summary>
        /// �����ͺ��̽� ������ �׽�Ʈ�մϴ�
        /// </summary>
        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"=== �����ͺ��̽� ���� �׽�Ʈ ===");
                System.Diagnostics.Debug.WriteLine($"ȣ��Ʈ: {_dbSettings.Host}:{_dbSettings.Port}");
                System.Diagnostics.Debug.WriteLine($"�����ͺ��̽�: {_dbSettings.Database}");
                System.Diagnostics.Debug.WriteLine($"�����: {_dbSettings.UserId}");
                System.Diagnostics.Debug.WriteLine($"SSL: {_dbSettings.UseSSL}");
                System.Diagnostics.Debug.WriteLine($"���� ���ڿ�: {_connectionString}");

                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();
                
                var isOpen = connection.State == ConnectionState.Open;
                System.Diagnostics.Debug.WriteLine($"���� ����: {connection.State}");
                System.Diagnostics.Debug.WriteLine($"���� ����: {connection.ServerVersion}");
                
                return isOpen;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"=== ���� �׽�Ʈ ���� ===");
                System.Diagnostics.Debug.WriteLine($"���� �޽���: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"���� Ÿ��: {ex.GetType().Name}");
                
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"���� ����: {ex.InnerException.Message}");
                }
                
                return false;
            }
        }

        /// <summary>
        /// �����ͺ��̽� �ʱ�ȭ
        /// </summary>
        public async Task<bool> InitializeDatabaseAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"=== �����ͺ��̽� �ʱ�ȭ ===");
                
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();
                
                // �����ͺ��̽� ����
                System.Diagnostics.Debug.WriteLine($"�����ͺ��̽� '{_dbSettings.Database}' ���� ��...");
                await connection.ChangeDatabaseAsync(_dbSettings.Database);
                
                // ���ν��� ���� Ȯ��
                var checkProcedureCmd = new MySqlCommand(@"
                    SELECT COUNT(*) 
                    FROM information_schema.ROUTINES 
                    WHERE ROUTINE_SCHEMA = @database 
                    AND ROUTINE_NAME = 'ms_usersavecode'", connection);
                checkProcedureCmd.Parameters.AddWithValue("@database", _dbSettings.Database);
                
                var procedureExists = Convert.ToInt32(await checkProcedureCmd.ExecuteScalarAsync()) > 0;
                System.Diagnostics.Debug.WriteLine($"���ν��� 'ms_usersavecode' ���� ����: {procedureExists}");
                
                // ���̺� ���� Ȯ��
                var checkTableCmd = new MySqlCommand(@"
                    SELECT COUNT(*) 
                    FROM information_schema.TABLES 
                    WHERE TABLE_SCHEMA = @database 
                    AND TABLE_NAME = 'player_save'", connection);
                checkTableCmd.Parameters.AddWithValue("@database", _dbSettings.Database);
                
                var tableExists = Convert.ToInt32(await checkTableCmd.ExecuteScalarAsync()) > 0;
                System.Diagnostics.Debug.WriteLine($"���̺� 'player_save' ���� ����: {tableExists}");
                
                if (!procedureExists || !tableExists)
                {
                    System.Diagnostics.Debug.WriteLine("�ʼ� ���ν��� �Ǵ� ���̺��� �������� �ʽ��ϴ�!");
                    return false;
                }
                
                System.Diagnostics.Debug.WriteLine("�����ͺ��̽� �ʱ�ȭ �Ϸ�");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"=== �����ͺ��̽� �ʱ�ȭ ���� ===");
                System.Diagnostics.Debug.WriteLine($"����: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// ���̺� �ڵ� �����͸� �����մϴ� (���ν��� ���)
        /// </summary>
        public async Task<bool> SaveAnalysisAsync(string folderPath, List<SaveCodeInfo> saveCodes, Dictionary<string, DateTime> fileHashes, string userKey = "default_user")
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"=== �����ͺ��̽� ���� ���� ===");
                System.Diagnostics.Debug.WriteLine($"������ ���̺� �ڵ� ��: {saveCodes.Count}");
                System.Diagnostics.Debug.WriteLine($"����� Ű: {userKey}");

                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();
                System.Diagnostics.Debug.WriteLine("�����ͺ��̽� ���� ����");

                // ���� ������ ����
                System.Diagnostics.Debug.WriteLine("���� ������ ���� ��...");
                using var deleteInventoryCmd = new MySqlCommand(@"
                    DELETE pi FROM player_inventory pi
                    INNER JOIN player_save ps ON pi.SAVE_ID = ps.SAVE_ID
                    WHERE ps.USER_ID = @userKey", connection);
                deleteInventoryCmd.Parameters.AddWithValue("@userKey", userKey);
                await deleteInventoryCmd.ExecuteNonQueryAsync();

                using var deleteCmd = new MySqlCommand("DELETE FROM player_save WHERE USER_ID = @userKey", connection);
                deleteCmd.Parameters.AddWithValue("@userKey", userKey);
                var deletedCount = await deleteCmd.ExecuteNonQueryAsync();
                System.Diagnostics.Debug.WriteLine($"���� ������ {deletedCount}�� ������");

                int successCount = 0;
                int errorCount = 0;
                long currentSaveId = DateTimeOffset.Now.ToUnixTimeMilliseconds();

                // �� ���̺� �ڵ带 ���ν����� ����
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
                            System.Diagnostics.Debug.WriteLine($"���: �� ���̺� �ڵ� �ǳʶٱ�");
                            continue;
                        }

                        // ������ 1~6�� �غ� (��������� "Empty"�� ä��)
                        var items = PrepareItemsForStorage(saveCode.Items);

                        // ���ν��� ȣ��
                        using var cmd = new MySqlCommand("ms_usersavecode", connection);
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.CommandTimeout = 60;

                        // �⺻ �Ű�����
                        cmd.Parameters.AddWithValue("p_save_id", currentSaveId);
                        cmd.Parameters.AddWithValue("p_user_key", userKey);
                        cmd.Parameters.AddWithValue("p_save_code", saveCode.SaveCode);
                        cmd.Parameters.AddWithValue("p_level", level);
                        cmd.Parameters.AddWithValue("p_class_code", characterName); // ���� �ڵ� ���ŵ� ĳ���͸� ����
                        cmd.Parameters.AddWithValue("p_str", str);
                        cmd.Parameters.AddWithValue("p_agi", agi);
                        cmd.Parameters.AddWithValue("p_int", intel);
                        cmd.Parameters.AddWithValue("p_gold", gold);
                        cmd.Parameters.AddWithValue("p_wood", wood);

                        // ������ �Ű����� �̸��� ���ν����� �°� ����
                        cmd.Parameters.AddWithValue("p_item_code1", items[0]);
                        cmd.Parameters.AddWithValue("p_item_code2", items[1]);
                        cmd.Parameters.AddWithValue("p_item_code3", items[2]);
                        cmd.Parameters.AddWithValue("p_item_code4", items[3]);
                        cmd.Parameters.AddWithValue("p_item_code5", items[4]);
                        cmd.Parameters.AddWithValue("p_item_code6", items[5]);

                        System.Diagnostics.Debug.WriteLine($"���ν��� ȣ��: {characterName} (ID: {currentSaveId})");
                        System.Diagnostics.Debug.WriteLine($"�Ű����� ��:");
                        System.Diagnostics.Debug.WriteLine($"  p_save_id: {currentSaveId}");
                        System.Diagnostics.Debug.WriteLine($"  p_user_key: {userKey}");
                        System.Diagnostics.Debug.WriteLine($"  p_class_code: {characterName}");
                        System.Diagnostics.Debug.WriteLine($"  p_level: {level}");
                        System.Diagnostics.Debug.WriteLine($"  �ɷ�ġ: STR={str}, AGI={agi}, INT={intel}");
                        System.Diagnostics.Debug.WriteLine($"  �ڿ�: GOLD={gold}, WOOD={wood}");
                        System.Diagnostics.Debug.WriteLine($"  ������: [{string.Join(", ", items)}]");

                        var cmdResult = await cmd.ExecuteNonQueryAsync();
                        
                        if (cmdResult > 0)
                        {
                            System.Diagnostics.Debug.WriteLine($"? ���� ����: {characterName} (ID: {currentSaveId})");
                            successCount++;
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"?? ���ν��� ����Ǿ����� ������� ���� 0��: {characterName}");
                            System.Diagnostics.Debug.WriteLine($"   - class_master ���̺� '{characterName}' ĳ���Ͱ� ���� �� ����");
                            errorCount++;
                        }
                        
                        currentSaveId++;
                    }
                    catch (MySqlException mysqlEx)
                    {
                        errorCount++;
                        System.Diagnostics.Debug.WriteLine($"MySQL ���� (ĳ����: {saveCode.CharacterName}):");
                        System.Diagnostics.Debug.WriteLine($"  ���� ��ȣ: {mysqlEx.Number}");
                        System.Diagnostics.Debug.WriteLine($"  ���� �޽���: {mysqlEx.Message}");
                        System.Diagnostics.Debug.WriteLine($"  SQL State: {mysqlEx.SqlState}");
                        
                        // �ɰ��� ������ �ƴϸ� ��� ����
                        if (mysqlEx.Message.Contains("Connection") || mysqlEx.Message.Contains("timeout"))
                        {
                            throw;
                        }
                        continue;
                    }
                    catch (Exception ex)
                    {
                        errorCount++;
                        System.Diagnostics.Debug.WriteLine($"���� ���� ����: {ex.Message}");
                        continue;
                    }
                }

                System.Diagnostics.Debug.WriteLine($"=== ���� �Ϸ�: ���� {successCount}, ���� {errorCount} ===");
                return successCount > 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"��ü ���� ����: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// ������ ����Ʈ�� 1~6�� ���Կ����� �غ��մϴ� (�� ������ "Empty"�� ä��)
        /// </summary>
        private string[] PrepareItemsForStorage(List<string> items)
        {
            var preparedItems = new string[6];
            
            // �⺻������ "Empty" ä���
            for (int i = 0; i < 6; i++)
            {
                preparedItems[i] = "Empty";
            }
            
            // ���� �������� ������ �ش� ���Կ� ��ġ
            if (items != null && items.Count > 0)
            {
                for (int i = 0; i < Math.Min(items.Count, 6); i++)
                {
                    var item = items[i]?.Trim();
                    if (!string.IsNullOrWhiteSpace(item))
                    {
                        // ���� �ڵ� ����
                        item = RemoveColorCodes(item);
                        
                        // �����۸��� �ʹ� ��� �߶󳻱� (DB ���ѿ� �°�)
                        if (item.Length > 95) // 100�� ���ѿ��� ������ 5�� ����
                        {
                            item = item.Substring(0, 95) + "...";
                        }
                        
                        // �� ���ڿ��̳� ���鸸 �ִ� ��� "Empty" ó��
                        if (string.IsNullOrWhiteSpace(item))
                        {
                            item = "Empty";
                        }
                        
                        preparedItems[i] = item;
                    }
                }
            }
            
            System.Diagnostics.Debug.WriteLine($"������ �غ� �Ϸ�:");
            for (int i = 0; i < 6; i++)
            {
                System.Diagnostics.Debug.WriteLine($"  ����{i + 1}: {preparedItems[i]}");
            }
            
            return preparedItems;
        }

        /// <summary>
        /// �ؽ�Ʈ���� ��ũ����Ʈ ���� �ڵ� ����
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
        /// ���ڿ��� ������ �����ϰ� �Ľ�
        /// </summary>
        private int ParseIntSafely(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return 0;
            
            var cleanValue = new string(value.Where(c => char.IsDigit(c)).ToArray());
            return int.TryParse(cleanValue, out var result) ? result : 0;
        }

        /// <summary>
        /// ���ڿ��� BIGINT�� �����ϰ� �Ľ�
        /// </summary>
        private long ParseBigIntSafely(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return 0;
            
            var cleanValue = new string(value.Where(c => char.IsDigit(c)).ToArray());
            return long.TryParse(cleanValue, out var result) ? result : 0;
        }

        /// <summary>
        /// ����ں� ���̺� �ڵ� ����� ��ȸ�մϴ�
        /// </summary>
        public async Task<List<SaveCodeInfo>> LoadUserSaveCodesAsync(string userKey = "default_user")
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                // player_inventory�� JOIN�Ͽ� ������ ������ �Բ� ��ȸ
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
                        
                        // ������ ó��
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
                            CharacterName = reader.IsDBNull("CLASS_NAME") ? "�� �� ����" : reader.GetString("CLASS_NAME"),
                            SaveCode = reader.GetString("SAVE_CODE"),
                            FileName = $"���̺�_{saveId}.txt",
                            FilePath = "",
                            FileDate = reader.GetDateTime("CREATE_DTM"), // DB�� ����� �ð� ���
                            FullContent = "",
                            Level = reader.GetInt32("P_LEVEL").ToString(),
                            Gold = reader.GetInt64("GOLD").ToString("N0"),
                            Wood = reader.GetInt64("WOOD").ToString("N0"),
                            Experience = "0", // DB�� ���� �ʵ�
                            PhysicalPower = reader.GetInt32("STAT_STR").ToString(),
                            MagicalPower = reader.GetInt32("STAT_AGI").ToString(),
                            SpiritualPower = reader.GetInt32("STAT_INT").ToString(),
                            Items = items,
                            ItemsDisplayText = string.Join(", ", items)
                        };

                        saveCodes.Add(saveCode);
                        
                        System.Diagnostics.Debug.WriteLine($"?? ���̺� �ε�: {saveCode.CharacterName} (ID: {saveId})");
                        System.Diagnostics.Debug.WriteLine($"   ��¥: {saveCode.FileDate:yyyy-MM-dd HH:mm:ss} (DB CREATE_DTM)");
                        System.Diagnostics.Debug.WriteLine($"   ����: {saveCode.Level}, ������: {items.Count}��");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"���ڵ� ó�� ����: {ex.Message}");
                        continue;
                    }
                }

                System.Diagnostics.Debug.WriteLine($"����� '{userKey}'�� ���̺� �ڵ� {saveCodes.Count}�� �ε� �Ϸ�");
                return saveCodes;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"����� ���̺� �ڵ� ��ȸ ����: {ex.Message}");
                return new List<SaveCodeInfo>();
            }
        }

        /// <summary>
        /// Ư�� ĳ������ ���̺� �ڵ常 ��ȸ�մϴ�
        /// </summary>
        public async Task<List<SaveCodeInfo>> LoadSaveCodesByCharacterAsync(string characterName, string userKey = "default_user")
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                // player_inventory�� JOIN�Ͽ� ������ ������ �Բ� ��ȸ
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
                        
                        // ������ ó��
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
                            CharacterName = reader.IsDBNull("CLASS_NAME") ? "�� �� ����" : reader.GetString("CLASS_NAME"),
                            SaveCode = reader.GetString("SAVE_CODE"),
                            FileName = $"���̺�_{saveId}.txt",
                            FilePath = "",
                            FileDate = reader.GetDateTime("CREATE_DTM"), // DB�� ����� �ð� ���
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
                        
                        System.Diagnostics.Debug.WriteLine($"ĳ���� ���̺� �ε�: {saveCode.CharacterName} (ID: {saveId}) - ��¥: {saveCode.FileDate:yyyy-MM-dd HH:mm:ss} - ������ {items.Count}��");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"���ڵ� ó�� ����: {ex.Message}");
                        continue;
                    }
                }

                System.Diagnostics.Debug.WriteLine($"ĳ���� '{characterName}'�� ���̺� �ڵ� {saveCodes.Count}�� �ε� �Ϸ�");
                return saveCodes;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ĳ���ͺ� ���̺� �ڵ� ��ȸ ����: {ex.Message}");
                return new List<SaveCodeInfo>();
            }
        }

        /// <summary>
        /// ���� �м� ����� �ε��մϴ�
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
        /// ĳ�� ��ȿ�� �˻�
        /// </summary>
        public bool IsCacheValid(AnalysisCache cache, string folderPath, IEnumerable<TxtFileInfo> currentFiles)
        {
            return false;
        }

        /// <summary>
        /// �����ͺ��̽� ������ �����ɴϴ�
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

                    var result = $"�����ͺ��̽� ({_dbSettings.Host}:{_dbSettings.Port}/{_dbSettings.Database})\n";
                    result += $"? ��ü ���̺��ڵ�: {totalSaves:N0}��\n";
                    result += $"? ��ϵ� �����: {totalUsers}��\n";
                    
                    if (lastSaveDate.HasValue)
                    {
                        result += $"? �ֱ� ����: {lastSaveDate.Value:yyyy-MM-dd HH:mm}";
                    }
                    else
                    {
                        result += "? ����� ������ ����";
                    }

                    return result;
                }

                return $"�����ͺ��̽� ({_dbSettings.Host}:{_dbSettings.Port}/{_dbSettings.Database})\n����� - ������ ����";
            }
            catch (Exception ex)
            {
                return $"�����ͺ��̽� ���� ����:\n{ex.Message}";
            }
        }

        /// <summary>
        /// ������� ��� ���̺� �ڵ带 �����մϴ�
        /// </summary>
        public async Task<bool> ClearAllDataAsync(string userKey = "default_user")
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                // ���� �κ��丮 �����͵� �Բ� ���� (���� ���Ἲ)
                var deleteInventoryCmd = new MySqlCommand(@"
                    DELETE pi FROM player_inventory pi
                    INNER JOIN player_save ps ON pi.SAVE_ID = ps.SAVE_ID
                    WHERE ps.USER_ID = @userKey", connection);
                deleteInventoryCmd.Parameters.AddWithValue("@userKey", userKey);
                var deletedInventoryCount = await deleteInventoryCmd.ExecuteNonQueryAsync();
                
                // ���̺� ������ ����
                var deleteCmd = new MySqlCommand("DELETE FROM player_save WHERE USER_ID = @userKey", connection);
                deleteCmd.Parameters.AddWithValue("@userKey", userKey);
                var deletedSaveCount = await deleteCmd.ExecuteNonQueryAsync();

                System.Diagnostics.Debug.WriteLine($"���� �Ϸ� - ���̺�: {deletedSaveCount}��, �κ��丮: {deletedInventoryCount}��");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"������ ���� ����: {ex.Message}");
                return false;
            }
        }
    }
}