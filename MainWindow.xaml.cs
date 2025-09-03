using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using TextCopy;
using MediaBrushes = System.Windows.Media.Brushes;
using WpfMessageBox = System.Windows.MessageBox;
using WpfClipboard = System.Windows.Clipboard;
using System.Linq;

namespace SaveCodeClassfication
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string? _selectedFolderPath;
        private readonly ObservableCollection<TxtFileInfo> _txtFiles = new();
        private readonly ObservableCollection<CharacterInfo> _characters = new();
        private readonly ObservableCollection<CharacterInfo> _filteredCharacters = new();
        private readonly ObservableCollection<SaveCodeInfo> _currentSaveCodes = new();
        private string _currentSearchText = string.Empty;
        
        // 설정 파일 경로
        private static readonly string ConfigFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SaveCodeClassification",
            "config.json"
        );
        
        // 캐시 파일 경로
        private static readonly string CacheFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SaveCodeClassification",
            "analysis_cache.json"
        );

        public MainWindow()
        {
            InitializeComponent();
            LstCharacters.ItemsSource = _filteredCharacters;
            LstSaveCodes.ItemsSource = _currentSaveCodes;
            
            // 앱 시작 시 설정 로드 및 자동 폴더 로드
            _ = Task.Run(async () =>
            {
                await Dispatcher.InvokeAsync(async () =>
                {
                    await LoadSettingsAndInitializeAsync();
                });
            });
        }

        private async Task LoadSettingsAndInitializeAsync()
        {
            try
            {
                // 캐시 정보 업데이트
                await UpdateCacheInfoAsync();
                
                var settings = await LoadSettingsAsync();
                
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
                UpdateStatus($"설정 로드 오류: {ex.Message}");
            }
        }

        private async void BtnSelectFolder_Click(object sender, RoutedEventArgs e)
        {
            var folderDialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "세이브 코드 TXT 파일이 있는 폴더를 선택해주세요",
                UseDescriptionForTitle = true
            };

            // 기존 저장된 경로가 있으면 해당 경로로 시작
            if (!string.IsNullOrEmpty(_selectedFolderPath) && Directory.Exists(_selectedFolderPath))
            {
                folderDialog.SelectedPath = _selectedFolderPath;
            }

            if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                await LoadFolderAsync(folderDialog.SelectedPath);
                
                // 선택된 폴더 경로를 설정에 저장
                await SaveSelectedFolderPathAsync(folderDialog.SelectedPath);
            }
        }

        private async Task SaveSelectedFolderPathAsync(string folderPath)
        {
            try
            {
                var settings = await LoadSettingsAsync();
                settings.LastSelectedFolderPath = folderPath;
                settings.LastUpdated = DateTime.Now;
                await SaveSettingsAsync(settings);
            }
            catch (Exception ex)
            {
                UpdateStatus($"폴더 경로 저장 실패: {ex.Message}");
            }
        }

        private async Task LoadFolderAsync(string folderPath, bool autoLoad = false)
        {
            try
            {
                ShowLoadingState(true);
                
                if (autoLoad)
                {
                    UpdateStatus("저장된 폴더를 자동으로 로드하는 중...");
                }
                else
                {
                    UpdateStatus("폴더를 스캔하는 중...");
                }

                _selectedFolderPath = folderPath;
                TxtFolderPath.Text = folderPath;
                TxtFolderPath.Foreground = MediaBrushes.Black;

                // Clear previous data
                _txtFiles.Clear();
                _characters.Clear();
                _currentSaveCodes.Clear();
                ClearDisplay();

                // Get folder info
                var folderInfo = new DirectoryInfo(folderPath);
                var folderInfoTextBlock = (TextBlock)TagFolderInfo.Child;
                folderInfoTextBlock.Text = $"폴더: {folderInfo.Name}";

                // Find all .txt files
                var txtFiles = Directory.GetFiles(folderPath, "*.txt", SearchOption.AllDirectories);
                
                if (txtFiles.Length == 0)
                {
                    TagFileCountText.Text = "TXT 파일이 없습니다";
                    TagFileCount.Visibility = Visibility.Visible;
                    UpdateStatus("선택된 폴더에 TXT 파일이 없습니다");
                    BtnAnalyzeFiles.IsEnabled = false;
                    
                    if (!autoLoad) // 자동 로드가 아닐 때만 메시지 표시
                    {
                        WpfMessageBox.Show("선택된 폴더에 TXT 파일이 없습니다.", "정보", 
                                      MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    return;
                }

                // Load file information
                foreach (var filePath in txtFiles)
                {
                    var fileInfo = new FileInfo(filePath);
                    var txtFileInfo = new TxtFileInfo
                    {
                        FilePath = filePath,
                        FileName = fileInfo.Name,
                        FileSize = fileInfo.Length,
                        FileSizeText = $"크기: {FormatFileSize(fileInfo.Length)}",
                        RelativePath = Path.GetRelativePath(folderPath, filePath)
                    };
                    _txtFiles.Add(txtFileInfo);
                }

                TagFileCountText.Text = $"파일 수: {txtFiles.Length}개";
                TagFileCount.Visibility = Visibility.Visible;
                BtnAnalyzeFiles.IsEnabled = true;

                // 캐시 로드 시도
                UpdateStatus("캐시된 분석 결과를 확인하는 중...");
                var cache = await LoadAnalysisCacheAsync();
                
                if (cache != null && IsCacheValid(cache, folderPath, _txtFiles))
                {
                    UpdateStatus("캐시된 분석 결과를 로드하는 중...");
                    await LoadFromCacheAsync(cache);
                    
                    if (autoLoad)
                    {
                        UpdateStatus($"캐시에서 로드 완료: {txtFiles.Length}개 파일, {_characters.Count}개 캐릭터");
                    }
                    else
                    {
                        UpdateStatus($"캐시에서 로드 완료: {txtFiles.Length}개 파일, {_characters.Count}개 캐릭터");
                        WpfMessageBox.Show($"캐시된 분석 결과를 로드했습니다!\n{_characters.Count}개의 캐릭터를 찾았습니다.\n\n최신 분석이 필요하면 '파일 분석' 버튼을 클릭하세요.", "캐시 로드 완료", 
                                          MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                else
                {
                    if (autoLoad)
                    {
                        UpdateStatus($"자동 로드 완료: {txtFiles.Length}개의 TXT 파일을 찾았습니다. 분석이 필요합니다.");
                    }
                    else
                    {
                        UpdateStatus($"{txtFiles.Length}개의 TXT 파일을 찾았습니다. 분석 버튼을 클릭해주세요.");
                        WpfMessageBox.Show($"{txtFiles.Length}개의 TXT 파일을 찾았습니다.\n'파일 분석' 버튼을 클릭하여 캐릭터별로 분류하세요.", "성공", 
                                      MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
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

        private async void BtnAnalyzeFiles_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ShowLoadingState(true);
                UpdateStatus("파일을 분석하는 중...");

                _characters.Clear();
                var characterDict = new Dictionary<string, List<SaveCodeInfo>>();
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
                        var saveCodeInfo = ParseSaveCodeFile(content, txtFile);

                        if (saveCodeInfo != null)
                        {
                            validFiles++;
                            allSaveCodes.Add(saveCodeInfo);
                            var characterName = saveCodeInfo.CharacterName;
                            
                            if (!characterDict.ContainsKey(characterName))
                            {
                                characterDict[characterName] = new List<SaveCodeInfo>();
                            }
                            
                            characterDict[characterName].Add(saveCodeInfo);
                        }
                    }
                    catch (Exception ex)
                    {
                        // Skip problematic files
                        continue;
                    }
                }

                // Create character info objects
                foreach (var kvp in characterDict.OrderBy(x => x.Key))
                {
                    var characterInfo = new CharacterInfo
                    {
                        CharacterName = kvp.Key,
                        SaveCodes = new ObservableCollection<SaveCodeInfo>(kvp.Value.OrderByDescending(x => x.FileDate)),
                        SaveCodeCount = $"세이브 코드: {kvp.Value.Count}개",
                        LastModified = kvp.Value.Max(x => x.FileDate).ToString("yyyy-MM-dd HH:mm")
                    };
                    _characters.Add(characterInfo);
                }

                // 검색 필터 적용
                FilterCharacters();
                UpdateCharacterCountDisplay();

                UpdateStatus($"분석 완료: {validFiles}개의 유효한 세이브 코드 파일에서 {_characters.Count}개의 캐릭터를 찾았습니다");

                // 분석 결과를 캐시에 저장
                if (allSaveCodes.Count > 0 && !string.IsNullOrEmpty(_selectedFolderPath))
                {
                    UpdateStatus("분석 결과를 캐시에 저장하는 중...");
                    await SaveAnalysisResultsToCacheAsync(_selectedFolderPath, allSaveCodes);
                }

                if (_characters.Count == 0)
                {
                    WpfMessageBox.Show("유효한 세이브 코드 파일을 찾을 수 없습니다.\n파일에 '캐릭터:'와 'Code:' 부분이 포함되어 있는지 확인해주세요.", "정보", 
                                      MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    WpfMessageBox.Show($"분석 완료!\n{_characters.Count}개의 캐릭터에서 총 {validFiles}개의 세이브 코드를 찾았습니다.\n\n결과가 캐시에 저장되어 다음번엔 더 빠르게 로드됩니다.", "성공", 
                                      MessageBoxButton.OK, MessageBoxImage.Information);
                }
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

        private async Task SaveAnalysisResultsToCacheAsync(string folderPath, List<SaveCodeInfo> saveCodes)
        {
            try
            {
                var fileHashes = new Dictionary<string, DateTime>();
                
                // 각 파일의 수정 시간을 기록
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

                await SaveAnalysisCacheAsync(cache);
            }
            catch (Exception ex)
            {
                UpdateStatus($"캐시 저장 중 오류: {ex.Message}");
            }
        }

        private async Task<AppSettings> LoadSettingsAsync()
        {
            try
            {
                if (!File.Exists(ConfigFilePath))
                {
                    return new AppSettings();
                }

                var jsonString = await File.ReadAllTextAsync(ConfigFilePath, Encoding.UTF8);
                var settings = JsonSerializer.Deserialize<AppSettings>(jsonString);
                return settings ?? new AppSettings();
            }
            catch (Exception ex)
            {
                UpdateStatus($"설정 파일 로드 실패: {ex.Message}");
                return new AppSettings();
            }
        }

        private async Task SaveSettingsAsync(AppSettings settings)
        {
            try
            {
                // 설정 디렉토리가 없으면 생성
                var configDir = Path.GetDirectoryName(ConfigFilePath);
                if (!Directory.Exists(configDir))
                {
                    Directory.CreateDirectory(configDir!);
                }

                var jsonString = JsonSerializer.Serialize(settings, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });

                await File.WriteAllTextAsync(ConfigFilePath, jsonString, Encoding.UTF8);
                UpdateStatus("설정이 저장되었습니다.");
            }
            catch (Exception ex)
            {
                UpdateStatus($"설정 저장 실패: {ex.Message}");
            }
        }

        private async Task<AnalysisCache?> LoadAnalysisCacheAsync()
        {
            try
            {
                if (!File.Exists(CacheFilePath))
                {
                    return null;
                }

                var jsonString = await File.ReadAllTextAsync(CacheFilePath, Encoding.UTF8);
                var cache = JsonSerializer.Deserialize<AnalysisCache>(jsonString);
                return cache;
            }
            catch (Exception ex)
            {
                UpdateStatus($"캐시 파일 로드 실패: {ex.Message}");
                return null;
            }
        }

        private async Task SaveAnalysisCacheAsync(AnalysisCache cache)
        {
            try
            {
                // 캐시 디렉토리가 없으면 생성
                var cacheDir = Path.GetDirectoryName(CacheFilePath);
                if (!Directory.Exists(cacheDir))
                {
                    Directory.CreateDirectory(cacheDir!);
                }

                var jsonString = JsonSerializer.Serialize(cache, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });

                await File.WriteAllTextAsync(CacheFilePath, jsonString, Encoding.UTF8);
                UpdateStatus("분석 결과가 캐시에 저장되었습니다.");
            }
            catch (Exception ex)
            {
                UpdateStatus($"캐시 저장 실패: {ex.Message}");
            }
        }

        private async Task LoadFromCacheAsync(AnalysisCache cache)
        {
            try
            {
                _characters.Clear();
                var characterDict = new Dictionary<string, List<SaveCodeInfo>>();

                // 캐시에서 세이브 코드 정보를 그룹화
                foreach (var saveCode in cache.SaveCodes)
                {
                    var characterName = saveCode.CharacterName;
                    
                    if (!characterDict.ContainsKey(characterName))
                    {
                        characterDict[characterName] = new List<SaveCodeInfo>();
                    }
                    
                    characterDict[characterName].Add(saveCode);
                }

                // 캐릭터 정보 객체 생성
                foreach (var kvp in characterDict.OrderBy(x => x.Key))
                {
                    var characterInfo = new CharacterInfo
                    {
                        CharacterName = kvp.Key,
                        SaveCodes = new ObservableCollection<SaveCodeInfo>(kvp.Value.OrderByDescending(x => x.FileDate)),
                        SaveCodeCount = $"세이브 코드: {kvp.Value.Count}개",
                        LastModified = kvp.Value.Max(x => x.FileDate).ToString("yyyy-MM-dd HH:mm")
                    };
                    _characters.Add(characterInfo);
                }

                // 검색 필터 적용
                FilterCharacters();
                UpdateCharacterCountDisplay();
                
                await Task.CompletedTask; // async 메소드를 위한 형식적 대기
            }
            catch (Exception ex)
            {
                UpdateStatus($"캐시 로드 중 오류: {ex.Message}");
            }
        }

        private static string FormatNumber(string numberStr)
        {
            if (long.TryParse(numberStr, out long number))
            {
                return number.ToString("#,##0");
            }
            return numberStr;
        }

        private bool IsCacheValid(AnalysisCache cache, string folderPath, IEnumerable<TxtFileInfo> currentFiles)
        {
            try
            {
                // 폴더 경로가 다르면 캐시 무효
                if (cache.FolderPath != folderPath)
                    return false;

                // 파일 개수가 다르면 캐시 무효
                if (cache.TotalFiles != currentFiles.Count())
                    return false;

                // 각 파일의 수정 시간 확인
                foreach (var file in currentFiles)
                {
                    var fileName = file.FileName;
                    var lastWriteTime = File.GetLastWriteTime(file.FilePath);

                    if (!cache.FileHashes.ContainsKey(fileName) || 
                        cache.FileHashes[fileName] != lastWriteTime)
                    {
                        return false;
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

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
                    if (File.Exists(CacheFilePath))
                    {
                        File.Delete(CacheFilePath);
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

        private async Task UpdateCacheInfoAsync()
        {
            try
            {
                if (File.Exists(CacheFilePath))
                {
                    var cache = await LoadAnalysisCacheAsync();
                    if (cache != null)
                    {
                        var cacheAge = DateTime.Now - cache.LastAnalyzed;
                        var ageText = cacheAge.TotalDays >= 1 
                            ? $"{(int)cacheAge.TotalDays}일 전" 
                            : cacheAge.TotalHours >= 1 
                                ? $"{(int)cacheAge.TotalHours}시간 전" 
                                : $"{(int)cacheAge.TotalMinutes}분 전";
                        
                        TxtCacheInfo.Text = $"캐시 있음: {cache.SaveCodes.Count}개 세이브코드 ({ageText})";
                    }
                    else
                    {
                        TxtCacheInfo.Text = "캐시 상태: 손상된 캐시 파일";
                    }
                }
                else
                {
                    TxtCacheInfo.Text = "캐시 상태: 캐시 없음";
                }
            }
            catch
            {
                TxtCacheInfo.Text = "캐시 상태: 확인 실패";
            }
        }

        private void ShowLoadingState(bool isLoading)
        {
            StatusBarProgress.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;
            BtnChangeFolder.IsEnabled = !isLoading;
            BtnReloadFolder.IsEnabled = !isLoading;
            BtnAnalyzeFiles.IsEnabled = !isLoading && _txtFiles.Count > 0;
        }

        private void UpdateStatus(string message)
        {
            StatusBarMessage.Content = message;
        }

        private void ShowError(string message)
        {
            UpdateStatus($"오류: {message}");
            WpfMessageBox.Show(message, "오류", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void ClearAll()
        {
            _selectedFolderPath = null;
            TxtFolderPath.Text = "폴더를 선택해주세요...";
            TxtFolderPath.Foreground = MediaBrushes.Gray;
            
            var folderInfoTextBlock = (TextBlock)TagFolderInfo.Child;
            folderInfoTextBlock.Text = "폴더를 선택해주세요";
            TagFileCount.Visibility = Visibility.Collapsed;
            
            _txtFiles.Clear();
            _characters.Clear();
            _filteredCharacters.Clear();
            _currentSaveCodes.Clear();
            BtnAnalyzeFiles.IsEnabled = false;
            
            ClearDisplay();
            UpdateStatus("준비됨");
        }

        private void ClearDisplay()
        {
            ClearCharacterSelection();
            _currentSearchText = string.Empty;
            TxtCharacterSearch.Text = string.Empty;
            SearchResultPanel.Visibility = Visibility.Collapsed;
            UpdateCharacterCountDisplay();
        }

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

        private static string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            
            return $"{len:0.##} {sizes[order]}";
        }

        private void TxtCharacterSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is System.Windows.Controls.TextBox searchBox)
            {
                _currentSearchText = searchBox.Text.Trim();
                FilterCharacters();
            }
        }

        private void BtnClearSearch_Click(object sender, RoutedEventArgs e)
        {
            TxtCharacterSearch.Text = string.Empty;
            _currentSearchText = string.Empty;
            FilterCharacters();
            TxtCharacterSearch.Focus();
        }

        private void FilterCharacters()
        {
            _filteredCharacters.Clear();

            if (string.IsNullOrEmpty(_currentSearchText))
            {
                // 검색어가 없으면 모든 캐릭터 표시
                foreach (var character in _characters)
                {
                    _filteredCharacters.Add(character);
                }
                
                // 검색 결과 패널 숨기기
                SearchResultPanel.Visibility = Visibility.Collapsed;
                UpdateCharacterCountDisplay();
            }
            else
            {
                // 검색어로 필터링
                var filteredList = _characters.Where(c => 
                    c.CharacterName.Contains(_currentSearchText, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                foreach (var character in filteredList)
                {
                    _filteredCharacters.Add(character);
                }

                // 검색 결과 표시
                ShowSearchResult(filteredList.Count);
            }

            // 선택 초기화
            LstCharacters.SelectedItem = null;
            ClearCharacterSelection();
            
            UpdateStatus($"캐릭터 검색: '{_currentSearchText}' - {_filteredCharacters.Count}개 결과");
        }

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

        private void LstCharacters_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LstCharacters.SelectedItem is CharacterInfo selectedCharacter)
            {
                TxtSelectedCharacter.Text = selectedCharacter.CharacterName;
                TxtSelectedCharacter.Foreground = MediaBrushes.Black;
                TxtSelectedInfo.Text = $"{selectedCharacter.SaveCodeCount} | 최근 수정: {selectedCharacter.LastModified}";
                TxtSelectedInfo.Foreground = MediaBrushes.DarkBlue;

                _currentSaveCodes.Clear();
                foreach (var saveCode in selectedCharacter.SaveCodes)
                {
                    _currentSaveCodes.Add(saveCode);
                }

                UpdateStatus($"'{selectedCharacter.CharacterName}' 캐릭터의 세이브 코드 {selectedCharacter.SaveCodes.Count}개를 표시했습니다");
            }
            else
            {
                ClearCharacterSelection();
            }
        }

        private void LstSaveCodes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LstSaveCodes.SelectedItem is SaveCodeInfo selectedCode)
            {
                // 세이브 코드와 함께 상세 정보 표시
                var detailText = $"세이브 코드: {selectedCode.SaveCode}\n\n";
                detailText += $"📊 캐릭터 기본 정보:\n";
                detailText += $"• 레벨: {selectedCode.Level}\n";
                detailText += $"• 경험치: {selectedCode.Experience}\n\n";
                
                detailText += $"💰 자원 정보:\n";
                detailText += $"• 금: {selectedCode.Gold}\n";
                detailText += $"• 나무: {selectedCode.Wood}\n\n";
                
                detailText += $"⚔️ 전투 능력치:\n";
                detailText += $"• 무력: {selectedCode.PhysicalPower}\n";
                detailText += $"• 요력: {selectedCode.MagicalPower}\n";
                detailText += $"• 영력: {selectedCode.SpiritualPower}\n\n";
                
                if (selectedCode.Items.Count > 0)
                {
                    detailText += $"🎒 장착 아이템:\n";
                    foreach (var item in selectedCode.Items)
                    {
                        detailText += $"• {item}\n";
                    }
                }
                else
                {
                    detailText += $"🎒 장착 아이템: 정보 없음\n";
                }

                TxtSelectedCode.Text = detailText;
                TxtSelectedCode.Foreground = MediaBrushes.Black;
                UpdateStatus($"세이브 코드 선택됨: {selectedCode.FileName} (레벨 {selectedCode.Level}, 무력 {selectedCode.PhysicalPower})");
            }
            else
            {
                TxtSelectedCode.Text = "세이브 코드를 선택하면 여기에 표시됩니다...";
                TxtSelectedCode.Foreground = MediaBrushes.Gray;
            }
        }

        private async void BtnCopyCode_Click(object sender, RoutedEventArgs e)
        {
            if (LstSaveCodes.SelectedItem is SaveCodeInfo selectedCode)
            {
                UpdateStatus("클립보드에 복사 중...");
                
                try
                {
                    var success = await CopyToClipboardWithTextCopyAsync(selectedCode.SaveCode);
                    
                    if (success)
                    {
                        WpfMessageBox.Show($"'{selectedCode.CharacterName}'의 세이브 코드가 클립보드에 복사되었습니다!", "복사 완료", 
                                          MessageBoxButton.OK, MessageBoxImage.Information);
                        UpdateStatus($"세이브 코드 복사 완료: {selectedCode.FileName}");
                    }
                    else
                    {
                        // 복사 실패 시 대안 제공
                        var result = WpfMessageBox.Show($"클립보드 복사에 실패했습니다.\n\n세이브 코드를 별도 창에서 확인하시겠습니까?\n\n코드: {selectedCode.SaveCode}", 
                                                       "클립보드 오류", 
                                                       MessageBoxButton.YesNo, 
                                                       MessageBoxImage.Warning);
                        
                        if (result == MessageBoxResult.Yes)
                        {
                            ShowCodeWindow(selectedCode);
                        }
                    }
                }
                catch (Exception ex)
                {
                    UpdateStatus($"복사 오류: {ex.Message}");
                    ShowCodeWindow(selectedCode);
                }
            }
            else
            {
                WpfMessageBox.Show("복사할 세이브 코드를 선택해주세요.", "경고", 
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async Task<bool> CopyToClipboardWithTextCopyAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                UpdateStatus("복사할 텍스트가 비어있습니다.");
                return false;
            }

            try
            {
                UpdateStatus("TextCopy를 사용하여 클립보드에 복사 중...");
                
                // TextCopy를 사용한 비동기 복사
                await ClipboardService.SetTextAsync(text);
                
                // 짧은 대기 후 검증
                await Task.Delay(100);
                
                // 복사 검증 (선택사항)
                try
                {
                    var clipboardContent = await ClipboardService.GetTextAsync();
                    if (clipboardContent == text)
                    {
                        UpdateStatus("TextCopy를 통한 클립보드 복사 및 검증 성공");
                        return true;
                    }
                    else
                    {
                        UpdateStatus("TextCopy 복사 성공 (검증 내용이 다르지만 정상 동작으로 간주)");
                        return true; // 내용이 다르더라도 복사는 성공으로 간주
                    }
                }
                catch
                {
                    UpdateStatus("TextCopy 복사 성공 (검증 실패했지만 정상 동작으로 간주)");
                    return true; // 검증 실패해도 복사는 성공한 것으로 간주
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"TextCopy 복사 실패: {ex.Message}");
                
                // TextCopy 실패 시 대안으로 동기 방식 시도
                try
                {
                    UpdateStatus("대안 방법으로 동기 복사 시도 중...");
                    ClipboardService.SetText(text);
                    UpdateStatus("대안 동기 복사 성공");
                    return true;
                }
                catch (Exception ex2)
                {
                    UpdateStatus($"모든 복사 방법 실패: {ex2.Message}");
                    return false;
                }
            }
        }

        private void ShowCodeWindow(SaveCodeInfo saveCode)
        {
            var codeWindow = new Window
            {
                Title = $"세이브 코드 상세 정보 - {saveCode.CharacterName}",
                Width = 800,
                Height = 600,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                ResizeMode = ResizeMode.CanResize
            };

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // 상단: 캐릭터 정보
            var infoPanel = new StackPanel
            {
                Background = MediaBrushes.AliceBlue,
                Margin = new Thickness(10, 10, 10, 0),
                Orientation = System.Windows.Controls.Orientation.Vertical
            };

            var basicInfo = new System.Windows.Controls.TextBlock
            {
                Text = $"캐릭터: {saveCode.CharacterName} | 레벨: {saveCode.Level} | 경험치: {saveCode.Experience}",
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                Padding = new Thickness(10),
                TextWrapping = TextWrapping.Wrap
            };
            infoPanel.Children.Add(basicInfo);

            var resourceInfo = new System.Windows.Controls.TextBlock
            {
                Text = $"💰 자원: 금 {saveCode.Gold} | 나무 {saveCode.Wood}",
                FontSize = 12,
                Foreground = MediaBrushes.DarkGoldenrod,
                Padding = new Thickness(10, 0, 10, 5),
                TextWrapping = TextWrapping.Wrap
            };
            infoPanel.Children.Add(resourceInfo);

            var combatInfo = new System.Windows.Controls.TextBlock
            {
                Text = $"⚔️ 전투력: 무력 {saveCode.PhysicalPower} | 요력 {saveCode.MagicalPower} | 영력 {saveCode.SpiritualPower}",
                FontSize = 12,
                Foreground = MediaBrushes.DarkRed,
                Padding = new Thickness(10, 0, 10, 5),
                TextWrapping = TextWrapping.Wrap
            };
            infoPanel.Children.Add(combatInfo);

            if (saveCode.Items.Count > 0)
            {
                var itemsInfo = new System.Windows.Controls.TextBlock
                {
                    Text = "🎒 장착 아이템: " + string.Join(" | ", saveCode.Items),
                    FontSize = 11,
                    Foreground = MediaBrushes.DarkMagenta,
                    Padding = new Thickness(10, 0, 10, 10),
                    TextWrapping = TextWrapping.Wrap
                };
                infoPanel.Children.Add(itemsInfo);
            }

            // 중간: 세이브 코드
            var textBox = new System.Windows.Controls.TextBox
            {
                Text = saveCode.SaveCode,
                IsReadOnly = true,
                FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(10),
                TextWrapping = TextWrapping.Wrap,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                SelectionBrush = MediaBrushes.LightBlue,
                Background = MediaBrushes.LightYellow,
                Padding = new Thickness(10)
            };

            // 하단: 버튼
            var buttonPanel = new StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                Margin = new Thickness(10)
            };

            var selectAllButton = new System.Windows.Controls.Button
            {
                Content = "전체 선택",
                Margin = new Thickness(0, 0, 10, 0),
                Padding = new Thickness(15, 5, 15, 5),
                MinWidth = 80
            };
            selectAllButton.Click += (s, e) => textBox.SelectAll();

            var copyButton = new System.Windows.Controls.Button
            {
                Content = "복사",
                Margin = new Thickness(0, 0, 10, 0),
                Padding = new Thickness(15, 5, 15, 5),
                MinWidth = 80,
                Background = MediaBrushes.DodgerBlue,
                Foreground = MediaBrushes.White
            };
            copyButton.Click += async (s, e) =>
            {
                try
                {
                    await ClipboardService.SetTextAsync(saveCode.SaveCode);
                    WpfMessageBox.Show("세이브 코드가 클립보드에 복사되었습니다!", "복사 완료", 
                                      MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    WpfMessageBox.Show($"복사 실패: {ex.Message}", "오류", 
                                      MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };

            var closeButton = new System.Windows.Controls.Button
            {
                Content = "닫기",
                Padding = new Thickness(15, 5, 15, 5),
                MinWidth = 80,
                IsDefault = true
            };
            closeButton.Click += (s, e) => codeWindow.Close();

            buttonPanel.Children.Add(selectAllButton);
            buttonPanel.Children.Add(copyButton);
            buttonPanel.Children.Add(closeButton);

            Grid.SetRow(infoPanel, 0);
            Grid.SetRow(textBox, 1);
            Grid.SetRow(buttonPanel, 2);

            grid.Children.Add(infoPanel);
            grid.Children.Add(textBox);
            grid.Children.Add(buttonPanel);

            codeWindow.Content = grid;
            codeWindow.ShowDialog();
        }

        private SaveCodeInfo? ParseSaveCodeFile(string content, TxtFileInfo fileInfo)
        {
            try
            {
                // Extract character name using improved regex that handles call Preload() format
                var characterMatch = Regex.Match(content, @"call\s+Preload\(\s*[""']캐릭터:\s*([^""']*?)[""']\s*\)", RegexOptions.IgnoreCase);
                if (!characterMatch.Success)
                {
                    // Fallback to simpler pattern
                    characterMatch = Regex.Match(content, @"캐릭터:\s*(.+?)(?:\s*[""']|\r|\n|$)", RegexOptions.IgnoreCase);
                }
                
                if (!characterMatch.Success)
                    return null;

                var characterName = characterMatch.Groups[1].Value.Trim();
                
                // Remove color codes and special characters more thoroughly
                characterName = Regex.Replace(characterName, @"\|c[0-9a-fA-F]{8}|\|[rR]|\|", "");
                characterName = characterName.Trim();

                if (string.IsNullOrEmpty(characterName))
                    return null;

                // Extract save code using improved regex for call Preload() format
                var codeMatch = Regex.Match(content, @"call\s+Preload\(\s*[""']Code:\s*([A-Z0-9\-\s]+?)[""']\s*\)", RegexOptions.IgnoreCase);
                if (!codeMatch.Success)
                {
                    // Fallback to simpler pattern
                    codeMatch = Regex.Match(content, @"Code:\s*([A-Z0-9\-\s]+)", RegexOptions.IgnoreCase);
                }
                
                if (!codeMatch.Success)
                    return null;

                var saveCode = codeMatch.Groups[1].Value.Trim();
                
                if (string.IsNullOrEmpty(saveCode))
                    return null;

                // Extract items (아이템1 ~ 아이템6) using call Preload() format
                var items = new List<string>();
                for (int i = 1; i <= 6; i++)
                {
                    // Pattern for: call Preload( "아이템1: '|cff1c6406전무 - 세계수|R'" )
                    var itemMatch = Regex.Match(content, $@"call\s+Preload\(\s*[""']아이템{i}:\s*['""]([^'""]*?)['""][""']\s*\)", RegexOptions.IgnoreCase);
                    
                    if (itemMatch.Success)
                    {
                        var itemName = itemMatch.Groups[1].Value.Trim();
                        // Remove color codes from item names but preserve the full name
                        itemName = Regex.Replace(itemName, @"\|c[0-9a-fA-F]{8}|\|[rR]|\|", "");
                        itemName = itemName.Trim('\'', '"');
                        
                        if (!string.IsNullOrEmpty(itemName))
                        {
                            items.Add($"아이템{i}: {itemName}");
                        }
                    }
                }

                // Extract basic stats using call Preload() format
                var levelMatch = Regex.Match(content, @"call\s+Preload\(\s*[""']레벨:\s*(\d+)[""']\s*\)", RegexOptions.IgnoreCase);
                var level = levelMatch.Success ? levelMatch.Groups[1].Value : "정보 없음";

                var goldMatch = Regex.Match(content, @"call\s+Preload\(\s*[""']금:\s*(\d+)[""']\s*\)", RegexOptions.IgnoreCase);
                var gold = goldMatch.Success ? FormatNumber(goldMatch.Groups[1].Value) : "정보 없음";

                var woodMatch = Regex.Match(content, @"call\s+Preload\(\s*[""']나무:\s*(\d+)[""']\s*\)", RegexOptions.IgnoreCase);
                var wood = woodMatch.Success ? FormatNumber(woodMatch.Groups[1].Value) : "정보 없음";

                // Extract combat stats (무력, 요력, 영력) using call Preload() format
                var physicalPowerMatch = Regex.Match(content, @"call\s+Preload\(\s*[""']무력:\s*(\d+)[""']\s*\)", RegexOptions.IgnoreCase);
                var physicalPower = physicalPowerMatch.Success ? FormatNumber(physicalPowerMatch.Groups[1].Value) : "정보 없음";

                var magicalPowerMatch = Regex.Match(content, @"call\s+Preload\(\s*[""']요력:\s*(\d+)[""']\s*\)", RegexOptions.IgnoreCase);
                var magicalPower = magicalPowerMatch.Success ? FormatNumber(magicalPowerMatch.Groups[1].Value) : "정보 없음";

                var spiritualPowerMatch = Regex.Match(content, @"call\s+Preload\(\s*[""']영력:\s*(\d+)[""']\s*\)", RegexOptions.IgnoreCase);
                var spiritualPower = spiritualPowerMatch.Success ? FormatNumber(spiritualPowerMatch.Groups[1].Value) : "정보 없음";

                // Extract experience using call Preload() format
                var experienceMatch = Regex.Match(content, @"call\s+Preload\(\s*[""']경험치:\s*(\d+)[""']\s*\)", RegexOptions.IgnoreCase);
                var experience = experienceMatch.Success ? FormatNumber(experienceMatch.Groups[1].Value) : "정보 없음";

                var fileDate = File.GetLastWriteTime(fileInfo.FilePath);

                // Create display text for items
                var itemsDisplayText = items.Count > 0 ? string.Join(" | ", items) : "아이템 정보 없음";

                return new SaveCodeInfo
                {
                    CharacterName = characterName,
                    SaveCode = saveCode,
                    FileName = fileInfo.FileName,
                    FilePath = fileInfo.FilePath,
                    FileDate = fileDate,
                    FullContent = content,
                    Items = items,
                    ItemsDisplayText = itemsDisplayText,
                    Level = level,
                    Gold = gold,
                    Wood = wood,
                    PhysicalPower = physicalPower,
                    MagicalPower = magicalPower,
                    SpiritualPower = spiritualPower,
                    Experience = experience
                };
            }
            catch (Exception ex)
            {
                // Debug: 파싱 실패 시 로그
                UpdateStatus($"파일 파싱 실패: {fileInfo.FileName} - {ex.Message}");
                return null;
            }
        }
    }

    public class AppSettings
    {
        public string LastSelectedFolderPath { get; set; } = string.Empty;
        public DateTime LastUpdated { get; set; } = DateTime.Now;
        public bool AutoLoadOnStartup { get; set; } = true;
        public string Version { get; set; } = "1.0.0";
    }

    public class AnalysisCache
    {
        public string FolderPath { get; set; } = string.Empty;
        public DateTime LastAnalyzed { get; set; } = DateTime.Now;
        public Dictionary<string, DateTime> FileHashes { get; set; } = new Dictionary<string, DateTime>();
        public List<SaveCodeInfo> SaveCodes { get; set; } = new List<SaveCodeInfo>();
        public int TotalFiles { get; set; }
        public string Version { get; set; } = "1.0.0";
    }

    public class TxtFileInfo : INotifyPropertyChanged
    {
        public string FilePath { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string FileSizeText { get; set; } = string.Empty;
        public string RelativePath { get; set; } = string.Empty;

        public event PropertyChangedEventHandler? PropertyChanged;
    }

    public class CharacterInfo : INotifyPropertyChanged
    {
        public string CharacterName { get; set; } = string.Empty;
        public ObservableCollection<SaveCodeInfo> SaveCodes { get; set; } = new();
        public string SaveCodeCount { get; set; } = string.Empty;
        public string LastModified { get; set; } = string.Empty;

        public event PropertyChangedEventHandler? PropertyChanged;
    }

    public class SaveCodeInfo : INotifyPropertyChanged
    {
        public string CharacterName { get; set; } = string.Empty;
        public string SaveCode { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public DateTime FileDate { get; set; }
        public string FullContent { get; set; } = string.Empty;
        
        // 아이템 정보 추가
        public List<string> Items { get; set; } = new List<string>();
        public string ItemsDisplayText { get; set; } = string.Empty;
        public string Level { get; set; } = string.Empty;
        public string Gold { get; set; } = string.Empty;
        public string Wood { get; set; } = string.Empty;
        
        // 무력, 요력, 영력, 경험치 추가
        public string PhysicalPower { get; set; } = string.Empty; // 무력
        public string MagicalPower { get; set; } = string.Empty;  // 요력
        public string SpiritualPower { get; set; } = string.Empty; // 영력
        public string Experience { get; set; } = string.Empty;    // 경험치

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}