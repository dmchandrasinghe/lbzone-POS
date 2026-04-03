using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace LasanthaPOS.Desktop.Services;

public class ApiService
{
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

    public ApiService()
    {
        _http = new HttpClient { BaseAddress = new Uri("http://localhost:5100/api/") };
    }

    public string? CurrentUser { get; private set; }
    public string? CurrentRole { get; private set; }

    public async Task<bool> LoginAsync(string username, string password)
    {
        var body = JsonSerializer.Serialize(new { username, password });
        var response = await _http.PostAsync("auth/login",
            new StringContent(body, Encoding.UTF8, "application/json"));

        if (!response.IsSuccessStatusCode) return false;

        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        CurrentUser = doc.RootElement.GetProperty("fullName").GetString();
        CurrentRole = doc.RootElement.GetProperty("role").GetString();
        return true;
    }

    public async Task<List<T>> GetAsync<T>(string endpoint)
    {
        var response = await _http.GetAsync(endpoint);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<T>>(json, _json) ?? new();
    }

    public async Task<T?> GetSingleAsync<T>(string endpoint)
    {
        var response = await _http.GetAsync(endpoint);
        if (!response.IsSuccessStatusCode) return default;
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(json, _json);
    }

    public async Task<HttpResponseMessage> PostAsync<T>(string endpoint, T data)
    {
        var body = JsonSerializer.Serialize(data);
        return await _http.PostAsync(endpoint, new StringContent(body, Encoding.UTF8, "application/json"));
    }

    public async Task<HttpResponseMessage> PutAsync<T>(string endpoint, T data)
    {
        var body = JsonSerializer.Serialize(data);
        return await _http.PutAsync(endpoint, new StringContent(body, Encoding.UTF8, "application/json"));
    }

    public async Task<HttpResponseMessage> DeleteAsync(string endpoint) =>
        await _http.DeleteAsync(endpoint);
}
