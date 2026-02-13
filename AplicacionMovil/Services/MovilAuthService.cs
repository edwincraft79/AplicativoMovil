using System.Net.Http.Json;

namespace AplicacionMovil.Services;

public class MovilAuthService
{
    private readonly HttpClient _http;

    public MovilAuthService(HttpClient http)
    {
        _http = http;
    }

    public sealed class LoginRequest
    {
        public string UserName { get; set; } = "";
        public string Password { get; set; } = "";
        public string? CodigoMovil { get; set; }
    }

    public sealed class LoginResponse
    {
        public string Token { get; set; } = "";
        public string UserName { get; set; } = "";
        public string? Sucursal { get; set; }
        public string? Zona { get; set; }
        public string? CodigoMovil { get; set; }
        public List<string> Roles { get; set; } = new();
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest req)
    {
        var resp = await _http.PostAsJsonAsync("api/movil/auth/login", req);
        if (!resp.IsSuccessStatusCode) return null;

        return await resp.Content.ReadFromJsonAsync<LoginResponse>();
    }
}
