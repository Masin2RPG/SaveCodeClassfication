using System.Windows;
using System.Windows.Controls;
using SaveCodeClassfication.Services;
using SaveCodeClassfication.Models;  
using SaveCodeClassfication.Utils;
using System.Threading.Tasks;
using WpfMessageBox = System.Windows.MessageBox;

namespace SaveCodeClassfication.Views
{
    /// <summary>
    /// 회원가입 창
    /// </summary>
    public partial class RegisterWindow : Window
    {
        private DatabaseService? _databaseService;
        private readonly SettingsService _settingsService;

        public string RegisteredUserId { get; private set; } = string.Empty;

        public RegisterWindow()
        {
            InitializeComponent();
            
            // 서비스 초기화
            _settingsService = new SettingsService(PathConstants.ConfigFilePath);
            InitializeDatabaseService();
            
            // Enter 키 이벤트 설정
            TxtUserId.KeyDown += Input_KeyDown;
            TxtPassword.KeyDown += Input_KeyDown;
            TxtConfirmPassword.KeyDown += Input_KeyDown;
            TxtAuthKey.KeyDown += Input_KeyDown;
        }

        /// <summary>
        /// 데이터베이스 서비스 초기화
        /// </summary>
        private async void InitializeDatabaseService()
        {
            try
            {
                var settings = await _settingsService.LoadSettingsAsync();
                _databaseService = new DatabaseService(settings.DatabaseSettings);
            }
            catch
            {
                // 기본 설정으로 초기화
                var defaultSettings = new DatabaseSettings();
                _databaseService = new DatabaseService(defaultSettings);
            }
        }

        /// <summary>
        /// 입력 필드 변경 시 회원가입 버튼 활성화 상태 업데이트
        /// </summary>
        private void UpdateRegisterButtonState()
        {
            var isFormValid = !string.IsNullOrWhiteSpace(TxtUserId.Text) && 
                             !string.IsNullOrWhiteSpace(TxtPassword.Password) &&
                             !string.IsNullOrWhiteSpace(TxtConfirmPassword.Password) &&
                             !string.IsNullOrWhiteSpace(TxtAuthKey.Text) &&
                             TxtPassword.Password == TxtConfirmPassword.Password;

            BtnRegister.IsEnabled = isFormValid;
        }

        /// <summary>
        /// 아이디 입력 변경 이벤트
        /// </summary>
        private void TxtUserId_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateRegisterButtonState();
        }

