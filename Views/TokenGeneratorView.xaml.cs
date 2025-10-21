using SaveCodeClassfication.Models;
using SaveCodeClassfication.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Threading.Tasks;

namespace SaveCodeClassfication.Views
{
    /// <summary>
    /// TokenGeneratorView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class TokenGeneratorView : System.Windows.Controls.UserControl
    {
        private UserService? _userService;
        private string _currentToken = string.Empty;
        private List<TokenInfo> _tokenList = new List<TokenInfo>();

        public TokenGeneratorView()
        {
            InitializeComponent();
            InitializeControls();
        }

        public void SetUserService(UserService userService)
        {
            _userService = userService;
            // UserService가 설정되면 토큰 목록을 로드
            _ = LoadTokenListAsync();
        }

        private void InitializeControls()
        {
            // 슬라이더 값 변경 이벤트
            SliderTokenLength.ValueChanged += (s, e) =>
            {
                TxtTokenLength.Text = $"{(int)SliderTokenLength.Value}자";
                GeneratePreviewToken();
            };

            // 날짜 변경 시 미리보기 토큰 생성
            DatePickerEffective.SelectedDateChanged += (s, e) => GeneratePreviewToken();

            // 초기 토큰 생성
            GeneratePreviewToken();
        }

        private void GeneratePreviewToken()
        {
            if (_userService != null)
            {
                int length = (int)SliderTokenLength.Value;
                _currentToken = _userService.GenerateRandomToken(length);
                TxtPreviewToken.Text = _currentToken;
            }
        }

        private async Task LoadTokenListAsync()
        {
            if (_userService == null) return;

            try
            {
                System.Diagnostics.Debug.WriteLine("=== 토큰 목록 로드 시작 ===");
                
                // 먼저 테이블 존재 여부 확인
                var tableExists = await _userService.CheckTokenTableExistsAsync();
                if (!tableExists)
                {
                    ShowError("user_auth_tokens 테이블을 찾을 수 없습니다.");
                    return;
                }
                
                System.Diagnostics.Debug.WriteLine("테이블 확인 완료, 토큰 목록 조회 시작");
                _tokenList = await _userService.GetAllTokensAsync();
                
                System.Diagnostics.Debug.WriteLine($"조회된 토큰 개수: {_tokenList.Count}");
                
                DataGridTokens.ItemsSource = null; // 기존 바인딩 해제
                DataGridTokens.ItemsSource = _tokenList; // 새로운 데이터 바인딩
                
                UpdateTokenCount();
                
                System.Diagnostics.Debug.WriteLine("토큰 목록 UI 업데이트 완료");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"토큰 목록 로드 오류: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"스택 트레이스: {ex.StackTrace}");
                ShowError($"토큰 목록을 불러오는 중 오류가 발생했습니다: {ex.Message}");
            }
        }

        private void UpdateTokenCount()
        {
            var totalCount = _tokenList.Count;
            var activeCount = _tokenList.Count(t => t.StatusText == "사용가능");
            var usedCount = _tokenList.Count(t => t.StatusText == "사용됨");
            var expiredCount = _tokenList.Count(t => t.StatusText == "만료됨");
            
            TxtTokenCount.Text = $"총 {totalCount}개 (사용가능: {activeCount}, 사용됨: {usedCount}, 만료됨: {expiredCount})";
        }

        private void BtnRefreshToken_Click(object sender, RoutedEventArgs e)
        {
            GeneratePreviewToken();
        }

        private async void BtnGenerateToken_Click(object sender, RoutedEventArgs e)
        {
            if (_userService == null)
            {
                ShowError("사용자 서비스가 초기화되지 않았습니다.");
                return;
            }

            if (!DatePickerEffective.SelectedDate.HasValue)
            {
                ShowError("사용 가능 날짜를 선택해주세요.");
                return;
            }

            try
            {
                // UI 비활성화
                BtnGenerateToken.IsEnabled = false;
                BtnGenerateToken.Content = "생성 중...";

                var effectiveDate = DatePickerEffective.SelectedDate.Value;
                System.Diagnostics.Debug.WriteLine($"토큰 생성 시도: Date={effectiveDate:yyyy-MM-dd}, Token={_currentToken}");
                
                var result = await _userService.CreateAuthTokenAsync(effectiveDate, _currentToken);

                if (result.IsSuccess)
                {
                    System.Diagnostics.Debug.WriteLine("토큰 생성 성공! 목록 새로고침 시작...");
                    ShowSuccess(result.GeneratedToken, effectiveDate);
                    
                    // 토큰 목록 새로고침
                    await LoadTokenListAsync();
                    System.Diagnostics.Debug.WriteLine("토큰 목록 새로고침 완료");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"토큰 생성 실패: {result.ErrorMessage}");
                    ShowError(result.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"토큰 생성 예외 발생: {ex.Message}");
                ShowError($"토큰 생성 중 오류가 발생했습니다: {ex.Message}");
            }
            finally
            {
                // UI 다시 활성화
                BtnGenerateToken.IsEnabled = true;
                BtnGenerateToken.Content = "토큰 생성";
            }
        }

        private void BtnCopyToken_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(TxtGeneratedToken.Text))
            {
                System.Windows.Clipboard.SetText(TxtGeneratedToken.Text);
                System.Windows.MessageBox.Show("토큰이 클립보드에 복사되었습니다.", "복사 완료", 
                              MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private async void BtnRefreshTokenList_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("=== 수동 토큰 목록 새로고침 버튼 클릭 ===");
            
            try
            {
                // UI 피드백
                BtnRefreshTokenList.IsEnabled = false;
                BtnRefreshTokenList.Content = "새로고침 중...";
                
                await LoadTokenListAsync();
                
                System.Diagnostics.Debug.WriteLine("수동 새로고침 완료");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"수동 새로고침 오류: {ex.Message}");
            }
            finally
            {
                BtnRefreshTokenList.IsEnabled = true;
                BtnRefreshTokenList.Content = "목록 새로고침";
            }
        }

