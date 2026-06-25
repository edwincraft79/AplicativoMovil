using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.Maui.Storage;
using AplicacionMovil.Modules.Deficiencias.Data;
using AplicacionMovil.Modules.Deficiencias.Models;
using AplicacionMovil.Core.Models;

namespace AplicacionMovil.Services
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;
        private const string BASE_URL = "https://elpuregistro.com/api"; // Cambiar según tu servidor
        private const string TOKEN_KEY = "jwt_token";
        public ApiService()
        {
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromMinutes(5) // Timeout largo para subir fotos
            };
        }

        // ================== ENVIAR SUBSANACIÓN DE DEFICIENCIA ==================
        public async Task<(bool ok, string msg)> EnviarSubsanacionAsync(EjecucionDeficiencia ejecucion)
        {
            try
            {
                const string API_URL = BASE_URL + "/deficiencias-movil/subsanar";

                var fotosBase64 = new List<string>();

                if (ejecucion.Fotos != null)
                {
                    foreach (var path in ejecucion.Fotos)
                    {
                        if (!File.Exists(path)) continue;
                        var bytes = await File.ReadAllBytesAsync(path);
                        fotosBase64.Add(Convert.ToBase64String(bytes));
                    }
                }

                var payload = new
                {
                    IdDeficiencia = ejecucion.IdDeficiencia,
                    CodigoDeficiencia = ejecucion.CodigoDeficiencia,
                    Usuario = ejecucion.UsuarioEjecucion,
                    FechaHoraAtencion = ejecucion.FechaEjecucion,
                    Observaciones = ejecucion.Observaciones,
                    EstadoSubsanacion = ejecucion.EstadoSubsanacion,
                    Latitud = ejecucion.Latitud,
                    Longitud = ejecucion.Longitud,
                    PrecisionGps = ejecucion.Precision,
                    FotosBase64 = fotosBase64
                };

                var json = System.Text.Json.JsonSerializer.Serialize(payload);

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(API_URL, content);

                var respText = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    return (false, $"HTTP {(int)response.StatusCode} {response.StatusCode}\n{respText}");

                return (true, "OK");
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }



        // ================== OBTENER DEFICIENCIAS POR USUARIO ==================
        public async Task<List<Deficiencia>> ObtenerDeficienciasPorUsuarioAsync(int idUsuario)
        {
            try
            {
                const string API_URL = BASE_URL + "/deficiencias/usuario/";

                var response = await _httpClient.GetAsync($"{API_URL}{idUsuario}");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var deficiencias = System.Text.Json.JsonSerializer.Deserialize<List<Deficiencia>>(json);
                    return deficiencias ?? new List<Deficiencia>();
                }

                return new List<Deficiencia>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener deficiencias por usuario: {ex.Message}");
                return new List<Deficiencia>();
            }
        }

        // ================== OBTENER HISTORIAL DE DEFICIENCIAS ==================
        public async Task<List<EjecucionDeficiencia>> ObtenerHistorialDeficienciasAsync(int idUsuario)
        {
            try
            {
                const string API_URL = BASE_URL + "/deficiencias/historial/";

                var response = await _httpClient.GetAsync($"{API_URL}{idUsuario}");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var historial = System.Text.Json.JsonSerializer.Deserialize<List<EjecucionDeficiencia>>(json);
                    return historial ?? new List<EjecucionDeficiencia>();
                }

                return new List<EjecucionDeficiencia>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener historial desde API: {ex.Message}");
                return new List<EjecucionDeficiencia>();
            }
        }

        // ================== SINCRONIZAR PENDIENTES ==================
        public async Task<int> SincronizarPendientesAsync(List<EjecucionDeficiencia> pendientes)
        {
            int exitosos = 0;

            foreach (var ejecucion in pendientes)
            {
                try
                {
                    var (ok, msg) = await EnviarSubsanacionAsync(ejecucion);
                    if (ok)
                    {
                        exitosos++;
                    }
                    else
                    {
                        Console.WriteLine($"No se pudo sincronizar {ejecucion.CodigoDeficiencia}: {msg}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error al sincronizar {ejecucion.CodigoDeficiencia}: {ex.Message}");
                }
            }

            return exitosos;
        }

        private async Task AplicarBearerAsync()
        {
            try
            {
                var token = SesionMovil.Token;

                if (string.IsNullOrWhiteSpace(token))
                    token = await SesionMovil.GetTokenAsync(); // si quieres fallback

                token = (token ?? "").Trim();
                if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    token = token.Substring(7).Trim();

                if (string.IsNullOrWhiteSpace(token))
                {
                    _httpClient.DefaultRequestHeaders.Authorization = null;
                    return;
                }

                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);
            }
            catch
            {
                _httpClient.DefaultRequestHeaders.Authorization = null;
            }
        }



        // ================== ENVIAR INSPECCIÓN (MÓVIL) ==================
        // POST: /api/movil/deficiencias/inspecciones   (requiere JWT)
        public async Task<(bool ok, string msg, long idServidor)> EnviarInspeccionAsync(InspeccionDefCreateRequest req)
        {
            try
            {
                // ✅ 1) Aplicar JWT
                await AplicarBearerAsync();

                // ✅ 2) Preparar foto
                string? fotoBase64 = null;
                if (!string.IsNullOrWhiteSpace(req.RutaFoto) && File.Exists(req.RutaFoto))
                {
                    var bytes = await File.ReadAllBytesAsync(req.RutaFoto);
                    fotoBase64 = Convert.ToBase64String(bytes);
                }

                const string API_URL = BASE_URL + "/movil/deficiencias/inspecciones";

                // ✅ 3) Enviar payload REAL (incluye foto)
                var payload = new
                {
                    req.UnidadZonal,
                    req.CodigoDenunciante,
                    req.CodigoTipoInstalacion,
                    req.NivelTension,           // ✅ AGREGADO
                    req.CodigoTipificacion,
                    req.Observaciones,
                    req.FechaInspeccion,
                    req.UtmEste,
                    req.UtmNorte,
                    req.Latitud,
                    req.Longitud,
                    req.PrecisionGpsM,
                    FotoBase64 = fotoBase64
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(API_URL, content);
                var respText = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    return (false, $"HTTP {(int)response.StatusCode} {response.StatusCode}\n{respText}", 0);

                using var doc = JsonDocument.Parse(respText);
                var id = doc.RootElement.TryGetProperty("id", out var p) ? p.GetInt64() : 0;

                return (true, "OK", id);
            }
            catch (Exception ex)
            {
                return (false, ex.Message, 0);
            }
        }
    }
}
