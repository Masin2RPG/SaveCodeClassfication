using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using SaveCodeClassfication.Models;

namespace SaveCodeClassfication.Services
{
    /// <summary>
    /// API 통신을 위한 기본 서비스
    /// </summary>
    public class ApiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public ApiService()
        {
            _httpClient = new HttpClient();
            _baseUrl = "http://211.202.189.93:5036/api"; // 외부 API 서버 주소로 변경
            
            // JSON 옵션 설정
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };
        }

        protected async Task<T?> GetAsync<T>(string endpoint)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/{endpoint}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<T>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GET API 호출 오류: {ex.Message}");
                return default(T);
            }
        }

        protected async Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint, TRequest data)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/{endpoint}", data);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<TResponse>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"POST API 호출 오류: {ex.Message}");
                return default(TResponse);
            }
        }

        protected async Task<TResponse?> PutAsync<TRequest, TResponse>(string endpoint, TRequest data)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"{_baseUrl}/{endpoint}", data);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<TResponse>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PUT API 호출 오류: {ex.Message}");
                return default(TResponse);
            }
        }

        protected async Task<TResponse?> DeleteAsync<TResponse>(string endpoint)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"{_baseUrl}/{endpoint}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<TResponse>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DELETE API 호출 오류: {ex.Message}");
                return default(TResponse);
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}