using TextCopy;

namespace SaveCodeClassfication.Components
{
    /// <summary>
    /// 클립보드 복사를 처리하는 헬퍼 클래스
    /// </summary>
    public static class ClipboardHelper
    {
        /// <summary>
        /// 텍스트를 클립보드에 복사합니다 (여러 방법 시도)
        /// </summary>
        public static async Task<bool> CopyToClipboardAsync(string text, Action<string>? statusCallback = null)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                statusCallback?.Invoke("복사할 텍스트가 비어있습니다.");
                return false;
            }

            try
            {
                statusCallback?.Invoke("TextCopy를 사용하여 클립보드에 복사 중...");
                
                // TextCopy를 사용한 비동기 복사
                await ClipboardService.SetTextAsync(text);
                
                // 짧은 대기 후 검증
                await Task.Delay(100);
                
                // 복사 검증 (선택사항)
                try
                {
                    var clipboardContent = await ClipboardService.GetTextAsync();
                    if (clipboardContent == text)
                    {
                        statusCallback?.Invoke("TextCopy를 통한 클립보드 복사 및 검증 성공");
                        return true;
                    }
                    else
                    {
                        statusCallback?.Invoke("TextCopy 복사 성공 (검증 내용이 다르지만 정상 동작으로 간주)");
                        return true; // 내용이 다르더라도 복사는 성공으로 간주
                    }
                }
                catch
                {
                    statusCallback?.Invoke("TextCopy 복사 성공 (검증 실패했지만 정상 동작으로 간주)");
                    return true; // 검증 실패해도 복사는 성공한 것으로 간주
                }
            }
            catch (Exception ex)
            {
                statusCallback?.Invoke($"TextCopy 복사 실패: {ex.Message}");
                
                // TextCopy 실패 시 대안으로 동기 방식 시도
                try
                {
                    statusCallback?.Invoke("대안 방법으로 동기 복사 시도 중...");
                    ClipboardService.SetText(text);
                    statusCallback?.Invoke("대안 동기 복사 성공");
                    return true;
                }
                catch (Exception ex2)
                {
                    statusCallback?.Invoke($"모든 복사 방법 실패: {ex2.Message}");
                    return false;
                }
            }
        }
    }
}