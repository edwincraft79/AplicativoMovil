using AplicacionMovil.Core.Models;
using AplicacionMovil.Modules.Reclamos.Data;
using AplicacionMovil.Modules.Reclamos.Models;
using AplicacionMovil.Services;
using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AplicacionMovil.Modules.Reclamos.Pages
{
    public partial class HistorialPage : ContentPage
    {
        private const string API_URL = "https://elpuregistro.com/api/InterrupcionesMovil/registrar";

        private readonly HttpClient _http = new();
        private readonly DatabaseService _db = new();

        private List<EjecucionReclamoOt> _lista = new();

        public HistorialPage()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await CargarHistorialAsync();
        }

        // ================== CARGAR HISTORIAL ==================
        private async Task CargarHistorialAsync()
        {
            try
            {
                _lista = await _db.ListarReclamosOtAsync();

                cvHistorial.ItemsSource = null;
                cvHistorial.ItemsSource = _lista;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"No se pudo leer el historial.\n{ex.Message}", "OK");
            }
        }

        // ================== SWIPE ENVIAR ==================
        private async void OnEnviarPendienteInvoked(object sender, EventArgs e)
        {
            if (sender is not SwipeItem swipeItem)
                return;

            if (swipeItem.BindingContext is not EjecucionReclamoOt reg)
                return;

            if (reg.Sincronizado)
            {
                await DisplayAlert("Info", "Este registro ya fue enviado.", "OK");
                return;
            }

            var confirmar = await DisplayAlert(
                "Enviar registro",
                "¿Desea enviar este registro al servidor?",
                "Sí",
                "No");

            if (!confirmar)
                return;

            try
            {
                var ok = await EnviarRegistroAlServidorAsync(reg);

                if (!ok)
                    return;

                await _db.MarcarReclamoOtComoSincronizadoAsync(reg.Id);
                await CargarHistorialAsync();

                await DisplayAlert("Listo", "El registro se envió correctamente.", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"No se pudo enviar el registro.\n{ex.Message}", "OK");
            }
        }

        // ================== ENVIAR AL SERVIDOR ==================
        private async Task<bool> EnviarRegistroAlServidorAsync(EjecucionReclamoOt reg)
        {
            string? fotoBase64 = null;

            var fotos = await _db.ObtenerFotosReclamoOtAsync(reg.Id);
            var primeraFoto = fotos.FirstOrDefault();

            if (primeraFoto != null &&
                !string.IsNullOrWhiteSpace(primeraFoto.RutaLocal) &&
                File.Exists(primeraFoto.RutaLocal))
            {
                var bytes = await File.ReadAllBytesAsync(primeraFoto.RutaLocal);
                fotoBase64 = Convert.ToBase64String(bytes);
            }

            var modelo = new InterrupcionMovilRequest
            {
                CodigoReclamo = reg.CodigoReclamo ?? "",
                Usuario = reg.Usuario ?? (SesionMovil.Usuario ?? ""),
                FechaHoraAtencion = reg.FechaHoraAtencion,

                ClasificacionReclamo = reg.ClasificacionReclamo ?? "",
                NaturalezaInterrupcion = reg.NaturalezaInterrupcion ?? "",
                EquipoReposicion = reg.EquipoReposicion ?? "",
                FasesReposicion = reg.FasesReposicion ?? "",
                DetalleAlumbrado = reg.DetalleAlumbrado ?? "",
                DetalleRiesgo = reg.DetalleRiesgo ?? "",
                Desestimado = reg.Desestimado,
                DescripcionSolucion = reg.DescripcionSolucion ?? "",

                FotoBase64 = fotoBase64,

                Latitud = reg.Latitud,
                Longitud = reg.Longitud,
                PrecisionGps = reg.PrecisionGps
            };

            var json = JsonSerializer.Serialize(modelo);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var resp = await _http.PostAsync(API_URL, content);

            if (!resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync();
                await DisplayAlert(
                    "Servidor",
                    $"Error {(int)resp.StatusCode} - {resp.StatusCode}\n{body}",
                    "OK");
                return false;
            }

            return true;
        }

        // ================== ELIMINAR ==================
        private async void OnEliminarInvoked(object sender, EventArgs e)
        {
            if (sender is not SwipeItem swipeItem)
                return;

            if (swipeItem.BindingContext is not EjecucionReclamoOt reg)
                return;

            var confirmar = await DisplayAlert(
                "Eliminar registro",
                "¿Desea eliminar este registro del historial offline?",
                "Sí",
                "No");

            if (!confirmar)
                return;

            await _db.EliminarReclamoOtAsync(reg.Id);
            await CargarHistorialAsync();
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("///MisOtPage");
        }
    }
}