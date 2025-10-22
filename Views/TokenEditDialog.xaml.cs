using SaveCodeClassfication.Models;
using SaveCodeClassfication.Services;
using System;
using System.Windows;
using System.Windows.Controls;

namespace SaveCodeClassfication.Views
{
    /// <summary>
    /// TokenEditDialog.xaml�� ���� ��ȣ �ۿ� ��
    /// </summary>
    public partial class TokenEditDialog : Window
    {
        private readonly UserService? _userService;
        private readonly ApiUserService? _apiUserService;
        private readonly TokenInfo _tokenInfo;
        
        public bool IsUpdated { get; private set; } = false;

        // ���� UserService ��� ������
        public TokenEditDialog(UserService userService, TokenInfo tokenInfo)
        {
            InitializeComponent();
            _userService = userService;
            _tokenInfo = tokenInfo;
            
            InitializeData();
        }

        // API ��� ������
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
            
            // ���¿� ���� ���� ����
            switch (_tokenInfo.StatusText)
            {
                case "��밡��":
                    TxtStatus.Foreground = System.Windows.Media.Brushes.Green;
                    break;
                case "����":
                    TxtStatus.Foreground = System.Windows.Media.Brushes.Red;
                    break;
                case "�����":
                    TxtStatus.Foreground = System.Windows.Media.Brushes.Gray;
                    break;
            }
        }

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (!DatePickerNewDate.SelectedDate.HasValue)
            {
                System.Windows.MessageBox.Show("�� ��ȿ ��¥�� �������ּ���.", "�Է� ����", 
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var newDate = DatePickerNewDate.SelectedDate.Value;
            
            // ���� ��¥�� ������ �������� ����
            if (newDate.Date == _tokenInfo.Effective_Date.Date)
            {
                DialogResult = true;
                return;
            }

            try
            {
                BtnSave.IsEnabled = false;
                BtnSave.Content = "���� ��...";

                TokenUpdateResult result;

                // API ��� �Ǵ� ���� DB ���� ��� ����
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
                    throw new InvalidOperationException("����� ���񽺰� �ʱ�ȭ���� �ʾҽ��ϴ�.");
                }

                if (result.IsSuccess)
                {
                    IsUpdated = true;
                    System.Windows.MessageBox.Show("��ū�� ���������� �����Ǿ����ϴ�.", "���� �Ϸ�", 
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                    DialogResult = true;
                }
                else
                {
                    System.Windows.MessageBox.Show(result.ErrorMessage, "���� ����", 
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"��ū ���� �� ������ �߻��߽��ϴ�: {ex.Message}", "����", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                BtnSave.IsEnabled = true;
                BtnSave.Content = "����";
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}