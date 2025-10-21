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
    /// 로그인 창
    /// </summary>
    public partial class LoginWindow : Window
    {
        private DatabaseService? _databaseService;
        private readonly SettingsService _settingsService;

        public string LoggedInUserId { get; private set; } = string.Empty;
        public bool IsLoginSuccessful { get; private set; } = false;
        public bool IsAdminUser { get; private set; } = false;

        public LoginWindow()
        {
            InitializeComponent();
            
            // 서비스 초기화
            _settingsService = new SettingsService(PathConstants.ConfigFilePath);
            InitializeDatabaseService();
            
            // Enter 키 이벤트 설정
            TxtUserId.KeyDown += Input_KeyDown;
            TxtPassword.KeyDown += Input_KeyDown;
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
        /// 입력 필드 변경 시 로그인 버튼 활성화 상태 업데이트
        /// </summary>
        private void UpdateLoginButtonState()
        {
            BtnLogin.IsEnabled = !string.IsNullOrWhiteSpace(TxtUserId.Text) && 
                                !string.IsNullOrWhiteSpace(TxtPassword.Password);
        }

        /// <summary>
        /// 아이디 입력 변경 이벤트
        /// </summary>
        private void TxtUserId_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateLoginButtonState();
        }

        /// <summary>
        /// 비밀번호 입력 변경 이벤트
        /// </summary>
        private void TxtPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            UpdateLoginButtonState();
        }

        /// <summary>
        /// Enter 키 입력 처리
        /// </summary>
        private void Input_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter && BtnLogin.IsEnabled)
            {
                BtnLogin_Click(sender, new RoutedEventArgs());
            }
        }

        /// <summary>
        /// 로그인 버튼 클릭
        /// </summary>
        private async void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtUserId.Text) || 
                string.IsNullOrWhiteSpace(TxtPassword.Password))
            {
                WpfMessageBox.Show("아이디와 비밀번호를 모두 입력해주세요.", "입력 오류", 
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            await PerformLoginAsync();
        }

        /// <summary>
        /// 로그인 수행
        /// </summary>
        private async Task PerformLoginAsync()
        {
            try
            {
                ShowLoadingState(true);

                var userId = TxtUserId.Text.Trim();
                var password = TxtPassword.Password;

                if (_databaseService == null)
                {
                    WpfMessageBox.Show("데이터베이스 서비스가 초기화되지 않았습니다.", "오류", 
                                      MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // UserService를 DatabaseService의 연결 문자열로 초기화
                UserService userService;
                try
                {
                    var connectionString = _databaseService.GetConnectionString();
                    userService = new UserService(connectionString);
                    System.Diagnostics.Debug.WriteLine($"UserService 초기화 완료: {connectionString.Substring(0, Math.Min(50, connectionString.Length))}...");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"UserService 초기화 실패: {ex.Message}");
                    WpfMessageBox.Show($"사용자 서비스 초기화에 실패했습니다: {ex.Message}", "초기화 오류", 
                                      MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // 상세 로그인 검증 수행 (Admin_Yn 포함)
                System.Diagnostics.Debug.WriteLine($"로그인 시도: 사용자 ID = {userId}");
                var loginResult = await userService.ValidateUserWithDetailsAsync(userId, password);
                System.Diagnostics.Debug.WriteLine($"로그인 검증 결과: IsValid={loginResult.IsValid}, IsAdmin={loginResult.IsAdmin}");

                if (loginResult.IsValid)
                {
                    LoggedInUserId = userId;
                    IsLoginSuccessful = true;
                    IsAdminUser = loginResult.IsAdmin;
                    
                    var adminMessage = loginResult.IsAdmin ? " (관리자)" : "";
                    System.Diagnostics.Debug.WriteLine($"로그인 성공: {userId}{adminMessage}");
                    
                    WpfMessageBox.Show($"환영합니다, {userId}님{adminMessage}!", "로그인 성공", 
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    // DialogResult를 설정하여 App.xaml.cs에서 처리할 수 있도록 함
                    System.Diagnostics.Debug.WriteLine("DialogResult를 True로 설정하여 창을 닫는 중...");
                    DialogResult = true;
                    Close();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"로그인 실패: {loginResult.ErrorMessage}");
                    WpfMessageBox.Show(loginResult.ErrorMessage, "로그인 실패", 
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                    
                    // 비밀번호 필드 초기화
                    TxtPassword.Password = string.Empty;
                    TxtPassword.Focus();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"로그인 처리 중 예외: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"스택 트레이스: {ex.StackTrace}");
                WpfMessageBox.Show($"로그인 중 오류가 발생했습니다:\n{ex.Message}", "오류", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                ShowLoadingState(false);
            }
        }

        /// <summary>
        /// 회원가입 버튼 클릭
        /// </summary>
        private void BtnRegister_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var registerWindow = new RegisterWindow();
                registerWindow.Owner = this;
                
                if (registerWindow.ShowDialog() == true)
                {
                    // 회원가입 성공 시 자동으로 해당 계정으로 로그인 처리
                    if (!string.IsNullOrEmpty(registerWindow.RegisteredUserId))
                    {
                        TxtUserId.Text = registerWindow.RegisteredUserId;
                        TxtPassword.Focus();
                        
                        WpfMessageBox.Show("회원가입이 완료되었습니다!\n로그인을 진행해주세요.", "회원가입 완료", 
                                      MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                WpfMessageBox.Show($"회원가입 창을 열 수 없습니다:\n{ex.Message}", "오류", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 로딩 상태 표시
        /// </summary>
        private void ShowLoadingState(bool isLoading)
        {
            LoadingPanel.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;
            BtnLogin.IsEnabled = !isLoading && !string.IsNullOrWhiteSpace(TxtUserId.Text) && 
                                !string.IsNullOrWhiteSpace(TxtPassword.Password);
            BtnRegister.IsEnabled = !isLoading;
            TxtUserId.IsEnabled = !isLoading;
            TxtPassword.IsEnabled = !isLoading;
        }
    }
}