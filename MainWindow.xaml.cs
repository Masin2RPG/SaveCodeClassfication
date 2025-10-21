using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SaveCodeClassfication.Models;
using SaveCodeClassfication.Services;
using SaveCodeClassfication.Utils;
using SaveCodeClassfication.Components;
using MediaBrushes = System.Windows.Media.Brushes;
using WpfMessageBox = System.Windows.MessageBox;

namespace SaveCodeClassfication
{
    /// <summary>
    /// 메인 윈도우 - 리팩토링된 버전
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Fields
        private string? _selectedFolderPath;
        private readonly ObservableCollection<TxtFileInfo> _txtFiles = new();
        private readonly ObservableCollection<CharacterInfo> _characters = new();
        private readonly ObservableCollection<CharacterInfo> _filteredCharacters = new();
        private readonly ObservableCollection<SaveCodeInfo> _currentSaveCodes = new();
        private string _currentSearchText = string.Empty;
        
        // 간단한 날짜 정렬 시스템
        private SimpleSortSettings _simpleSortSettings = new();

        // Services
        private readonly SettingsService _settingsService;
        private readonly CharacterNameMappingService _nameMappingService;
        private readonly SaveCodeParserService _parserService;
        private DatabaseService _databaseService;
        private CacheService _cacheService;
        private UserService _userService; // 토큰 생성을 위한 UserService 추가

        // 로그인 사용자 정보
        public string LoggedInUserId { get; set; } = string.Empty;
        public bool IsAdminUser { get; set; } = false;
        #endregion

        #region Constructor
        public MainWindow()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== MainWindow 생성자 시작 ===");
                
                InitializeComponent();
                System.Diagnostics.Debug.WriteLine("InitializeComponent 완료");
                
                // 기본 서비스만 초기화
                System.Diagnostics.Debug.WriteLine("기본 Services 초기화 중...");
                _settingsService = new SettingsService(PathConstants.ConfigFilePath);
                
                // 기본 데이터베이스 서비스 초기화
                var defaultDbSettings = new DatabaseSettings();
                _databaseService = new DatabaseService(defaultDbSettings);
                _cacheService = new CacheService(_databaseService, _settingsService);
                _userService = new UserService(defaultDbSettings.GetConnectionString());
                _nameMappingService = new CharacterNameMappingService(PathConstants.CharNameMappingPath);
                _parserService = new SaveCodeParserService(_nameMappingService);
                System.Diagnostics.Debug.WriteLine("기본 Services 초기화 완료");
                
                // UI 기본 설정
                System.Diagnostics.Debug.WriteLine("UI 기본 설정 중...");
                LstCharacters.ItemsSource = _filteredCharacters;
                LstSaveCodes.ItemsSource = _currentSaveCodes;
                System.Diagnostics.Debug.WriteLine("UI 기본 설정 완료");
                
