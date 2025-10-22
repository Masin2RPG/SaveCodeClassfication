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
    /// TokenGeneratorView.xaml�� ���� ��ȣ �ۿ� ��
    /// </summary>
    public partial class TokenGeneratorView : System.Windows.Controls.UserControl
    {
        private ApiUserService? _apiUserService;
        private string _currentToken = string.Empty;
        private List<TokenInfo> _tokenList = new List<TokenInfo>();

        public TokenGeneratorView()
        {
            InitializeComponent();
            InitializeControls();
            InitializeApiService();
        }

        /// <summary>
        /// API ��� ���� �ʱ�ȭ
        /// </summary>
        private async void InitializeApiService()
        {
            try
            {
                _apiUserService = new ApiUserService();
                
                // API ���� �׽�Ʈ �� ��ū ��� �ε�
                _ = LoadTokenListAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"API ����� ���� �ʱ�ȭ ����: {ex.Message}");
                ShowError($"API ���� �ʱ�ȭ ����: {ex.Message}");
            }
        }

        public void SetUserService(UserService userService)
        {
            // API ������� ����Ǿ� �� �̻� ������� ����
            // ȣȯ���� ���� �޼���� ���������� ���ο����� API ���� ���
        }

        private void InitializeControls()
        {
            // �����̴� �� ���� �̺�Ʈ
            SliderTokenLength.ValueChanged += (s, e) =>
            {
                TxtTokenLength.Text = $"{(int)SliderTokenLength.Value}��";
                GeneratePreviewToken();
            };

            // ��¥ ���� �� �̸����� ��ū ����
            DatePickerEffective.SelectedDateChanged += (s, e) => GeneratePreviewToken();

            // �ʱ� ��ū ����
            GeneratePreviewToken();
        }

        private async void GeneratePreviewToken()
        {
            if (_apiUserService != null)
            {
                int length = (int)SliderTokenLength.Value;
                _currentToken = await _apiUserService.GenerateRandomTokenAsync(length);
                TxtPreviewToken.Text = _currentToken;
            }
        }

        private async Task LoadTokenListAsync()
        {
            if (_apiUserService == null) return;

            try
            {
                System.Diagnostics.Debug.WriteLine("=== API ��� ��ū ��� �ε� ���� ===");
                
                _tokenList = await _apiUserService.GetAllTokensAsync();
                
                System.Diagnostics.Debug.WriteLine($"API���� ��ȸ�� ��ū ����: {_tokenList.Count}");
                
                DataGridTokens.ItemsSource = null; // ���� ���ε� ����
                DataGridTokens.ItemsSource = _tokenList; // ���ο� ������ ���ε�
                
                UpdateTokenCount();
                
                System.Diagnostics.Debug.WriteLine("API ��� ��ū ��� UI ������Ʈ �Ϸ�");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"API ��ū ��� �ε� ����: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"���� Ʈ���̽�: {ex.StackTrace}");
                ShowError($"API�� ���� ��ū ��� ��ȸ �� ������ �߻��߽��ϴ�: {ex.Message}");
            }
        }

        private void UpdateTokenCount()
        {
            var totalCount = _tokenList.Count;
            var activeCount = _tokenList.Count(t => t.StatusText == "��밡��");
            var usedCount = _tokenList.Count(t => t.StatusText == "����");
            var expiredCount = _tokenList.Count(t => t.StatusText == "�����");
            
            TxtTokenCount.Text = $"�� {totalCount}�� (��밡��: {activeCount}, ����: {usedCount}, �����: {expiredCount})";
        }

        private void BtnRefreshToken_Click(object sender, RoutedEventArgs e)
        {
            GeneratePreviewToken();
        }

        private async void BtnGenerateToken_Click(object sender, RoutedEventArgs e)
        {
            if (_apiUserService == null)
            {
                ShowError("API ����� ���񽺰� �ʱ�ȭ���� �ʾҽ��ϴ�.");
                return;
            }

            if (!DatePickerEffective.SelectedDate.HasValue)
            {
                ShowError("��� ���� ��¥�� �������ּ���.");
                return;
            }

            try
            {
                // UI ��Ȱ��ȭ
                BtnGenerateToken.IsEnabled = false;
                BtnGenerateToken.Content = "���� ��...";

                var effectiveDate = DatePickerEffective.SelectedDate.Value;
                System.Diagnostics.Debug.WriteLine($"API ��ū ���� �õ�: Date={effectiveDate:yyyy-MM-dd}, Token={_currentToken}");
                
                var result = await _apiUserService.CreateAuthTokenAsync(effectiveDate, _currentToken);

                if (result.IsSuccess)
                {
                    System.Diagnostics.Debug.WriteLine("API ��ū ���� ����! ��� ���ΰ�ħ ����...");
                    ShowSuccess(result.GeneratedToken, effectiveDate);
                    
                    // ��ū ��� ���ΰ�ħ
                    await LoadTokenListAsync();
                    System.Diagnostics.Debug.WriteLine("API ��� ��ū ��� ���ΰ�ħ �Ϸ�");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"API ��ū ���� ����: {result.ErrorMessage}");
                    ShowError(result.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"API ��ū ���� ���� �߻�: {ex.Message}");
                ShowError($"API�� ���� ��ū ���� �� ������ �߻��߽��ϴ�: {ex.Message}");
            }
            finally
            {
                // UI �ٽ� Ȱ��ȭ
                BtnGenerateToken.IsEnabled = true;
                BtnGenerateToken.Content = "��ū ����";
            }
        }

        private void BtnCopyToken_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(TxtGeneratedToken.Text))
            {
                System.Windows.Clipboard.SetText(TxtGeneratedToken.Text);
                System.Windows.MessageBox.Show("��ū�� Ŭ�����忡 ����Ǿ����ϴ�.", "���� �Ϸ�", 
                              MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private async void BtnRefreshTokenList_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("=== API ��� ���� ��ū ��� ���ΰ�ħ ��ư Ŭ�� ===");
            
            try
            {
                // UI �ǵ��
                BtnRefreshTokenList.IsEnabled = false;
                BtnRefreshTokenList.Content = "���ΰ�ħ ��...";
                
                await LoadTokenListAsync();
                
                System.Diagnostics.Debug.WriteLine("API ��� ���� ���ΰ�ħ �Ϸ�");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"API ��� ���� ���ΰ�ħ ����: {ex.Message}");
            }
            finally
            {
                BtnRefreshTokenList.IsEnabled = true;
                BtnRefreshTokenList.Content = "��� ���ΰ�ħ";
            }
        }

        private async void BtnEditToken_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.Tag is TokenInfo tokenInfo && _apiUserService != null)
            {
                var editDialog = new TokenEditDialog(_apiUserService, tokenInfo);
                editDialog.Owner = Window.GetWindow(this);
                
                if (editDialog.ShowDialog() == true && editDialog.IsUpdated)
                {
                    // ��ū ��� ���ΰ�ħ
                    await LoadTokenListAsync();
                }
            }
        }

        private async void BtnDeleteToken_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.Tag is TokenInfo tokenInfo && _apiUserService != null)
            {
                var result = System.Windows.MessageBox.Show(
                    $"��ū '{tokenInfo.Auth_tokens}'��(��) ���� �����Ͻðڽ��ϱ�?\n\n�� �۾��� �ǵ��� �� �����ϴ�.",
                    "��ū ���� Ȯ��",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        var deleteResult = await _apiUserService.DeleteTokenAsync(tokenInfo.Auth_tokens);
                        
                        if (deleteResult.IsSuccess)
                        {
                            System.Windows.MessageBox.Show("��ū�� ���������� �����Ǿ����ϴ�.", "���� �Ϸ�", 
                                          MessageBoxButton.OK, MessageBoxImage.Information);
                            // ��ū ��� ���ΰ�ħ
                            await LoadTokenListAsync();
                        }
                        else
                        {
                            System.Windows.MessageBox.Show(deleteResult.ErrorMessage, "���� ����", 
                                          MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Windows.MessageBox.Show($"API�� ���� ��ū ���� �� ������ �߻��߽��ϴ�: {ex.Message}", "����", 
                                      MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void ShowSuccess(string token, DateTime effectiveDate)
        {
            // ���� �г� ǥ��
            ResultPanel.Visibility = Visibility.Visible;
            ErrorPanel.Visibility = Visibility.Collapsed;

            // ��� ���� ǥ��
            TxtGeneratedToken.Text = token;
            TxtEffectiveDate.Text = effectiveDate.ToString("yyyy�� MM�� dd��");
            TxtCreationTime.Text = DateTime.Now.ToString("yyyy�� MM�� dd�� HH:mm:ss");

            // ���� ��ư Ȱ��ȭ
            BtnCopyToken.IsEnabled = true;

            // �� ��ū ����
            GeneratePreviewToken();
        }

        private void ShowError(string errorMessage)
        {
            // ���� �г� ǥ��
            ErrorPanel.Visibility = Visibility.Visible;
            ResultPanel.Visibility = Visibility.Collapsed;

            // ���� �޽��� ǥ��
            TxtErrorMessage.Text = errorMessage;

            // ���� ��ư ��Ȱ��ȭ
            BtnCopyToken.IsEnabled = false;
        }
    }
}