        /// <summary>
        /// 비밀번호 입력 변경 이벤트
        /// </summary>
        private void TxtPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            UpdateRegisterButtonState();
            CheckPasswordMatch();
        }

        /// <summary>
        /// 비밀번호 확인 입력 변경 이벤트
        /// </summary>
        private void TxtConfirmPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            UpdateRegisterButtonState();
            CheckPasswordMatch();
        }

        /// <summary>
        /// 인증키 입력 변경 이벤트
        /// </summary>
        private void TxtAuthKey_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateRegisterButtonState();
        }

        /// <summary>
        /// 비밀번호 일치 여부 확인
        /// </summary>
        private void CheckPasswordMatch()
        {
            if (!string.IsNullOrEmpty(TxtPassword.Password) && 
                !string.IsNullOrEmpty(TxtConfirmPassword.Password))
            {
                if (TxtPassword.Password != TxtConfirmPassword.Password)
                {
                    TxtConfirmPassword.BorderBrush = System.Windows.Media.Brushes.Red;
                }
                else
                {
                    TxtConfirmPassword.BorderBrush = System.Windows.Media.Brushes.Green;
                }
            }
            else
            {
                TxtConfirmPassword.BorderBrush = System.Windows.Media.Brushes.LightGray;
            }
        }

        /// <summary>
        /// Enter 키 입력 처리
        /// </summary>
        private void Input_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter && BtnRegister.IsEnabled)
            {
                BtnRegister_Click(sender, new RoutedEventArgs());
            }
        }

        /// <summary>
        /// 회원가입 버튼 클릭
        /// </summary>
        private async void BtnRegister_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInput())
                return;

            await PerformRegisterAsync();
        }

        /// <summary>
        /// 입력값 유효성 검사
        /// </summary>
        private bool ValidateInput()
        {
            // 빈 값 검사
            if (string.IsNullOrWhiteSpace(TxtUserId.Text))
            {
                WpfMessageBox.Show("아이디를 입력해주세요.", "입력 오류", 
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtUserId.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(TxtPassword.Password))
            {
                WpfMessageBox.Show("비밀번호를 입력해주세요.", "입력 오류", 
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtPassword.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(TxtConfirmPassword.Password))
            {
                WpfMessageBox.Show("비밀번호 확인을 입력해주세요.", "입력 오류", 
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtConfirmPassword.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(TxtAuthKey.Text))
            {
                WpfMessageBox.Show("인증키를 입력해주세요.", "입력 오류", 
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtAuthKey.Focus();
                return false;
            }

            // 비밀번호 일치 검사
            if (TxtPassword.Password != TxtConfirmPassword.Password)
            {
                WpfMessageBox.Show("비밀번호가 일치하지 않습니다.", "입력 오류", 
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtConfirmPassword.Focus();
                return false;
            }

            // 아이디 길이 검사
            if (TxtUserId.Text.Length < 3)
            {
                WpfMessageBox.Show("아이디는 3자 이상이어야 합니다.", "입력 오류", 
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtUserId.Focus();
                return false;
            }

            // 비밀번호 길이 검사
            if (TxtPassword.Password.Length < 4)
            {
                WpfMessageBox.Show("비밀번호는 4자 이상이어야 합니다.", "입력 오류", 
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtPassword.Focus();
                return false;
            }

            return true;
        }

        /// <summary>
        /// 회원가입 수행
        /// </summary>
        private async Task PerformRegisterAsync()
        {
            try
            {
                ShowLoadingState(true);

                var userId = TxtUserId.Text.Trim();
                var password = TxtPassword.Password;
                var authKey = TxtAuthKey.Text.Trim();

                if (_databaseService == null)
                {
                    WpfMessageBox.Show("데이터베이스 서비스가 초기화되지 않았습니다.", "오류", 
                                      MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // 1단계: 회원가입 전 검증 수행 (프로시저 호출 전에 먼저 체크)
                UpdateLoadingMessage("사용자 정보를 검증하는 중...");
                var validationResult = await _databaseService.ValidateRegistrationAsync(userId, authKey);

                if (!validationResult.IsValid)
                {
                    WpfMessageBox.Show(validationResult.ErrorMessage, "회원가입 실패", 
                                      MessageBoxButton.OK, MessageBoxImage.Warning);
                    
                    // 실패 시 해당 필드에 포커스
                    if (validationResult.ErrorMessage.Contains("아이디"))
                    {
                        TxtUserId.Focus();
                        TxtUserId.SelectAll();
                    }
                    else if (validationResult.ErrorMessage.Contains("인증키"))
                    {
                        TxtAuthKey.Focus();
                        TxtAuthKey.SelectAll();
                    }
                    return;
                }

                // 2단계: 검증 통과 시 회원가입 프로시저 호출
                UpdateLoadingMessage("회원가입을 처리하는 중...");
                var registerResult = await _databaseService.RegisterUserAsync(userId, password, authKey);

                if (registerResult.IsSuccess)
                {
                    RegisteredUserId = userId;
                    
                    WpfMessageBox.Show($"회원가입이 완료되었습니다!\n환영합니다, {userId}님!", "회원가입 성공", 
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    DialogResult = true;
                    Close();
                }
                else
                {
                    WpfMessageBox.Show(registerResult.ErrorMessage, "회원가입 실패", 
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                    
                    // 실패 시 해당 필드에 포커스
                    if (registerResult.ErrorMessage.Contains("아이디"))
                    {
                        TxtUserId.Focus();
                        TxtUserId.SelectAll();
                    }
                    else if (registerResult.ErrorMessage.Contains("인증키"))
                    {
                        TxtAuthKey.Focus();
                        TxtAuthKey.SelectAll();
                    }
                }
            }
            catch (Exception ex)
            {
                WpfMessageBox.Show($"회원가입 중 오류가 발생했습니다:\n{ex.Message}", "오류", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                ShowLoadingState(false);
            }
        }

        /// <summary>
        /// 로딩 메시지 업데이트
        /// </summary>
        private void UpdateLoadingMessage(string message)
        {
            var loadingText = LoadingPanel.FindName("LoadingText") as TextBlock;
            if (loadingText != null)
            {
                loadingText.Text = message;
            }
        }

        /// <summary>
        /// 취소 버튼 클릭
        /// </summary>
        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        /// <summary>
        /// 로딩 상태 표시
        /// </summary>
        private void ShowLoadingState(bool isLoading)
        {
            LoadingPanel.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;
            BtnRegister.IsEnabled = !isLoading && ValidateFormState();
            BtnCancel.IsEnabled = !isLoading;
            TxtUserId.IsEnabled = !isLoading;
            TxtPassword.IsEnabled = !isLoading;
            TxtConfirmPassword.IsEnabled = !isLoading;
            TxtAuthKey.IsEnabled = !isLoading;
        }

        /// <summary>
        /// 폼 상태 검증
        /// </summary>
        private bool ValidateFormState()
        {
            return !string.IsNullOrWhiteSpace(TxtUserId.Text) && 
                   !string.IsNullOrWhiteSpace(TxtPassword.Password) &&
                   !string.IsNullOrWhiteSpace(TxtConfirmPassword.Password) &&
                   !string.IsNullOrWhiteSpace(TxtAuthKey.Text) &&
                   TxtPassword.Password == TxtConfirmPassword.Password;
        }
    }
}