        private async void BtnEditToken_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.Tag is TokenInfo tokenInfo && _userService != null)
            {
                var editDialog = new TokenEditDialog(_userService, tokenInfo);
                editDialog.Owner = Window.GetWindow(this);
                
                if (editDialog.ShowDialog() == true && editDialog.IsUpdated)
                {
                    // 토큰 목록 새로고침
                    await LoadTokenListAsync();
                }
            }
        }

        private async void BtnDeleteToken_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.Tag is TokenInfo tokenInfo && _userService != null)
            {
                var result = System.Windows.MessageBox.Show(
                    $"토큰 '{tokenInfo.Auth_tokens}'을(를) 정말 삭제하시겠습니까?\n\n이 작업은 되돌릴 수 없습니다.",
                    "토큰 삭제 확인",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        var deleteResult = await _userService.DeleteTokenAsync(tokenInfo.Auth_tokens);
                        
                        if (deleteResult.IsSuccess)
                        {
                            System.Windows.MessageBox.Show("토큰이 성공적으로 삭제되었습니다.", "삭제 완료", 
                                          MessageBoxButton.OK, MessageBoxImage.Information);
                            // 토큰 목록 새로고침
                            await LoadTokenListAsync();
                        }
                        else
                        {
                            System.Windows.MessageBox.Show(deleteResult.ErrorMessage, "삭제 실패", 
                                          MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Windows.MessageBox.Show($"토큰 삭제 중 오류가 발생했습니다: {ex.Message}", "오류", 
                                      MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void ShowSuccess(string token, DateTime effectiveDate)
        {
            // 성공 패널 표시
            ResultPanel.Visibility = Visibility.Visible;
            ErrorPanel.Visibility = Visibility.Collapsed;

            // 결과 정보 표시
            TxtGeneratedToken.Text = token;
            TxtEffectiveDate.Text = effectiveDate.ToString("yyyy년 MM월 dd일");
            TxtCreationTime.Text = DateTime.Now.ToString("yyyy년 MM월 dd일 HH:mm:ss");

            // 복사 버튼 활성화
            BtnCopyToken.IsEnabled = true;

            // 새 토큰 생성
            GeneratePreviewToken();
        }

        private void ShowError(string errorMessage)
        {
            // 오류 패널 표시
            ErrorPanel.Visibility = Visibility.Visible;
            ResultPanel.Visibility = Visibility.Collapsed;

            // 오류 메시지 표시
            TxtErrorMessage.Text = errorMessage;

            // 복사 버튼 비활성화
            BtnCopyToken.IsEnabled = false;
        }
    }
}