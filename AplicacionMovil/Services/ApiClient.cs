using AplicacionMovil.Data;
using System.Net;
using System.Net.Http.Headers;

namespace AplicacionMovil.Services
{
    public static class ApiClient
    {
        private static readonly HttpClientHandler _handler = new HttpClientHandler
        {
            AllowAutoRedirect = false,                  // ✅ CLAVE: no seguir redirects
            AutomaticDecompression = DecompressionMethods.All
        };

        public static readonly HttpClient Http = new HttpClient(_handler)
        {
            BaseAddress = new Uri("https://elpuregistro.com/")
        };

        public static async Task ApplyBearerAsync()
        {
            if (string.IsNullOrWhiteSpace(SesionMovil.Token)) 
                await SesionMovil.RestaurarAsync();

            Http.DefaultRequestHeaders.Authorization = null;

            // ✅ pedir JSON explícitamente
            Http.DefaultRequestHeaders.Accept.Clear();
            Http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            if (!string.IsNullOrWhiteSpace(SesionMovil.Token))
            {
                Http.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", SesionMovil.Token.Trim());
            }
        }
    }
}
