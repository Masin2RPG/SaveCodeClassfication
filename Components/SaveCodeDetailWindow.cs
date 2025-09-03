using System.Windows;
using System.Windows.Controls;
using SaveCodeClassfication.Models;
using TextCopy;
using MediaBrushes = System.Windows.Media.Brushes;
using WinForms = System.Windows.Forms;

namespace SaveCodeClassfication.Components
{
    /// <summary>
    /// ���̺� �ڵ� �� ������ ǥ���ϴ� â ������Ʈ
    /// </summary>
    public static class SaveCodeDetailWindow
    {
        /// <summary>
        /// ���̺� �ڵ� �� ���� â�� ǥ���մϴ�
        /// </summary>
        public static void Show(SaveCodeInfo saveCode, Window owner)
        {
            var codeWindow = new Window
            {
                Title = $"���̺� �ڵ� �� ���� - {saveCode.CharacterName}",
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
        /// ���� �׸��带 �����մϴ�
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
        /// ĳ���� ���� �г��� �����մϴ�
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
                Text = $"ĳ����: {saveCode.CharacterName} | ����: {saveCode.Level} | ����ġ: {saveCode.Experience}",
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                Padding = new Thickness(10),
                TextWrapping = TextWrapping.Wrap
            };

            var resourceInfo = new TextBlock
            {
                Text = $"?? �ڿ�: �� {saveCode.Gold} | ���� {saveCode.Wood}",
                FontSize = 12,
                Foreground = MediaBrushes.DarkGoldenrod,
                Padding = new Thickness(10, 0, 10, 5),
                TextWrapping = TextWrapping.Wrap
            };

            var combatInfo = new TextBlock
            {
                Text = $"?? ������: ���� {saveCode.PhysicalPower} | ��� {saveCode.MagicalPower} | ���� {saveCode.SpiritualPower}",
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
                    Text = "?? ���� ������: " + string.Join(" | ", saveCode.Items),
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
        /// ���̺� �ڵ� �ؽ�Ʈ�ڽ��� �����մϴ�
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
        /// ��ư �г��� �����մϴ�
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
                Content = "��ü ����",
                Margin = new Thickness(0, 0, 10, 0),
                Padding = new Thickness(15, 5, 15, 5),
                MinWidth = 80
            };

            var copyButton = new System.Windows.Controls.Button
            {
                Content = "����",
                Margin = new Thickness(0, 0, 10, 0),
                Padding = new Thickness(15, 5, 15, 5),
                MinWidth = 80,
                Background = MediaBrushes.DodgerBlue,
                Foreground = MediaBrushes.White
            };

            var closeButton = new System.Windows.Controls.Button
            {
                Content = "�ݱ�",
                Padding = new Thickness(15, 5, 15, 5),
                MinWidth = 80,
                IsDefault = true
            };

            // �̺�Ʈ �ڵ鷯 ����
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
                    System.Windows.MessageBox.Show("���̺� �ڵ尡 Ŭ�����忡 ����Ǿ����ϴ�!", "���� �Ϸ�", 
                                   MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"���� ����: {ex.Message}", "����", 
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