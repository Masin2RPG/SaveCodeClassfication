using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
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
        private readonly CacheService _cacheService;
        private readonly CharacterNameMappingService _nameMappingService;
        private readonly SaveCodeParserService _parserService;
        #endregion

        #region Constructor
        public MainWindow()
        {
            InitializeComponent();
            
            // Services 초기화
            _settingsService = new SettingsService(PathConstants.ConfigFilePath);
            _cacheService = new CacheService(PathConstants.CacheFilePath);
            _nameMappingService = new CharacterNameMappingService(PathConstants.CharNameMappingPath);
            _parserService = new SaveCodeParserService(_nameMappingService);
            
            // UI 초기화
            LstCharacters.ItemsSource = _filteredCharacters;
            LstSaveCodes.ItemsSource = _currentSaveCodes;
            
            // 비동기 초기화
            _ = Task.Run(async () =>
            {
                await Dispatcher.InvokeAsync(async () =>
                {
                    await InitializeApplicationAsync();
                });
            });
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
                // 설정 로드 및 정렬 옵션 설정
                var settings = await _settingsService.LoadSettingsAsync();
                _simpleSortSettings = settings.SimpleSortSettings;
                
                // 정렬 UI 업데이트
                UpdateDateSortDisplayUI();
                
                // 캐릭터 이름 매핑 로드
                var mappingLoaded = await _nameMappingService.LoadMappingsAsync();
                if (mappingLoaded)
                {
                    UpdateStatus($"캐릭터 이름 매핑 로드 완료: {_nameMappingService.GetMappingCount()}개 매핑");
                }
                else
                {
                    UpdateStatus("charName.json 파일이 없습니다. 원래 캐릭터명을 사용합니다.");
                }

                // 캐시 정보 업데이트
                await UpdateCacheInfoAsync();
                
                if (!string.IsNullOrEmpty(settings.LastSelectedFolderPath) && 
                    Directory.Exists(settings.LastSelectedFolderPath))
                {
                    UpdateStatus("저장된 폴더 경로를 로드하는 중...");
                    await LoadFolderAsync(settings.LastSelectedFolderPath, autoLoad: true);
                }
                else
                {
                    UpdateStatus("저장된 폴더 경로가 없습니다. 폴더를 선택해주세요.");
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"초기화 오류: {ex.Message}");
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
            UpdateStatus("캐시된 분석 결과를 확인하는 중...");
            var cache = await _cacheService.LoadCacheAsync();
            
            if (cache != null && _cacheService.IsCacheValid(cache, folderPath, _txtFiles))
            {
                UpdateStatus("캐시된 분석 결과를 로드하는 중...");
                await LoadFromCacheAsync(cache);
                
                var message = $"캐시에서 로드 완료: {txtFiles.Length}개 파일, {_characters.Count}개 캐릭터";
                UpdateStatus(message);
                
                if (!autoLoad)
                {
                    WpfMessageBox.Show($"캐시된 분석 결과를 로드했습니다!\n{_characters.Count}개의 캐릭터를 찾았습니다.\n\n최신 분석이 필요하면 '파일 분석' 버튼을 클릭하세요.", 
                                      "캐시 로드 완료", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                var message = autoLoad 
                    ? $"자동 로드 완료: {txtFiles.Length}개의 TXT 파일을 찾았습니다. 분석이 필요합니다."
                    : $"{txtFiles.Length}개의 TXT 파일을 찾았습니다. 분석 버튼을 클릭해주세요.";
                
                UpdateStatus(message);
                
                if (!autoLoad)
                {
                    WpfMessageBox.Show($"{txtFiles.Length}개의 TXT 파일을 찾았습니다.\n'파일 분석' 버튼을 클릭하여 캐릭터별로 분류하세요.", 
                                      "성공", MessageBoxButton.OK, MessageBoxImage.Information);
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
            var characterDict = result.SaveCodes
                .GroupBy(s => s.CharacterName)
                .ToDictionary(g => g.Key, g => g.ToList());

            // 캐릭터 정보 객체 생성
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

            // UI 업데이트
            FilterCharacters();
            UpdateCharacterCountDisplay();
            UpdateStatus($"분석 완료: {result.ValidFiles}개의 유효한 세이브 코드 파일에서 {_characters.Count}개의 캐릭터를 찾았습니다 (정렬: {_simpleSortSettings.GetDisplayName()})");

            // 캐시 저장
            if (result.SaveCodes.Count > 0 && !string.IsNullOrEmpty(_selectedFolderPath))
            {
                UpdateStatus("분석 결과를 캐시에 저장하는 중...");
                await SaveAnalysisResultsToCacheAsync(_selectedFolderPath, result.SaveCodes);
            }

            // 결과 메시지
            ShowAnalysisCompletionMessage(result.ValidFiles);
        }

        /// <summary>
        /// 분석 완료 메시지를 표시합니다
        /// </summary>
        private void ShowAnalysisCompletionMessage(int validFiles)
        {
            if (_characters.Count == 0)
            {
                WpfMessageBox.Show("유효한 세이브 코드 파일을 찾을 수 없습니다.\n파일에 '캐릭터:'와 'Code:' 부분이 포함되어 있는지 확인해주세요.", 
                                  "정보", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                WpfMessageBox.Show($"분석 완료!\n{_characters.Count}개의 캐릭터에서 총 {validFiles}개의 세이브 코드를 찾았습니다.\n\n결과가 캐시에 저장되어 다음번엔 더 빠르게 로드됩니다.", 
                                  "성공", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        #endregion

        #region Cache Operations
        /// <summary>
        /// 분석 결과를 캐시에 저장합니다
        /// </summary>
        private async Task SaveAnalysisResultsToCacheAsync(string folderPath, List<SaveCodeInfo> saveCodes)
        {
            try
            {
                var fileHashes = new Dictionary<string, DateTime>();
                
                foreach (var file in _txtFiles)
                {
                    fileHashes[file.FileName] = File.GetLastWriteTime(file.FilePath);
                }

                var cache = new AnalysisCache
                {
                    FolderPath = folderPath,
                    LastAnalyzed = DateTime.Now,
                    FileHashes = fileHashes,
                    SaveCodes = saveCodes,
                    TotalFiles = _txtFiles.Count,
                    Version = "1.0.0"
                };

                await _cacheService.SaveCacheAsync(cache);
            }
            catch (Exception ex)
            {
                UpdateStatus($"캐시 저장 중 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 캐시에서 데이터를 로드합니다
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
                
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                UpdateStatus($"캐시 로드 중 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 캐시 정보를 업데이트합니다
        /// </summary>
        private async Task UpdateCacheInfoAsync()
        {
            TxtCacheInfo.Text = await _cacheService.GetCacheInfoAsync();
        }

        /// <summary>
        /// 캐시 삭제 버튼 클릭
        /// </summary>
        private async void BtnClearCache_Click(object sender, RoutedEventArgs e)
        {
            var result = WpfMessageBox.Show(
                "캐시를 삭제하시겠습니까?\n\n삭제 후에는 다시 파일 분석을 해야 합니다.",
                "캐시 삭제 확인",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var deleted = _cacheService.DeleteCache();
                    if (deleted)
                    {
                        UpdateStatus("캐시가 삭제되었습니다.");
                        await UpdateCacheInfoAsync();
                        WpfMessageBox.Show("캐시가 성공적으로 삭제되었습니다.", "삭제 완료", 
                                          MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        WpfMessageBox.Show("삭제할 캐시 파일이 없습니다.", "정보", 
                                          MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    ShowError($"캐시 삭제 실패: {ex.Message}");
                }
            }
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
            _filteredCharacters.Clear();

            if (string.IsNullOrEmpty(_currentSearchText))
            {
                foreach (var character in _characters)
                {
                    _filteredCharacters.Add(character);
                }
                
                SearchResultPanel.Visibility = Visibility.Collapsed;
                UpdateCharacterCountDisplay();
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

                ShowSearchResult(filteredList.Count);
            }

            LstCharacters.SelectedItem = null;
            ClearCharacterSelection();
            
            UpdateStatus($"캐릭터 검색: '{_currentSearchText}' - {_filteredCharacters.Count}개 결과");
        }

        /// <summary>
        /// 검색 결과를 표시합니다
        /// </summary>
        private void ShowSearchResult(int resultCount)
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

        /// <summary>
        /// 캐릭터 개수 표시를 업데이트합니다
        /// </summary>
        private void UpdateCharacterCountDisplay()
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
        #endregion

        #region UI Event Handlers
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
        }

        /// <summary>
        /// 상태를 업데이트합니다
        /// </summary>
        private void UpdateStatus(string message)
        {
            StatusBarMessage.Content = message;
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
            
            // 세이브 코드 (강조)
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
            UpdateStatus($"세이브 코드 '{selectedCode.FileName}' 상세 정보 표시 (레벨 {selectedCode.Level}, 무력 {selectedCode.PhysicalPower})");
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
        /// 초기화 버튼 클릭
        /// </summary>
        private async void BtnClearCharacters_Click(object sender, RoutedEventArgs e)
        {
            if (_characters.Count > 0)
            {
                var result = WpfMessageBox.Show(
                    "정말로 모든 분석 결과를 지우시겠습니까?",
                    "확인",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    ClearAll();
                    WpfMessageBox.Show("모든 내용이 지워졌습니다.", "정보", 
                                      MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }
        #endregion
    }
}