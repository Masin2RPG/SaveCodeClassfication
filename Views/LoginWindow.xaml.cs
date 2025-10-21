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
    /// �α��� â
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
            
            // ���� �ʱ�ȭ
            _settingsService = new SettingsService(PathConstants.ConfigFilePath);
            InitializeDatabaseService();
            
            // Enter Ű �̺�Ʈ ����
            TxtUserId.KeyDown += Input_KeyDown;
            TxtPassword.KeyDown += Input_KeyDown;
        }

        /// <summary>
        /// �����ͺ��̽� ���� �ʱ�ȭ
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
                // �⺻ �������� �ʱ�ȭ
                var defaultSettings = new DatabaseSettings();
                _databaseService = new DatabaseService(defaultSettings);
            }
        }

        /// <summary>
        /// �Է� �ʵ� ���� �� �α��� ��ư Ȱ��ȭ ���� ������Ʈ
        /// </summary>
        private void UpdateLoginButtonState()
        {
            BtnLogin.IsEnabled = !string.IsNullOrWhiteSpace(TxtUserId.Text) && 
                                !string.IsNullOrWhiteSpace(TxtPassword.Password);
        }

        /// <summary>
        /// ���̵� �Է� ���� �̺�Ʈ
        /// </summary>
        private void TxtUserId_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateLoginButtonState();
        }

        /// <summary>
        /// ��й�ȣ �Է� ���� �̺�Ʈ
        /// </summary>
        private void TxtPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            UpdateLoginButtonState();
        }

        /// <summary>
        /// Enter Ű �Է� ó��
        /// </summary>
        private void Input_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter && BtnLogin.IsEnabled)
            {
                BtnLogin_Click(sender, new RoutedEventArgs());
            }
        }

        /// <summary>
        /// �α��� ��ư Ŭ��
        /// </summary>
        private async void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtUserId.Text) || 
                string.IsNullOrWhiteSpace(TxtPassword.Password))
            {
                WpfMessageBox.Show("���̵�� ��й�ȣ�� ��� �Է����ּ���.", "�Է� ����", 
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            await PerformLoginAsync();
        }

        /// <summary>
        /// �α��� ����
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
                    WpfMessageBox.Show("�����ͺ��̽� ���񽺰� �ʱ�ȭ���� �ʾҽ��ϴ�.", "����", 
                                      MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // UserService�� DatabaseService�� ���� ���ڿ��� �ʱ�ȭ
                UserService userService;
                try
                {
                    var connectionString = _databaseService.GetConnectionString();
                    userService = new UserService(connectionString);
                    System.Diagnostics.Debug.WriteLine($"UserService �ʱ�ȭ �Ϸ�: {connectionString.Substring(0, Math.Min(50, connectionString.Length))}...");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"UserService �ʱ�ȭ ����: {ex.Message}");
                    WpfMessageBox.Show($"����� ���� �ʱ�ȭ�� �����߽��ϴ�: {ex.Message}", "�ʱ�ȭ ����", 
                                      MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // �� �α��� ���� ���� (Admin_Yn ����)
                System.Diagnostics.Debug.WriteLine($"�α��� �õ�: ����� ID = {userId}");
                var loginResult = await userService.ValidateUserWithDetailsAsync(userId, password);
                System.Diagnostics.Debug.WriteLine($"�α��� ���� ���: IsValid={loginResult.IsValid}, IsAdmin={loginResult.IsAdmin}");

                if (loginResult.IsValid)
                {
                    LoggedInUserId = userId;
                    IsLoginSuccessful = true;
                    IsAdminUser = loginResult.IsAdmin;
                    
                    var adminMessage = loginResult.IsAdmin ? " (������)" : "";
                    System.Diagnostics.Debug.WriteLine($"�α��� ����: {userId}{adminMessage}");
                    
                    WpfMessageBox.Show($"ȯ���մϴ�, {userId}��{adminMessage}!", "�α��� ����", 
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    // DialogResult�� �����Ͽ� App.xaml.cs���� ó���� �� �ֵ��� ��
                    System.Diagnostics.Debug.WriteLine("DialogResult�� True�� �����Ͽ� â�� �ݴ� ��...");
                    DialogResult = true;
                    Close();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"�α��� ����: {loginResult.ErrorMessage}");
                    WpfMessageBox.Show(loginResult.ErrorMessage, "�α��� ����", 
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                    
                    // ��й�ȣ �ʵ� �ʱ�ȭ
                    TxtPassword.Password = string.Empty;
                    TxtPassword.Focus();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"�α��� ó�� �� ����: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"���� Ʈ���̽�: {ex.StackTrace}");
                WpfMessageBox.Show($"�α��� �� ������ �߻��߽��ϴ�:\n{ex.Message}", "����", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                ShowLoadingState(false);
            }
        }

        /// <summary>
        /// ȸ������ ��ư Ŭ��
        /// </summary>
        private void BtnRegister_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var registerWindow = new RegisterWindow();
                registerWindow.Owner = this;
                
                if (registerWindow.ShowDialog() == true)
                {
                    // ȸ������ ���� �� �ڵ����� �ش� �������� �α��� ó��
                    if (!string.IsNullOrEmpty(registerWindow.RegisteredUserId))
                    {
                        TxtUserId.Text = registerWindow.RegisteredUserId;
                        TxtPassword.Focus();
                        
                        WpfMessageBox.Show("ȸ�������� �Ϸ�Ǿ����ϴ�!\n�α����� �������ּ���.", "ȸ������ �Ϸ�", 
                                      MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                WpfMessageBox.Show($"ȸ������ â�� �� �� �����ϴ�:\n{ex.Message}", "����", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// �ε� ���� ǥ��
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