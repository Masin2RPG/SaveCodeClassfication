using SaveCodeClassfication.Models;
using SaveCodeClassfication.Services;
using System;
using System.Windows;
using System.Windows.Controls;

namespace SaveCodeClassfication.Views
{
    /// <summary>
    /// TokenEditDialog.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class TokenEditDialog : Window
    {
        private readonly UserService? _userService;
        private readonly ApiUserService? _apiUserService;
        private readonly TokenInfo _tokenInfo;
        
        public bool IsUpdated { get; private set; } = false;

        // 기존 UserService 기반 생성자
        public TokenEditDialog(UserService userService, TokenInfo tokenInfo)
        {
            InitializeComponent();
            _userService = userService;
            _tokenInfo = tokenInfo;
            
            InitializeData();
        }

        // API 기반 생성자
        public TokenEditDialog(ApiUserService apiUserService, TokenInfo tokenInfo)
        {
            InitializeComponent();
            _apiUserService = apiUserService;
            _tokenInfo = tokenInfo;
            
            InitializeData();
        }

        private void InitializeData()
        {
            TxtToken.Text = _tokenInfo.Auth_tokens;
            TxtCurrentDate.Text = _tokenInfo.Effective_Date.ToString("yyyy-MM-dd");
            DatePickerNewDate.SelectedDate = _tokenInfo.Effective_Date;
            TxtStatus.Text = _tokenInfo.StatusText;
            
            // 상태에 따른 색상 설정
            switch (_tokenInfo.StatusText)
            {
                case "사용가능":
                    TxtStatus.Foreground = System.Windows.Media.Brushes.Green;
                    break;
                case "사용됨":
                    TxtStatus.Foreground = System.Windows.Media.Brushes.Red;
                    break;
                case "만료됨":
                    TxtStatus.Foreground = System.Windows.Media.Brushes.Gray;
                    break;
            }
        }

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (!DatePickerNewDate.SelectedDate.HasValue)
            {
                System.Windows.MessageBox.Show("새 유효 날짜를 선택해주세요.", "입력 오류", 
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var newDate = DatePickerNewDate.SelectedDate.Value;
            
            // 현재 날짜와 같으면 저장하지 않음
            if (newDate.Date == _tokenInfo.Effective_Date.Date)
            {
                DialogResult = true;
                return;
            }

            try
            {
                BtnSave.IsEnabled = false;
                BtnSave.Content = "저장 중...";

                TokenUpdateResult result;

                // API 기반 또는 직접 DB 접근 방식 선택
                if (_apiUserService != null)
                {
                    result = await _apiUserService.UpdateTokenEffectiveDateAsync(_tokenInfo.Auth_tokens, newDate);
                }
                else if (_userService != null)
                {
                    result = await _userService.UpdateTokenEffectiveDateAsync(_tokenInfo.Auth_tokens, newDate);
                }
                else
                {
                    throw new InvalidOperationException("사용자 서비스가 초기화되지 않았습니다.");
                }

                if (result.IsSuccess)
                {
                    IsUpdated = true;
                    System.Windows.MessageBox.Show("토큰이 성공적으로 수정되었습니다.", "수정 완료", 
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                    DialogResult = true;
                }
                else
                {
                    System.Windows.MessageBox.Show(result.ErrorMessage, "수정 실패", 
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"토큰 수정 중 오류가 발생했습니다: {ex.Message}", "오류", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                BtnSave.IsEnabled = true;
                BtnSave.Content = "저장";
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}