                System.Diagnostics.Debug.WriteLine("=== MainWindow 생성자 완료 ===");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"=== MainWindow 생성자 오류 ===");
                System.Diagnostics.Debug.WriteLine($"오류 타입: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"오류 메시지: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"스택 트레이스: {ex.StackTrace}");
                
                // 생성자 실패 시 예외를 다시 던져서 App.xaml.cs에서 처리
                throw;
            }
        }

        /// <summary>
        /// 메인창 초기화를 App.xaml.cs에서 호출할 수 있도록 public 메서드로 제공
        /// </summary>
        public async Task PostInitializeAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== PostInitializeAsync 시작 ===");
                
                // 사용자 정보 설정 (UI 스레드에서 직접 실행)
                if (!string.IsNullOrEmpty(LoggedInUserId))
                {
                    var adminText = IsAdminUser ? " (관리자)" : "";
                    Title = $"Masin 세이브코드 분류 - {LoggedInUserId}님{adminText} 환영합니다";
                    System.Diagnostics.Debug.WriteLine($"창 제목 설정: {Title}");
                    
                    // 관리자 권한에 따라 토큰 생성 탭 표시/숨김
                    SetTokenGeneratorTabVisibility(IsAdminUser);
                    System.Diagnostics.Debug.WriteLine("토큰 생성 탭 설정 완료");
                }
                
                // TokenGeneratorControl 설정
                try
                {
                    if (TokenGeneratorControl != null && _userService != null)
                    {
                        TokenGeneratorControl.SetUserService(_userService);
                        System.Diagnostics.Debug.WriteLine("TokenGeneratorControl 설정 완료");
                    }
                }
                catch (Exception tokenEx)
                {
                    System.Diagnostics.Debug.WriteLine($"TokenGeneratorControl 설정 오류: {tokenEx.Message}");
                }
                
                System.Diagnostics.Debug.WriteLine("=== PostInitializeAsync 완료 (간소화 버전) ===");
                await Task.CompletedTask; // 비동기 메서드 형식 유지
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PostInitializeAsync 오류: {ex.Message}");
                UpdateStatus($"초기화 중 오류: {ex.Message}");
            }
        }
        #endregion

        #region Initialization
        /// <summary>
        /// 애플리케이션 초기화
        /// </summary>
        private async Task InitializeApplicationAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== InitializeApplicationAsync 시작 ===");
                
                // 설정 로드 및 정렬 옵션 설정
                System.Diagnostics.Debug.WriteLine("설정 로드 중...");
                var settings = await _settingsService.LoadSettingsAsync();
                _simpleSortSettings = settings.SimpleSortSettings;
                System.Diagnostics.Debug.WriteLine("설정 로드 완료");
                
                // 데이터베이스 서비스 재초기화 (설정에서 로드된 값으로)
                System.Diagnostics.Debug.WriteLine("데이터베이스 서비스 재초기화 중...");
                _databaseService = new DatabaseService(settings.DatabaseSettings);
                _cacheService = new CacheService(_databaseService, _settingsService);
                System.Diagnostics.Debug.WriteLine("데이터베이스 서비스 재초기화 완료");
                
                // UserService 재초기화 (설정에서 로드된 연결 문자열로)
                System.Diagnostics.Debug.WriteLine("UserService 재초기화 중...");
                _userService = new UserService(settings.DatabaseSettings.GetConnectionString());
                System.Diagnostics.Debug.WriteLine("UserService 재초기화 완료");

                // 데이터베이스 연결 테스트 및 초기화
                System.Diagnostics.Debug.WriteLine("데이터베이스 연결 테스트 중...");
                UpdateStatus("데이터베이스 연결을 확인하는 중...");
                var dbConnected = await _cacheService.TestDatabaseConnectionAsync();
                
                if (dbConnected)
                {
                    System.Diagnostics.Debug.WriteLine("데이터베이스 연결 성공");
                    UpdateStatus("데이터베이스 테이블을 초기화하는 중...");
                    var dbInitialized = await _cacheService.InitializeDatabaseAsync();
                    
                    if (dbInitialized)
                    {
                        System.Diagnostics.Debug.WriteLine("데이터베이스 초기화 성공");
                        UpdateStatus("데이터베이스 연결 성공");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("데이터베이스 초기화 실패");
                        UpdateStatus("데이터베이스 초기화 실패");
                        // 초기화 실패 시에도 계속 진행
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("데이터베이스 연결 실패");
                    UpdateStatus("데이터베이스 연결 실패");
                    // 연결 실패 시에도 계속 진행
                }
                
                // 캐릭터 이름 매핑 로드
                try
                {
                    System.Diagnostics.Debug.WriteLine("캐릭터 이름 매핑 로드 중...");
                    var mappingLoaded = await _nameMappingService.LoadMappingsAsync();
                    if (mappingLoaded)
                    {
                        System.Diagnostics.Debug.WriteLine($"캐릭터 이름 매핑 로드 성공: {_nameMappingService.GetMappingCount()}개");
                        UpdateStatus($"캐릭터 이름 매핑 로드 완료: {_nameMappingService.GetMappingCount()}개 매핑");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("캐릭터 이름 매핑 파일 없음");
                        UpdateStatus("charName.json 파일이 없습니다. 원래 캐릭터명을 사용합니다.");
                    }
                }
                catch (Exception mappingEx)
                {
                    System.Diagnostics.Debug.WriteLine($"캐릭터 이름 매핑 로드 오류: {mappingEx.Message}");
                    UpdateStatus($"캐릭터 이름 매핑 로드 오류: {mappingEx.Message}");
                }

                // 데이터베이스 정보 업데이트
                try
                {
                    System.Diagnostics.Debug.WriteLine("데이터베이스 정보 업데이트 중...");
                    await UpdateCacheInfoAsync();
                    System.Diagnostics.Debug.WriteLine("데이터베이스 정보 업데이트 완료");
                }
                catch (Exception cacheInfoEx)
                {
                    System.Diagnostics.Debug.WriteLine($"데이터베이스 정보 업데이트 오류: {cacheInfoEx.Message}");
                    UpdateStatus($"데이터베이스 정보 업데이트 오류: {cacheInfoEx.Message}");
                }
                
                // 폴더 로드
                try
                {
                    if (!string.IsNullOrEmpty(settings.LastSelectedFolderPath) && 
                        Directory.Exists(settings.LastSelectedFolderPath))
                    {
                        System.Diagnostics.Debug.WriteLine($"저장된 폴더 로드: {settings.LastSelectedFolderPath}");
                        UpdateStatus("저장된 폴더 경로를 로드하는 중...");
                        await LoadFolderAsync(settings.LastSelectedFolderPath, autoLoad: true);
                        System.Diagnostics.Debug.WriteLine("폴더 로드 완료");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("저장된 폴더 경로 없음");
                        UpdateStatus("저장된 폴더 경로가 없습니다. 폴더를 선택해주세요.");
                    }
                }
                catch (Exception folderEx)
                {
                    System.Diagnostics.Debug.WriteLine($"폴더 로드 오류: {folderEx.Message}");
                    UpdateStatus($"폴더 로드 오류: {folderEx.Message}");
                }
                
                System.Diagnostics.Debug.WriteLine("=== InitializeApplicationAsync 완료 ===");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"=== InitializeApplicationAsync 전체 오류 ===");
                System.Diagnostics.Debug.WriteLine($"오류 타입: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"오류 메시지: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"스택 트레이스: {ex.StackTrace}");
                UpdateStatus($"초기화 오류: {ex.Message}");
                
                // 초기화 실패해도 애플리케이션은 계속 실행
            }
        }
        #endregion

        #region Folder Operations
        /// <summary>
        /// 폴더 선택 버튼 클릭
        /// </summary>
        private async void BtnSelectFolder_Click(object sender, RoutedEventArgs e)
        {
            var folderDialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "세이브 코드 TXT 파일이 있는 폴더를 선택해주세요",
                UseDescriptionForTitle = true
            };

            if (!string.IsNullOrEmpty(_selectedFolderPath) && Directory.Exists(_selectedFolderPath))
            {
                folderDialog.SelectedPath = _selectedFolderPath;
            }

            if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                await LoadFolderAsync(folderDialog.SelectedPath);
                await SaveSelectedFolderPathAsync(folderDialog.SelectedPath);
            }
        }

        /// <summary>
        /// 폴더 새로고침 버튼 클릭
        /// </summary>
        private async void BtnReloadFolder_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_selectedFolderPath) && Directory.Exists(_selectedFolderPath))
            {
                await LoadFolderAsync(_selectedFolderPath);
            }
            else
            {
                WpfMessageBox.Show("새로고침할 폴더가 없습니다. 먼저 폴더를 선택해주세요.", "알림", 
                                  MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        /// <summary>
        /// 폴더를 로드합니다
        /// </summary>
        private async Task LoadFolderAsync(string folderPath, bool autoLoad = false)
        {
            try
            {
                ShowLoadingState(true);
                
                UpdateStatus(autoLoad ? "저장된 폴더를 자동으로 로드하는 중..." : "폴더를 스캔하는 중...");

                _selectedFolderPath = folderPath;
                TxtFolderPath.Text = folderPath;
                TxtFolderPath.Foreground = MediaBrushes.Black;

                // 이전 데이터 정리
                ClearData();

                // 폴더 정보 표시
                var folderInfo = new DirectoryInfo(folderPath);
                var folderInfoTextBlock = (TextBlock)TagFolderInfo.Child;
                folderInfoTextBlock.Text = $"폴더: {folderInfo.Name}";

                // TXT 파일 찾기
                var txtFiles = Directory.GetFiles(folderPath, "*.txt", SearchOption.AllDirectories);
                
                if (txtFiles.Length == 0)
                {
                    HandleNoFilesFound(autoLoad);
                    return;
                }

                // 파일 정보 로드
                LoadFileInformation(txtFiles, folderPath);

                // 캐시 처리
                await HandleCacheAsync(folderPath, txtFiles, autoLoad);
            }
            catch (Exception ex)
            {
                ShowError($"폴더 로드 중 오류가 발생했습니다: {ex.Message}");
            }
            finally
            {
                ShowLoadingState(false);
            }
        }

        /// <summary>
        /// 파일 정보를 로드합니다
        /// </summary>
        private void LoadFileInformation(string[] txtFiles, string folderPath)
        {
            foreach (var filePath in txtFiles)
            {
                var fileInfo = new FileInfo(filePath);
                var txtFileInfo = new TxtFileInfo
                {
                    FilePath = filePath,
                    FileName = fileInfo.Name,
                    FileSize = fileInfo.Length,
                    FileSizeText = $"크기: {FileUtils.FormatFileSize(fileInfo.Length)}",
                    RelativePath = Path.GetRelativePath(folderPath, filePath)
                };
                _txtFiles.Add(txtFileInfo);
            }

            TagFileCountText.Text = $"파일 수: {txtFiles.Length}개";
            TagFileCount.Visibility = Visibility.Visible;
            BtnAnalyzeFiles.IsEnabled = true;
        }

        /// <summary>
        /// 파일이 없는 경우를 처리합니다
        /// </summary>
        private void HandleNoFilesFound(bool autoLoad)
        {
            TagFileCountText.Text = "TXT 파일이 없습니다";
            TagFileCount.Visibility = Visibility.Visible;
            UpdateStatus("선택된 폴더에 TXT 파일이 없습니다");
            BtnAnalyzeFiles.IsEnabled = false;
            
            if (!autoLoad)
            {
                WpfMessageBox.Show("선택된 폴더에 TXT 파일이 없습니다.", "정보", 
                                  MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        /// <summary>
        /// 캐시를 처리합니다
        /// </summary>
        private async Task HandleCacheAsync(string folderPath, string[] txtFiles, bool autoLoad)
        {
            UpdateStatus("데이터베이스에서 저작된 세이브 코드를 확인하는 중...");
            
            // 데이터베이스에서 데이터 조회 시도
            await LoadCharactersFromDatabase();
            
            if (_characters.Count > 0)
            {
                var message = $"데이터베이스에서 로드 완료: {_characters.Count}개 캐릭터를 찾았습니다.";
                UpdateStatus(message);
                
                if (!autoLoad)
                {
                    WpfMessageBox.Show($"데이터베이스에서 기존 세이브 코드를 로드했습니다!\n{_characters.Count}개의 캐릭터를 찾았습니다.\n\n최신 분석이 필요하면 '파일 분석' 버튼을 클릭하세요.", 
                                      "데이터베이스 로드 완료", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                var message = autoLoad 
                    ? $"자동 로드 완료: {txtFiles.Length}개의 TXT 파일을 찾았습니다. 데이터베이스에 저장된 데이터가 없어 분석이 필요합니다."
                    : $"{txtFiles.Length}개의 TXT 파일을 찾았습니다. 데이터베이스에 저장된 데이터가 없습니다. 파일 분석을 진행해주세요.";
                
                UpdateStatus(message);
                
                if (!autoLoad)
                {
                    WpfMessageBox.Show($"{txtFiles.Length}개의 TXT 파일을 찾았습니다.\n데이터베이스에 저장된 세이브 코드가 없습니다.\n\n'파일 분석' 버튼을 클릭하여 캐릭터별로 분류하세요.", 
                                      "분석 필요", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        /// <summary>
        /// 선택된 폴더 경로를 저장합니다
        /// </summary>
        private async Task SaveSelectedFolderPathAsync(string folderPath)
        {
            try
            {
                var settings = await _settingsService.LoadSettingsAsync();
                settings.LastSelectedFolderPath = folderPath;
                settings.LastUpdated = DateTime.Now;
                
                var success = await _settingsService.SaveSettingsAsync(settings);
                if (!success)
                {
                    UpdateStatus("폴더 경로 저장 실패");
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"폴더 경로 저장 실패: {ex.Message}");
            }
        }
        #endregion

        #region File Analysis
        /// <summary>
        /// 파일 분석 버튼 클릭
        /// </summary>
        private async void BtnAnalyzeFiles_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ShowLoadingState(true);
                UpdateStatus("파일을 분석하는 중...");

                var result = await AnalyzeFilesAsync();
                
                await HandleAnalysisResultAsync(result);
            }
            catch (Exception ex)
            {
                ShowError($"파일 분석 중 오류가 발생했습니다: {ex.Message}");
            }
            finally
            {
                ShowLoadingState(false);
            }
        }

        /// <summary>
        /// 파일들을 분석합니다
        /// </summary>
        private async Task<(List<SaveCodeInfo> SaveCodes, int ValidFiles)> AnalyzeFilesAsync()
        {
            _characters.Clear();
            var allSaveCodes = new List<SaveCodeInfo>();
            int processedFiles = 0;
            int validFiles = 0;

            foreach (var txtFile in _txtFiles)
            {
                processedFiles++;
                UpdateStatus($"파일 분석 중... ({processedFiles}/{_txtFiles.Count})");

                try
                {
                    var content = await File.ReadAllTextAsync(txtFile.FilePath, Encoding.UTF8);
                    var saveCodeInfo = _parserService.ParseSaveCodeFile(content, txtFile);

                    if (saveCodeInfo != null)
                    {
                        validFiles++;
                        allSaveCodes.Add(saveCodeInfo);
                    }
                }
                catch
                {
                    // 문제가 있는 파일은 건너뛰기
                    continue;
                }
            }

            return (allSaveCodes, validFiles);
        }

        /// <summary>
        /// 분석 결과를 처리합니다
        /// </summary>
        private async Task HandleAnalysisResultAsync((List<SaveCodeInfo> SaveCodes, int ValidFiles) result)
        {
            // 분석 완료 메시지
            if (result.SaveCodes.Count == 0)
            {
                WpfMessageBox.Show("유효한 세이브 코드 파일을 찾을 수 없습니다.\n파일에 '캐릭터:'와 'Code:' 부분이 포함되어 있는지 확인해주세요.", 
                                  "정보", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // 중복 캐릭터 분석 및 정보 표시
            var characterGroups = result.SaveCodes
                .Where(s => !string.IsNullOrWhiteSpace(s.CharacterName))
                .GroupBy(s => _nameMappingService.GetDisplayCharacterName(s.CharacterName).Trim())
                .ToList();

            System.Diagnostics.Debug.WriteLine($"=== 분석 결과 ===");
            System.Diagnostics.Debug.WriteLine($"총 세이브 코드: {result.SaveCodes.Count}개");
            System.Diagnostics.Debug.WriteLine($"고유 캐릭터: {characterGroups.Count}개");
            
            foreach (var group in characterGroups)
            {
                System.Diagnostics.Debug.WriteLine($"📋 '{group.Key}': {group.Count()}개 세이브 코드");
            }

            UpdateStatus($"분석 완료: {result.ValidFiles}개의 유효한 세이브 코드 파일을 찾았습니다. 데이터베이스에 저장 중...");

            // 데이터베이스에 자동 저장
            var saveSuccess = await SaveToDatabase(result.SaveCodes);
            
            if (saveSuccess)
            {
                // 저장 성공 후 데이터베이스에서 데이터 조회해서 표시
                UpdateStatus("데이터베이스에서 저장된 데이터를 조회하는 중...");
                await LoadCharactersFromDatabase();
                
                UpdateStatus($"분석 및 저장 완료: {result.ValidFiles}개의 세이브 코드가 데이터베이스에 저장되고 조회되었습니다.");
                
                WpfMessageBox.Show($"파일 분석 및 데이터베이스 저장 완료!\n\n분석된 파일: {result.ValidFiles}개\n고유 캐릭터: {characterGroups.Count}개\n데이터베이스에서 조회된 캐릭터: {_characters.Count}개\n\n분석된 세이브 코드가 데이터베이스에 저장되고 화면에 표시되었습니다.", 
                                  "분석 완료", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                WpfMessageBox.Show($"파일 분석은 완료되었지만 데이터베이스 저장에 실패했습니다.\n\n분석된 파일: {result.ValidFiles}개\n고유 캐릭터: {characterGroups.Count}개\n\n데이터베이스 설정을 확인하고 '조회' 버튼을 사용해서 기존 데이터를 확인해보세요.", 
                                  "저장 실패", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// 데이터베이스에서 캐릭터 데이터를 조회해서 화면에 표시합니다
        /// </summary>
        private async Task LoadCharactersFromDatabase()
        {
            try
            {
                _characters.Clear();
                
                // 데이터베이스에서 세이브 코드 조회
                var saveCodes = await _cacheService.LoadCharacterSaveCodesAsync();
                
                if (saveCodes.Count == 0)
                {
                    UpdateStatus("데이터베이스에 저장된 세이브 코드가 없습니다.");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"=== 데이터베이스에서 {saveCodes.Count}개 세이브 코드 조회 완료 ===");

                // 캐릭터명으로만 그룹화 (파일명과 무관하게)
                var characterDict = new Dictionary<string, List<SaveCodeInfo>>();
                
                foreach (var saveCode in saveCodes)
                {
                    if (string.IsNullOrWhiteSpace(saveCode.CharacterName))
                        continue;

                    // 캐릭터 이름 매핑 적용
                    var originalName = saveCode.CharacterName;
                    var mappedName = _nameMappingService.GetDisplayCharacterName(originalName);
                    var finalName = mappedName.Trim();
                    
                    System.Diagnostics.Debug.WriteLine($"🔍 원본명: '{originalName}' -> 최종명: '{finalName}'");
                    
                    if (!characterDict.ContainsKey(finalName))
                    {
                        characterDict[finalName] = new List<SaveCodeInfo>();
                    }
                    
                    characterDict[finalName].Add(saveCode);
                }

                System.Diagnostics.Debug.WriteLine($"=== 캐릭터별 그룹화 결과: {characterDict.Count}개 고유 캐릭터 ===");

                // 캐릭터 정보 객체 생성
                foreach (var kvp in characterDict.OrderBy(x => x.Key))
                {
                    var characterName = kvp.Key;
                    var characterSaveCodes = kvp.Value;
                    
                    // 세이브 코드를 현재 날짜 정렬 설정으로 정렬 (파일명 중복 제거 없이)
                    var sortedSaveCodes = SimpleSortService.SortSaveCodes(characterSaveCodes, _simpleSortSettings);
                    
                    System.Diagnostics.Debug.WriteLine($"📋 캐릭터: {characterName} - {characterSaveCodes.Count}개 세이브 코드");
                    foreach (var save in characterSaveCodes.Take(3)) // 처음 3개만 로그 출력
                    {
                        System.Diagnostics.Debug.WriteLine($"   📅 {save.FileDate:yyyy-MM-dd HH:mm:ss} - {save.FileName}");
                    }
                    if (characterSaveCodes.Count > 3)
                    {
                        System.Diagnostics.Debug.WriteLine($"   ... 및 {characterSaveCodes.Count - 3}개 더");
                    }
                    
                    var characterInfo = new CharacterInfo
                    {
                        CharacterName = characterName,
                        OriginalCharacterName = characterName,
                        SaveCodes = new ObservableCollection<SaveCodeInfo>(sortedSaveCodes),
                        SaveCodeCount = $"세이브 코드: {characterSaveCodes.Count}개",
                        LastModified = characterSaveCodes.Max(x => x.FileDate).ToString("yyyy-MM-dd HH:mm")
                    };
                    
                    _characters.Add(characterInfo);
                }

                // UI 업데이트
                FilterCharacters();
                UpdateCharacterCountDisplay();
                UpdateDatabaseSaveButtonState();
                
                System.Diagnostics.Debug.WriteLine($"✅ 최종 결과: {_characters.Count}개 고유 캐릭터가 UI에 표시됨");
                
                // 각 캐릭터의 세이브 코드 수 요약
                foreach (var character in _characters)
                {
                    System.Diagnostics.Debug.WriteLine($"   🎮 {character.CharacterName}: {character.SaveCodes.Count}개 세이브");
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"데이터베이스 조회 중 오류: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"LoadCharactersFromDatabase 오류: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"스택 트레이스: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// 데이터베이스에 세이브 코드를 저장합니다
        /// </summary>
        private async Task<bool> SaveToDatabase(List<SaveCodeInfo> saveCodes)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"=== MainWindow: 데이터베이스 저장 시작 ===");
                System.Diagnostics.Debug.WriteLine($"저장할 세이브 코드 수: {saveCodes.Count}");

                // 데이터베이스 연결 확인
                var connectionTest = await _cacheService.TestDatabaseConnectionAsync();
                if (!connectionTest)
                {
                    UpdateStatus("데이터베이스 연결 실패");
                    return false;
                }

                var fileHashes = new Dictionary<string, DateTime>();
                
                foreach (var file in _txtFiles)
                {
                    fileHashes[file.FileName] = File.GetLastWriteTime(file.FilePath);
                }

                var cache = new AnalysisCache
                {
                    FolderPath = _selectedFolderPath ?? "",
                    LastAnalyzed = DateTime.Now,
                    FileHashes = fileHashes,
                    SaveCodes = saveCodes,
                    TotalFiles = _txtFiles.Count,
                    Version = "1.0.0"
                };

                // 저장 시작 시간 기록
                var startTime = DateTime.Now;

                System.Diagnostics.Debug.WriteLine("CacheService.SaveCacheAsync 호출 전");
                var success = await _cacheService.SaveCacheAsync(cache);
                System.Diagnostics.Debug.WriteLine($"CacheService.SaveCacheAsync 결과: {success}");
                
                if (success)
                {
                    var elapsed = DateTime.Now - startTime;
                    System.Diagnostics.Debug.WriteLine($"데이터베이스 저장 완료: {saveCodes.Count}개 저장, 소요시간 {elapsed.TotalSeconds:F1}초");
                    
                    // 데이터베이스 정보 업데이트
                    await UpdateCacheInfoAsync();
                    return true;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("데이터베이스 저장 실패 - 프로시저 실행 오류");
                    return false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"=== MainWindow: 저장 중 예외 발생 ===");
                System.Diagnostics.Debug.WriteLine($"예외 타입: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"메시지: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"스택 트레이스: {ex.StackTrace}");
                
                UpdateStatus($"데이터베이스 저장 중 오류: {ex.Message}");
                return false;
            }
        }
        #endregion

        #region Cache Operations
        /// <summary>
        /// 데이터베이스에서 데이터를 로드합니다
        /// </summary>
        private async Task LoadFromCacheAsync(AnalysisCache cache)
        {
            try
            {
                _characters.Clear();
                var characterDict = cache.SaveCodes
                    .GroupBy(s => s.CharacterName)
                    .ToDictionary(g => g.Key, g => g.ToList());

                foreach (var kvp in characterDict.OrderBy(x => x.Key))
                {
                    // 세이브 코드를 현재 날짜 정렬 설정으로 정렬
                    var sortedSaveCodes = SimpleSortService.SortSaveCodes(kvp.Value, _simpleSortSettings);
                    
                    var characterInfo = new CharacterInfo
                    {
                        CharacterName = kvp.Key,
                        OriginalCharacterName = kvp.Key,
                        SaveCodes = new ObservableCollection<SaveCodeInfo>(sortedSaveCodes),
                        SaveCodeCount = $"세이브 코드: {kvp.Value.Count}개",
                        LastModified = kvp.Value.Max(x => x.FileDate).ToString("yyyy-MM-dd HH:mm")
                    };
                    _characters.Add(characterInfo);
                }

                FilterCharacters();
                UpdateCharacterCountDisplay();
                UpdateDatabaseSaveButtonState(); // 버튼 상태 업데이트 추가
                
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                UpdateStatus($"데이터베이스 로드 중 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 데이터베이스 정보를 업데이트합니다
        /// </summary>
        private async Task UpdateCacheInfoAsync()
        {
            TxtCacheInfo.Text = await _cacheService.GetCacheInfoAsync();
        }
        #endregion

        #region Search Operations
        /// <summary>
        /// 검색 텍스트 변경 이벤트
        /// </summary>
        private void TxtCharacterSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is System.Windows.Controls.TextBox searchBox)
            {
                _currentSearchText = searchBox.Text.Trim();
                FilterCharacters();
            }
        }

        /// <summary>
        /// 검색어 지우기 버튼 클릭
        /// </summary>
        private void BtnClearSearch_Click(object sender, RoutedEventArgs e)
        {
            TxtCharacterSearch.Text = string.Empty;
            _currentSearchText = string.Empty;
            FilterCharacters();
            TxtCharacterSearch.Focus();
        }

        /// <summary>
        /// 캐릭터를 필터링합니다
        /// </summary>
        private void FilterCharacters()
        {
            try
            {
                _filteredCharacters.Clear();

                if (string.IsNullOrEmpty(_currentSearchText))
                {
                    foreach (var character in _characters)
                    {
                        _filteredCharacters.Add(character);
                    }
                    
                    SearchResultPanel.Visibility = Visibility.Collapsed;
                }
                else
                {
                    var filteredList = _characters.Where(c => 
                        c.CharacterName.Contains(_currentSearchText, StringComparison.OrdinalIgnoreCase) ||
                        c.OriginalCharacterName.Contains(_currentSearchText, StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    foreach (var character in filteredList)
                    {
                        _filteredCharacters.Add(character);
                    }

                    ShowCharacterSearchResult(filteredList.Count);
                }

                LstCharacters.SelectedItem = null;
                ClearCharacterSelection();
                UpdateCharacterCountDisplay();
                
                UpdateStatus($"캐릭터 필터링 완료: {_filteredCharacters.Count}개 표시");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FilterCharacters 오류: {ex.Message}");
                UpdateStatus($"필터링 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 캐릭터 검색 결과를 표시합니다
        /// </summary>
        private void ShowCharacterSearchResult(int resultCount)
        {
            try
            {
                SearchResultPanel.Visibility = Visibility.Visible;
                
                if (resultCount == 0)
                {
                    TxtSearchResult.Text = "검색 결과 없음";
                    TxtSearchResult.Foreground = MediaBrushes.Orange;
                    TxtSearchCount.Text = $"'{_currentSearchText}'에 해당하는 캐릭터가 없습니다";
                }
                else
                {
                    TxtSearchResult.Text = "검색 결과";
                    TxtSearchResult.Foreground = MediaBrushes.DodgerBlue;
                    TxtSearchCount.Text = $"'{_currentSearchText}' - {resultCount}개 캐릭터 발견";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ShowCharacterSearchResult 오류: {ex.Message}");
            }
        }
        #endregion

        #region UI Event Handlers
        /// <summary>
        /// 날짜 정렬 방향을 토글합니다 (최신순 ⇄ 오래된순)
        /// </summary>
        private async void ToggleDateSort_Click(object sender, RoutedEventArgs e)
        {
            _simpleSortSettings.ToggleDirection();
            
            // 현재 선택된 캐릭터의 세이브 코드 다시 정렬
            if (LstCharacters.SelectedItem is CharacterInfo selectedCharacter)
            {
                ApplyDateSortToCurrentSaveCodes(selectedCharacter.SaveCodes);
            }
            
            // 설정 저장
            await SaveSimpleSortSettingsAsync();
            
            UpdateStatus($"정렬 방향 변경: {_simpleSortSettings.GetDisplayName()}");
        }

        /// <summary>
        /// 캐릭터 선택 변경 이벤트
        /// </summary>
        private void LstCharacters_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LstCharacters.SelectedItem is CharacterInfo selectedCharacter)
            {
                TxtSelectedCharacter.Text = selectedCharacter.CharacterName;
                TxtSelectedCharacter.Foreground = MediaBrushes.Black;
                TxtSelectedInfo.Text = $"{selectedCharacter.SaveCodeCount} | 최근 수정: {selectedCharacter.LastModified}";
                TxtSelectedInfo.Foreground = MediaBrushes.DarkBlue;

                // 날짜 정렬 적용하여 세이브 코드 표시
                ApplyDateSortToCurrentSaveCodes(selectedCharacter.SaveCodes);

                UpdateStatus($"'{selectedCharacter.CharacterName}' 캐릭터의 세이브 코드 {selectedCharacter.SaveCodes.Count}개를 표시했습니다 (정렬: {_simpleSortSettings.GetDisplayName()})");
            }
            else
            {
                ClearCharacterSelection();
            }
        }

        /// <summary>
        /// 현재 선택된 캐릭터의 세이브 코드에 날짜 정렬을 적용합니다
        /// </summary>
        private void ApplyDateSortToCurrentSaveCodes(IEnumerable<SaveCodeInfo> saveCodes)
        {
            _currentSaveCodes.Clear();
            var sortedCodes = SimpleSortService.SortSaveCodes(saveCodes, _simpleSortSettings);
            
            foreach (var saveCode in sortedCodes)
            {
                _currentSaveCodes.Add(saveCode);
            }
            
            // 현재 정렬 상태를 UI에 업데이트
            UpdateDateSortDisplayUI();
        }

        /// <summary>
        /// 현재 날짜 정렬 상태를 UI에 표시합니다
        /// </summary>
        private void UpdateDateSortDisplayUI()
        {
            try
            {
                var currentSortText = FindName("TxtCurrentDateSort") as TextBlock;
                if (currentSortText != null)
                {
                    currentSortText.Text = _simpleSortSettings.GetDisplayName();
                }
            }
            catch
            {
                // UI 컨트롤을 찾지 못한 경우 무시
            }
        }

        /// <summary>
        /// 캐릭터 개수 표시를 업데이트합니다
        /// </summary>
        private void UpdateCharacterCountDisplay()
        {
            try
            {
                var totalCount = _characters.Count;
                var displayedCount = _filteredCharacters.Count;
                
                if (string.IsNullOrEmpty(_currentSearchText))
                {
                    TxtCharacterCount.Text = $"분석된 캐릭터: {totalCount}개";
                }
                else
                {
                    TxtCharacterCount.Text = $"표시된 캐릭터: {displayedCount}개 / 전체: {totalCount}개";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateCharacterCountDisplay 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 간단한 정렬 설정을 저장합니다
        /// </summary>
        private async Task SaveSimpleSortSettingsAsync()
        {
            try
            {
                var settings = await _settingsService.LoadSettingsAsync();
                if (settings.RememberSortOption)
                {
                    settings.SimpleSortSettings = _simpleSortSettings;
                    await _settingsService.SaveSettingsAsync(settings);
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"정렬 설정 저장 실패: {ex.Message}");
            }
        }
        #endregion

        #region UI Helper Methods
        /// <summary>
        /// 로딩 상태를 표시합니다
        /// </summary>
        private void ShowLoadingState(bool isLoading)
        {
            StatusBarProgress.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;
            BtnChangeFolder.IsEnabled = !isLoading;
            BtnReloadFolder.IsEnabled = !isLoading;
            BtnAnalyzeFiles.IsEnabled = !isLoading && _txtFiles.Count > 0;
            
            // 데이터베이스 저장 버튼은 로딩 중이 아니고 분석된 캐릭터가 있을 때만 활성화
            var saveToDbButton = FindName("BtnSaveToDatabase") as System.Windows.Controls.Button;
            if (saveToDbButton != null)
            {
                saveToDbButton.IsEnabled = !isLoading && _characters.Count > 0;
            }
        }

        /// <summary>
        /// 상태를 업데이트합니다
        /// </summary>
        private void UpdateStatus(string message)
        {
            try
            {
                if (StatusBarMessage != null)
                {
                    StatusBarMessage.Content = message;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"StatusBar 업데이트 (UI 없음): {message}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"StatusBar 업데이트 오류: {ex.Message} | 메시지: {message}");
            }
        }

        /// <summary>
        /// 현재 정렬 상태를 포함한 상태를 업데이트합니다
        /// </summary>
        private void UpdateStatusWithSort(string baseMessage)
        {
            var sortInfo = _simpleSortSettings.GetDisplayName();
            StatusBarMessage.Content = $"{baseMessage} | 정렬: {sortInfo}";
        }

        /// <summary>
        /// 오류를 표시합니다
        /// </summary>
        private void ShowError(string message)
        {
            UpdateStatus($"오류: {message}");
            WpfMessageBox.Show(message, "오류", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        /// <summary>
        /// 모든 데이터를 지웁니다
        /// </summary>
        private void ClearAll()
        {
            _selectedFolderPath = null;
            TxtFolderPath.Text = "폴더를 선택해주세요...";
            TxtFolderPath.Foreground = MediaBrushes.Gray;
            
            var folderInfoTextBlock = (TextBlock)TagFolderInfo.Child;
            folderInfoTextBlock.Text = "폴더를 선택해주세요";
            TagFileCount.Visibility = Visibility.Collapsed;
            
            ClearData();
            BtnAnalyzeFiles.IsEnabled = false;
            
            ClearDisplay();
            UpdateStatus("준비됨");
        }

        /// <summary>
        /// 데이터를 정리합니다
        /// </summary>
        private void ClearData()
        {
            _txtFiles.Clear();
            _characters.Clear();
            _filteredCharacters.Clear();
            _currentSaveCodes.Clear();
            
            // 데이터베이스 저장 버튼 상태 업데이트
            UpdateDatabaseSaveButtonState();
        }

        /// <summary>
        /// 데이터베이스 저장 버튼 상태를 업데이트합니다
        /// </summary>
        private void UpdateDatabaseSaveButtonState()
        {
            var saveToDbButton = FindName("BtnSaveToDatabase") as System.Windows.Controls.Button;
            if (saveToDbButton != null)
            {
                saveToDbButton.IsEnabled = _characters.Count > 0;
            }
        }

        /// <summary>
        /// 화면 표시를 정리합니다
        /// </summary>
        private void ClearDisplay()
        {
            ClearCharacterSelection();
            _currentSearchText = string.Empty;
            TxtCharacterSearch.Text = string.Empty;
            SearchResultPanel.Visibility = Visibility.Collapsed;
            UpdateCharacterCountDisplay();
        }

        /// <summary>
        /// 캐릭터 선택을 정리합니다
        /// </summary>
        private void ClearCharacterSelection()
        {
            TxtSelectedCharacter.Text = "캐릭터를 선택해주세요";
            TxtSelectedInfo.Text = "캐릭터를 선택하면 세이브 코드가 표시됩니다";
            TxtSelectedCharacter.Foreground = MediaBrushes.Gray;
            TxtSelectedInfo.Foreground = MediaBrushes.Gray;
            TxtSelectedCode.Text = "세이브 코드를 선택하면 여기에 표시됩니다...";
            TxtSelectedCode.Foreground = MediaBrushes.Gray;
            _currentSaveCodes.Clear();
        }
        #endregion

        #region Copy Operations
        /// <summary>
        /// 코드 복사 버튼 클릭
        /// </summary>
        private async void BtnCopyCode_Click(object sender, RoutedEventArgs e)
        {
            if (LstSaveCodes.SelectedItem is SaveCodeInfo selectedCode)
            {
                UpdateStatus("클립보드에 복사 중...");
                
                try
                {
                    var success = await ClipboardHelper.CopyToClipboardAsync(selectedCode.SaveCode, UpdateStatus);
                    
                    if (success)
                    {
                        var displayName = _nameMappingService.GetDisplayCharacterName(selectedCode.CharacterName);
                        WpfMessageBox.Show($"'{displayName}'의 세이브 코드가 클립보드에 복사되었습니다!", "복사 완료", 
                                          MessageBoxButton.OK, MessageBoxImage.Information);
                        UpdateStatus($"세이브 코드 복사 완료: {selectedCode.FileName}");
                    }
                    else
                    {
                        WpfMessageBox.Show($"클립보드 복사에 실패했습니다.\n\n세이브 코드: {selectedCode.SaveCode}", 
                                          "클립보드 오류", 
                                          MessageBoxButton.OK, 
                                          MessageBoxImage.Warning);
                    }
                }
                catch (Exception ex)
                {
                    UpdateStatus($"복사 오류: {ex.Message}");
                    WpfMessageBox.Show($"복사 중 오류가 발생했습니다: {ex.Message}\n\n세이브 코드: {selectedCode.SaveCode}", 
                                      "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                WpfMessageBox.Show("복사할 세이브 코드를 선택해주세요.", "경고", 
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// 세이브 코드 선택 변경 이벤트
        /// </summary>
        private void LstSaveCodes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LstSaveCodes.SelectedItem is SaveCodeInfo selectedCode)
            {
                ShowSaveCodeDetails(selectedCode);
            }
            else
            {
                TxtSelectedCode.Text = "세이브 코드를 선택하면 여기에 상세 정보가 표시됩니다...";
                TxtSelectedCode.Foreground = MediaBrushes.Gray;
            }
        }

        /// <summary>
        /// 세이브 코드 상세 정보를 표시합니다
        /// </summary>
        private void ShowSaveCodeDetails(SaveCodeInfo selectedCode)
        {
            var displayName = _nameMappingService.GetDisplayCharacterName(selectedCode.CharacterName);
            
            var detailText = new StringBuilder();
            
            // 제목 정보 (더 크고 명확하게)
            detailText.AppendLine("═══════════════════════════════════");
            detailText.AppendLine($"📋 {selectedCode.FileName}");
            detailText.AppendLine("═══════════════════════════════════");
            detailText.AppendLine();
            
            // 헤더 정보
            detailText.AppendLine($"📅 날짜: {selectedCode.FileDate:yyyy-MM-dd HH:mm:ss}");
            detailText.AppendLine($"👤 캐릭터: {displayName}");
            detailText.AppendLine();
            
            // 세이브 코드 (강調)
            detailText.AppendLine("📋 세이브 코드:");
            detailText.AppendLine($"┌─────────────────────────────────────┐");
            detailText.AppendLine($"│ {selectedCode.SaveCode.PadRight(35)} │");
            detailText.AppendLine($"└─────────────────────────────────────┘");
            detailText.AppendLine();
            
            // 캐릭터 기본 정보
            detailText.AppendLine("📊 캐릭터 기본 정보:");
            detailText.AppendLine($"  • 레벨: {selectedCode.Level}");
            detailText.AppendLine($"  • 경험치: {selectedCode.Experience}");
            detailText.AppendLine();
            
            // 자원 정보
            detailText.AppendLine("💰 자원 정보:");
            detailText.AppendLine($"  • 금: {selectedCode.Gold}");
            detailText.AppendLine($"  • 나무: {selectedCode.Wood}");
            detailText.AppendLine();
            
            // 전투 능력치
            detailText.AppendLine("⚔️ 전투 능력치:");
            detailText.AppendLine($"  • 무력: {selectedCode.PhysicalPower}");
            detailText.AppendLine($"  • 요력: {selectedCode.MagicalPower}");
            detailText.AppendLine($"  • 영력: {selectedCode.SpiritualPower}");
            detailText.AppendLine();
            
            // 장착 아이템 (개선된 표시)
            detailText.AppendLine("🎒 장착 아이템:");
            if (selectedCode.Items.Count > 0)
            {
                for (int i = 0; i < selectedCode.Items.Count; i++)
                {
                    detailText.AppendLine($"  {i + 1:D2}. {selectedCode.Items[i]}");
                }
                detailText.AppendLine($"  → 총 {selectedCode.Items.Count}개 아이템 장착중");
            }
            else
            {
                detailText.AppendLine("  • 장착된 아이템이 없습니다");
            }

            TxtSelectedCode.Text = detailText.ToString();
            TxtSelectedCode.Foreground = MediaBrushes.Black;
            UpdateStatus($"세이브 코드 '{selectedCode.FileName}' 상세 정보 표시 (레 vel {selectedCode.Level}, 무력 {selectedCode.PhysicalPower})");
        }

        /// <summary>
        /// 전체 정보 복사 버튼 클릭
        /// </summary>
        private async void BtnCopyFullInfo_Click(object sender, RoutedEventArgs e)
        {
            if (LstSaveCodes.SelectedItem is SaveCodeInfo selectedCode)
            {
                UpdateStatus("전체 정보를 클립보드에 복사 중...");
                
                try
                {
                    var fullInfo = GenerateFullSaveCodeInfo(selectedCode);
                    var success = await ClipboardHelper.CopyToClipboardAsync(fullInfo, UpdateStatus);
                    
                    if (success)
                    {
                        var displayName = _nameMappingService.GetDisplayCharacterName(selectedCode.CharacterName);
                        WpfMessageBox.Show($"'{displayName}'의 전체 세이브 코드 정보가 클립보드에 복사되었습니다!", "복사 완료", 
                                          MessageBoxButton.OK, MessageBoxImage.Information);
                        UpdateStatus($"전체 정보 복사 완료: {selectedCode.FileName}");
                    }
                    else
                    {
                        WpfMessageBox.Show("클립보드 복사에 실패했습니다.", "오류", 
                                          MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    UpdateStatus($"복사 오류: {ex.Message}");
                    WpfMessageBox.Show($"복사 중 오류가 발생했습니다: {ex.Message}", "오류", 
                                      MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                WpfMessageBox.Show("먼저 세이브 코드를 선택해주세요.", "알림", 
                                  MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        /// <summary>
        /// 전체 세이브 코드 정보를 생성합니다
        /// </summary>
        private string GenerateFullSaveCodeInfo(SaveCodeInfo saveCode)
        {
            var sb = new StringBuilder();
            var displayName = _nameMappingService.GetDisplayCharacterName(saveCode.CharacterName);
            
            sb.AppendLine("=== 세이브 코드 상세 정보 ===");
            sb.AppendLine($"캐릭터: {displayName}");
            sb.AppendLine($"파일명: {saveCode.FileName}");
            sb.AppendLine($"날짜: {saveCode.FileDate:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine();
            
            sb.AppendLine("📋 세이브 코드:");
            sb.AppendLine(saveCode.SaveCode);
            sb.AppendLine();
            
            sb.AppendLine("📊 캐릭터 기본 정보:");
            sb.AppendLine($"• 레벨: {saveCode.Level}");
            sb.AppendLine($"• 경험치: {saveCode.Experience}");
            sb.AppendLine();
            
            sb.AppendLine("💰 자원 정보:");
            sb.AppendLine($"• 금: {saveCode.Gold}");
            sb.AppendLine($"• 나무: {saveCode.Wood}");
            sb.AppendLine();
            
            sb.AppendLine("⚔️ 전투 능력치:");
            sb.AppendLine($"• 무력: {saveCode.PhysicalPower}");
            sb.AppendLine($"• 요력: {saveCode.MagicalPower}");
            sb.AppendLine($"• 영력: {saveCode.SpiritualPower}");
            sb.AppendLine();
            
            if (saveCode.Items.Count > 0)
            {
                sb.AppendLine("🎒 장착 아이템:");
                foreach (var item in saveCode.Items)
                {
                    sb.AppendLine($"• {item}");
                }
            }
            else
            {
                sb.AppendLine("🎒 장착 아이템: 정보 없음");
            }
            
            sb.AppendLine();
            sb.AppendLine("=== Masin 세이브코드 분류 도구에서 생성 ===");
            
            return sb.ToString();
        }

        /// <summary>
        /// 데이터베이스 조회 버튼 클릭 (기존 초기화 버튼)
        /// </summary>
        private async void BtnClearCharacters_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ShowLoadingState(true);
                UpdateStatus("데이터베이스에서 저장된 세이브 코드를 조회하는 중...");
                
                await LoadCharactersFromDatabase();
                
                if (_characters.Count > 0)
                {
                    UpdateStatus($"데이터베이스 조회 완료: {_characters.Count}개 캐릭터를 찾았습니다.");
                    WpfMessageBox.Show($"데이터베이스 조회 완료!\n\n조회된 캐릭터: {_characters.Count}개\n\n저장된 모든 세이브 코드가 화면에 표시됩니다.", 
                                      "조회 완료", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    UpdateStatus("데이터베이스에 저장된 세이브 코드가 없습니다.");
                    WpfMessageBox.Show("데이터베이스에 저장된 세이브 코드가 없습니다.\n\n먼저 파일을 분석하여 세이브 코드를 저장해주세요.", 
                                      "조회 결과 없음", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                ShowError($"데이터베이스 조회 중 오류가 발생했습니다: {ex.Message}");
            }
            finally
            {
                ShowLoadingState(false);
            }
        }

        /// <summary>
        /// 현재 분석된 결과를 데이터베이스에 저장하는 버튼 클릭
        /// </summary>
        private async void BtnSaveToDatabase_Click(object sender, RoutedEventArgs e)
        {
            if (_characters.Count == 0)
            {
                WpfMessageBox.Show("저장할 분석 결과가 없습니다.\n먼저 파일을 분석해주세요.", "알림", 
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = WpfMessageBox.Show(
                $"현재 화면에 표시된 {_characters.Count}개 캐릭터의 세이브 코드를 데이터베이스에 저장하시겠습니까?\n\n기존 데이터가 있다면 새로운 데이터로 교체됩니다.",
                "데이터베이스 저장 확인",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    ShowLoadingState(true);
                    UpdateStatus("데이터베이스에 저장하는 중...");

                    // 현재 분석된 모든 세이브 코드를 수집
                    var allSaveCodes = new List<SaveCodeInfo>();
                    foreach (var character in _characters)
                    {
                        allSaveCodes.AddRange(character.SaveCodes);
                    }

                    var saveSuccess = await SaveToDatabase(allSaveCodes);
                    
                    if (saveSuccess)
                    {
                        UpdateStatus($"데이터베이스 저장 완료: {allSaveCodes.Count}개의 세이브 코드가 저장되었습니다.");
                        WpfMessageBox.Show($"데이터베이스 저장 완료!\n\n저장된 항목: {allSaveCodes.Count}개\n캐릭터 수: {_characters.Count}개\n\n다음번 실행 시 더 빠르게 로드됩니다.", 
                                          "저장 완료", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        UpdateStatus("데이터베이스 저장 실패");
                        WpfMessageBox.Show("데이터베이스 저장에 실패했습니다.\n\n상세한 오류 정보는 Visual Studio의 출력 창(디버그)에서 확인할 수 있습니다.", 
                                          "저장 실패", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                catch (Exception ex)
                {
                    ShowError($"데이터베이스 저장 중 오류가 발생했습니다: {ex.Message}");
                }
                finally
                {
                    ShowLoadingState(false);
                }
            }
        }

        /// <summary>
        /// 토큰 생성 탭의 표시 여부를 설정합니다
        /// </summary>
        private void SetTokenGeneratorTabVisibility(bool isVisible)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"토큰 탭 표시 여부 설정 시작: {isVisible}");
                
                // MainWindow.xaml에서 MainTabControl을 찾기
                var tabControl = FindName("MainTabControl") as System.Windows.Controls.TabControl;

                if (tabControl != null)
                {
                    System.Diagnostics.Debug.WriteLine($"MainTabControl을 찾음, 탭 개수: {tabControl.Items.Count}");
                
                    if (tabControl.Items.Count > 1)
                    {
                        var tokenTab = tabControl.Items[1] as TabItem; // 두 번째 탭이 토큰 생성 탭
                        
                        if (tokenTab != null)
                        {
                            if (isVisible)
                            {
                                tokenTab.Visibility = Visibility.Visible;
                                System.Diagnostics.Debug.WriteLine("토큰 생성 탭 표시됨 (관리자 권한)");
                            }
                            else
                            {
                                tokenTab.Visibility = Visibility.Collapsed;
                                

                                // 현재 토큰 탭이 선택되어 있다면 첫 번째 탭으로 이동
                                if (tabControl.SelectedItem == tokenTab)
                                {
                                    tabControl.SelectedIndex = 0;
                                    System.Diagnostics.Debug.WriteLine("토큰 탭이 선택되어 있어서 첫 번째 탭으로 이동");
                                }
                                
                                System.Diagnostics.Debug.WriteLine("토큰 생성 탭 숨김 (일반 사용자)");
                            }
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("경고: 두 번째 탭이 TabItem이 아님");
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"경고: TabControl에 탭이 부족함 (현재: {tabControl.Items.Count}개)");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("경고: MainTabControl을 찾을 수 없음 - 계속 진행");
                    // 탭 컨트롤을 찾지 못해도 메인창 로딩에는 문제없도록 함
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"토큰 탭 제어 오류: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"스택 트레이스: {ex.StackTrace}");
                
                // 토큰 탭 제어 실패는 치명적이지 않으므로 계속 진행
                UpdateStatus($"토큰 탭 설정 오류: {ex.Message}");
            }
        }
        #endregion
    }
}