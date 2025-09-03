using TextCopy;

namespace SaveCodeClassfication.Components
{
    /// <summary>
    /// Ŭ������ ���縦 ó���ϴ� ���� Ŭ����
    /// </summary>
    public static class ClipboardHelper
    {
        /// <summary>
        /// �ؽ�Ʈ�� Ŭ�����忡 �����մϴ� (���� ��� �õ�)
        /// </summary>
        public static async Task<bool> CopyToClipboardAsync(string text, Action<string>? statusCallback = null)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                statusCallback?.Invoke("������ �ؽ�Ʈ�� ����ֽ��ϴ�.");
                return false;
            }

            try
            {
                statusCallback?.Invoke("TextCopy�� ����Ͽ� Ŭ�����忡 ���� ��...");
                
                // TextCopy�� ����� �񵿱� ����
                await ClipboardService.SetTextAsync(text);
                
                // ª�� ��� �� ����
                await Task.Delay(100);
                
                // ���� ���� (���û���)
                try
                {
                    var clipboardContent = await ClipboardService.GetTextAsync();
                    if (clipboardContent == text)
                    {
                        statusCallback?.Invoke("TextCopy�� ���� Ŭ������ ���� �� ���� ����");
                        return true;
                    }
                    else
                    {
                        statusCallback?.Invoke("TextCopy ���� ���� (���� ������ �ٸ����� ���� �������� ����)");
                        return true; // ������ �ٸ����� ����� �������� ����
                    }
                }
                catch
                {
                    statusCallback?.Invoke("TextCopy ���� ���� (���� ���������� ���� �������� ����)");
                    return true; // ���� �����ص� ����� ������ ������ ����
                }
            }
            catch (Exception ex)
            {
                statusCallback?.Invoke($"TextCopy ���� ����: {ex.Message}");
                
                // TextCopy ���� �� ������� ���� ��� �õ�
                try
                {
                    statusCallback?.Invoke("��� ������� ���� ���� �õ� ��...");
                    ClipboardService.SetText(text);
                    statusCallback?.Invoke("��� ���� ���� ����");
                    return true;
                }
                catch (Exception ex2)
                {
                    statusCallback?.Invoke($"��� ���� ��� ����: {ex2.Message}");
                    return false;
                }
            }
        }
    }
}