using System.Windows;
using System.Windows.Controls;
using SaveCodeClassfication.Models;
using TextCopy;
using MediaBrushes = System.Windows.Media.Brushes;
using WinForms = System.Windows.Forms;

namespace SaveCodeClassfication.Components
{
    /// <summary>
    /// 세이브 코드 상세 정보를 표시하는 창 컴포넌트
    /// </summary>
    public static class SaveCodeDetailWindow
    {
        /// <summary>
        /// 세이브 코드 상세 정보 창을 표시합니다
        /// </summary>
        public static void Show(SaveCodeInfo saveCode, Window owner)
        {
            var codeWindow = new Window
            {
                Title = $"세이브 코드 상세 정보 - {saveCode.CharacterName}",
                Width = 800,
                Height = 600,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = owner,
                ResizeMode = ResizeMode.CanResize
            };

            var grid = CreateMainGrid();
            var infoPanel = CreateInfoPanel(saveCode);
            var textBox = CreateCodeTextBox(saveCode);
            var buttonPanel = CreateButtonPanel(saveCode, codeWindow);

            Grid.SetRow(infoPanel, 0);
            Grid.SetRow(textBox, 1);
            Grid.SetRow(buttonPanel, 2);

            grid.Children.Add(infoPanel);
            grid.Children.Add(textBox);
            grid.Children.Add(buttonPanel);

            codeWindow.Content = grid;
            codeWindow.ShowDialog();
        }

        /// <summary>
        /// 메인 그리드를 생성합니다
        /// </summary>
        private static Grid CreateMainGrid()
        {
            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            return grid;
        }

        /// <summary>
        /// 캐릭터 정보 패널을 생성합니다
        /// </summary>
        private static StackPanel CreateInfoPanel(SaveCodeInfo saveCode)
        {
            var infoPanel = new StackPanel
            {
                Background = MediaBrushes.AliceBlue,
                Margin = new Thickness(10, 10, 10, 0),
                Orientation = System.Windows.Controls.Orientation.Vertical
            };

            var basicInfo = new TextBlock
            {
                Text = $"캐릭터: {saveCode.CharacterName} | 레벨: {saveCode.Level} | 경험치: {saveCode.Experience}",
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                Padding = new Thickness(10),
                TextWrapping = TextWrapping.Wrap
            };

            var resourceInfo = new TextBlock
            {
                Text = $"?? 자원: 금 {saveCode.Gold} | 나무 {saveCode.Wood}",
                FontSize = 12,
                Foreground = MediaBrushes.DarkGoldenrod,
                Padding = new Thickness(10, 0, 10, 5),
                TextWrapping = TextWrapping.Wrap
            };

            var combatInfo = new TextBlock
            {
                Text = $"?? 전투력: 무력 {saveCode.PhysicalPower} | 요력 {saveCode.MagicalPower} | 영력 {saveCode.SpiritualPower}",
                FontSize = 12,
                Foreground = MediaBrushes.DarkRed,
                Padding = new Thickness(10, 0, 10, 5),
                TextWrapping = TextWrapping.Wrap
            };

            infoPanel.Children.Add(basicInfo);
            infoPanel.Children.Add(resourceInfo);
            infoPanel.Children.Add(combatInfo);

            if (saveCode.Items.Count > 0)
            {
                var itemsInfo = new TextBlock
                {
                    Text = "?? 장착 아이템: " + string.Join(" | ", saveCode.Items),
                    FontSize = 11,
                    Foreground = MediaBrushes.DarkMagenta,
                    Padding = new Thickness(10, 0, 10, 10),
                    TextWrapping = TextWrapping.Wrap
                };
                infoPanel.Children.Add(itemsInfo);
            }

            return infoPanel;
        }

        /// <summary>
        /// 세이브 코드 텍스트박스를 생성합니다
        /// </summary>
        private static System.Windows.Controls.TextBox CreateCodeTextBox(SaveCodeInfo saveCode)
        {
            return new System.Windows.Controls.TextBox
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
        }

        /// <summary>
        /// 버튼 패널을 생성합니다
        /// </summary>
        private static StackPanel CreateButtonPanel(SaveCodeInfo saveCode, Window parentWindow)
        {
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

            var copyButton = new System.Windows.Controls.Button
            {
                Content = "복사",
                Margin = new Thickness(0, 0, 10, 0),
                Padding = new Thickness(15, 5, 15, 5),
                MinWidth = 80,
                Background = MediaBrushes.DodgerBlue,
                Foreground = MediaBrushes.White
            };

            var closeButton = new System.Windows.Controls.Button
            {
                Content = "닫기",
                Padding = new Thickness(15, 5, 15, 5),
                MinWidth = 80,
                IsDefault = true
            };

            // 이벤트 핸들러 설정
            selectAllButton.Click += (s, e) =>
            {
                if (parentWindow.Content is Grid grid && grid.Children[1] is System.Windows.Controls.TextBox textBox)
                {
                    textBox.SelectAll();
                }
            };

            copyButton.Click += async (s, e) =>
            {
                try
                {
                    await ClipboardService.SetTextAsync(saveCode.SaveCode);
                    System.Windows.MessageBox.Show("세이브 코드가 클립보드에 복사되었습니다!", "복사 완료", 
                                   MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"복사 실패: {ex.Message}", "오류", 
                                   MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };

            closeButton.Click += (s, e) => parentWindow.Close();

            buttonPanel.Children.Add(selectAllButton);
            buttonPanel.Children.Add(copyButton);
            buttonPanel.Children.Add(closeButton);

            return buttonPanel;
        }
    }
}