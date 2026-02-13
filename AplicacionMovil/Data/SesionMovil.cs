// Data/SesionMovil.cs
using System.Text.Json;

namespace AplicacionMovil.Data
{
    public static class SesionMovil
    {
        private const string StorageKey = "sesion_movil_v1";

        public static bool EstaLogueado { get; private set; }

        public static string Usuario { get; private set; } = string.Empty;
        public static string Cliente { get; private set; } = string.Empty;
        public static string Zona { get; private set; } = string.Empty;

        public static string Movil { get; private set; } = string.Empty;        // Ej: "ILAVE1"
        public static string CodigoMovil { get; private set; } = string.Empty;  // Ej: "ILAVE1" (puede ser igual a Movil)

        public static string? Token { get; set; }
        public static Task<string?> GetTokenAsync() => SecureStorage.GetAsync("jwt_token");

        // Alias que usas para consultar OT
        public static string EquipoMovil => Movil;

        public static async Task<bool> RestaurarAsync()
        {
            try
            {
                var json = await SecureStorage.GetAsync(StorageKey);
                if (string.IsNullOrWhiteSpace(json))
                    return false;

                var dto = JsonSerializer.Deserialize<SesionDto>(json);
                if (dto == null || string.IsNullOrWhiteSpace(dto.Token))
                    return false;

                Usuario = dto.Usuario ?? "";
                Cliente = dto.Cliente ?? "";
                Zona = dto.Zona ?? "";
                Movil = dto.Movil ?? "";
                CodigoMovil = dto.CodigoMovil ?? dto.Movil ?? "";
                Token = dto.Token ?? "";

                EstaLogueado = true;
                return true;
            }
            catch
            {
                // si SecureStorage falla (emulador/permiso), simplemente no restauramos
                return false;
            }
        }

        public static async Task IniciarSesionAsync(
            string usuario,
            string cliente,
            string zona,
            string movil,
            string codigoMovil,
            string token)
        {
            Usuario = usuario ?? "";
            Cliente = cliente ?? "";
            Zona = zona ?? "";
            Movil = movil ?? "";
            CodigoMovil = string.IsNullOrWhiteSpace(codigoMovil) ? Movil : codigoMovil;
            Token = token ?? "";

            EstaLogueado = !string.IsNullOrWhiteSpace(Token);

            var dto = new SesionDto
            {
                Usuario = Usuario,
                Cliente = Cliente,
                Zona = Zona,
                Movil = Movil,
                CodigoMovil = CodigoMovil,
                Token = Token
            };

            try
            {
                await SecureStorage.SetAsync(StorageKey, JsonSerializer.Serialize(dto));
            }
            catch
            {
                // si no puede guardar, igual queda en memoria
            }
        }

        public static Task CerrarSesionAsync()
        {
            EstaLogueado = false;
            Usuario = Cliente = Zona = Movil = CodigoMovil = "";
            Token = "";

            try { SecureStorage.Remove(StorageKey); } catch { }
            return Task.CompletedTask;
        }

        private sealed class SesionDto
        {
            public string? Usuario { get; set; }
            public string? Cliente { get; set; }
            public string? Zona { get; set; }
            public string? Movil { get; set; }
            public string? CodigoMovil { get; set; }
            public string? Token { get; set; }
        }
    }
}
