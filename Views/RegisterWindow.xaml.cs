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
    /// ȸ������ â
    /// </summary>
    public partial class RegisterWindow : Window
    {
        private readonly SettingsService _settingsService;

        public string RegisteredUserId { get; private set; } = string.Empty;

        public RegisterWindow()
        {
            InitializeComponent();
            
            // ���� �ʱ�ȭ
            _settingsService = new SettingsService(PathConstants.ConfigFilePath);
            
            // Enter Ű �̺�Ʈ ����
            TxtUserId.KeyDown += Input_KeyDown;
            TxtPassword.KeyDown += Input_KeyDown;
            TxtConfirmPassword.KeyDown += Input_KeyDown;
            TxtAuthKey.KeyDown += Input_KeyDown;
        }

        /// <summary>
        /// �Է� �ʵ� ���� �� ȸ������ ��ư Ȱ��ȭ ���� ������Ʈ
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
        /// ���̵� �Է� ���� �̺�Ʈ
        /// </summary>
        private void TxtUserId_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateRegisterButtonState();
        }

        /// <summary>
        /// ��й�ȣ �Է� ���� �̺�Ʈ
        /// </summary>
        private void TxtPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            UpdateRegisterButtonState();
            CheckPasswordMatch();
        }

        /// <summary>
        /// ��й�ȣ Ȯ�� �Է� ���� �̺�Ʈ
        /// </summary>
        private void TxtConfirmPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            UpdateRegisterButtonState();
            CheckPasswordMatch();
        }

        /// <summary>
        /// ����Ű �Է� ���� �̺�Ʈ
        /// </summary>
        private void TxtAuthKey_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateRegisterButtonState();
        }

        /// <summary>
        /// ��й�ȣ ��ġ ���� Ȯ��
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
        /// Enter Ű �Է� ó��
        /// </summary>
        private void Input_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter && BtnRegister.IsEnabled)
            {
                BtnRegister_Click(sender, new RoutedEventArgs());
            }
        }

        /// <summary>
        /// ȸ������ ��ư Ŭ��
        /// </summary>
        private async void BtnRegister_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInput())
                return;

            await PerformRegisterAsync();
        }

        /// <summary>
        /// �Է°� ��ȿ�� �˻�
        /// </summary>
        private bool ValidateInput()
        {
            // �� �� �˻�
            if (string.IsNullOrWhiteSpace(TxtUserId.Text))
            {
                WpfMessageBox.Show("���̵� �Է����ּ���.", "�Է� ����", 
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtUserId.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(TxtPassword.Password))
            {
                WpfMessageBox.Show("��й�ȣ�� �Է����ּ���.", "�Է� ����", 
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtPassword.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(TxtConfirmPassword.Password))
            {
                WpfMessageBox.Show("��й�ȣ Ȯ���� �Է����ּ���.", "�Է� ����", 
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtConfirmPassword.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(TxtAuthKey.Text))
            {
                WpfMessageBox.Show("����Ű�� �Է����ּ���.", "�Է� ����", 
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtAuthKey.Focus();
                return false;
            }

            // ��й�ȣ ��ġ �˻�
            if (TxtPassword.Password != TxtConfirmPassword.Password)
            {
                WpfMessageBox.Show("��й�ȣ�� ��ġ���� �ʽ��ϴ�.", "�Է� ����", 
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtConfirmPassword.Focus();
                return false;
            }

            // ���̵� ���� �˻�
            if (TxtUserId.Text.Length < 3)
            {
                WpfMessageBox.Show("���̵�� 3�� �̻��̾�� �մϴ�.", "�Է� ����", 
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtUserId.Focus();
                return false;
            }

            // ��й�ȣ ���� �˻�
            if (TxtPassword.Password.Length < 4)
            {
                WpfMessageBox.Show("��й�ȣ�� 4�� �̻��̾�� �մϴ�.", "�Է� ����", 
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtPassword.Focus();
                return false;
            }

            return true;
        }

        /// <summary>
        /// ȸ������ ����
        /// </summary>
        private async Task PerformRegisterAsync()
        {
            try
            {
                ShowLoadingState(true);

                var userId = TxtUserId.Text.Trim();
                var password = TxtPassword.Password;
                var authKey = TxtAuthKey.Text.Trim();

                // API ��� �����ͺ��̽� ���� �ʱ�ȭ
                ApiDatabaseService? apiDatabaseService = null;
                try
                {
                    UpdateLoadingMessage("API ������ �����ϴ� ��...");
                    apiDatabaseService = new ApiDatabaseService();
                    
                    // API ���� �׽�Ʈ
                    var isConnected = await apiDatabaseService.TestConnectionAsync();
                    
                    if (!isConnected)
                    {
                        WpfMessageBox.Show("API ������ ������ �� �����ϴ�.\n" +
                                          "API ������ ���������� Ȯ�����ּ���.\n" +
                                          "(�⺻ �ּ�: http://localhost:5036)", 
                                          "API ���� ����", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }
                catch (Exception ex)
                {
                    WpfMessageBox.Show($"API ���� �ʱ�ȭ �� ������ �߻��߽��ϴ�:\n{ex.Message}", 
                                      "�ʱ�ȭ ����", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // 1�ܰ�: ȸ������ �� ���� ���� (API ���)
                UpdateLoadingMessage("����� ������ �����ϴ� ��...");
                var validationResult = await apiDatabaseService.ValidateRegistrationAsync(userId, authKey);

                if (!validationResult.IsValid)
                {
                    WpfMessageBox.Show(validationResult.ErrorMessage, "ȸ������ ����", 
                                      MessageBoxButton.OK, MessageBoxImage.Warning);
                    
                    // ���� �� �ش� �ʵ忡 ��Ŀ��
                    if (validationResult.ErrorMessage.Contains("���̵�"))
                    {
                        TxtUserId.Focus();
                        TxtUserId.SelectAll();
                    }
                    else if (validationResult.ErrorMessage.Contains("����Ű"))
                    {
                        TxtAuthKey.Focus();
                        TxtAuthKey.SelectAll();
                    }
                    return;
                }

                // 2�ܰ�: ���� ��� �� ȸ������ API ȣ��
                UpdateLoadingMessage("ȸ�������� ó���ϴ� ��...");
                var registerResult = await apiDatabaseService.RegisterUserAsync(userId, password, authKey);

                if (registerResult.IsSuccess)
                {
                    RegisteredUserId = userId;
                    
                    WpfMessageBox.Show($"ȸ�������� �Ϸ�Ǿ����ϴ�!\nȯ���մϴ�, {userId}��!", "ȸ������ ����", 
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    DialogResult = true;
                    Close();
                }
                else
                {
                    WpfMessageBox.Show(registerResult.ErrorMessage, "ȸ������ ����", 
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                    
                    // ���� �� �ش� �ʵ忡 ��Ŀ��
                    if (registerResult.ErrorMessage.Contains("���̵�"))
                    {
                        TxtUserId.Focus();
                        TxtUserId.SelectAll();
                    }
                    else if (registerResult.ErrorMessage.Contains("����Ű"))
                    {
                        TxtAuthKey.Focus();
                        TxtAuthKey.SelectAll();
                    }
                }
            }
            catch (Exception ex)
            {
                WpfMessageBox.Show($"ȸ������ �� ������ �߻��߽��ϴ�:\n{ex.Message}", "����", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                ShowLoadingState(false);
            }
        }

        /// <summary>
        /// �ε� �޽��� ������Ʈ
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
        /// ��� ��ư Ŭ��
        /// </summary>
        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        /// <summary>
        /// �ε� ���� ǥ��
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
        /// �� ���� ����
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