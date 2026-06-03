using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Polly;
using Polly.Extensions.Http;

namespace EnergyPC.WinForms.Services;

/// <summary>
/// Client HTTP unique (Singleton) vers l'API locale.
/// Réutiliser une seule instance d'HttpClient évite l'épuisement des ports TCP.
/// Intègre une politique de réessais (Polly) sur les erreurs transitoires.
/// </summary>
public class ApiClient
{
    private readonly HttpClient _http;
    private readonly IAsyncPolicy<HttpResponseMessage> _policy;

    public ApiClient(string baseUrl)
    {
        _http = new HttpClient { BaseAddress = new Uri(baseUrl) };
        _http.Timeout = TimeSpan.FromSeconds(5);

        // 3 réessais avec back-off exponentiel (200, 400, 800 ms).
        _policy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(3, n =>
                TimeSpan.FromMilliseconds(200 * Math.Pow(2, n)));
    }

    /// <summary>Renseigne le jeton JWT pour les appels suivants.</summary>
    public void SetToken(string token) =>
        _http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

    /// <summary>GET JSON typé, avec réessais transitoires.</summary>
    public async Task<T?> GetAsync<T>(string url)
    {
        var rep = await _policy.ExecuteAsync(() => _http.GetAsync(url));
        rep.EnsureSuccessStatusCode();
        return await rep.Content.ReadFromJsonAsync<T>();
    }

    /// <summary>POST JSON typé (utilisé pour /api/auth/login).</summary>
    public async Task<TOut?> PostAsync<TIn, TOut>(string url, TIn body)
    {
        var rep = await _http.PostAsJsonAsync(url, body);
        rep.EnsureSuccessStatusCode();
        return await rep.Content.ReadFromJsonAsync<TOut>();
    }
}
