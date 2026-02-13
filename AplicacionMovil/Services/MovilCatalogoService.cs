using System.Net.Http.Json;
using AplicacionMovil.Models;

namespace AplicacionMovil.Services
{
    public class MovilCatalogoService
    {
        private readonly HttpClient _http;

        public MovilCatalogoService()
        {
            _http = new HttpClient
            {
                BaseAddress = new Uri("https://elpuregistro.com/")
            };
        }

        public async Task<List<OperadorDto>> ObtenerOperadoresAsync()
        {
            try
            {
                var data = await _http.GetFromJsonAsync<List<OperadorDto>>(
                    "api/movil/catalogo/operadores");

                return data ?? new List<OperadorDto>();
            }
            catch
            {
                return new List<OperadorDto>();
            }
        }
    }
}
