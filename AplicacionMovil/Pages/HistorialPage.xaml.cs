using AplicacionMovil.Data;          // InterrupcionRegistro, SesionMovil
using AplicacionMovil.Models;       // InterrupcionMovilRequest
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AplicacionMovil.Pages
{
    public partial class HistorialPage : ContentPage
    {
        // MISMA URL QUE USAS EN OnEnviarClicked DEL FORMULARIO PRINCIPAL
        private const string API_URL =
            "https://elpuregistro.com/api/InterrupcionesMovil/registrar";

        private readonly HttpClient _http = new();
        private readonly string _archivoPendientesPath;

        private List<InterrupcionRegistro> _lista = new();

        public HistorialPage()
        {
            InitializeComponent();

            _archivoPendientesPath = Path.Combine(
                FileSystem.AppDataDirectory,
                "interrupciones_pendientes.json");
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
                if (File.Exists(_archivoPendientesPath))
                {
                    var json = await File.ReadAllTextAsync(_archivoPendientesPath);
                    _lista = JsonSerializer.Deserialize<List<InterrupcionRegistro>>(json)
                             ?? new List<InterrupcionRegistro>();
                }
                else
                {
                    _lista = new List<InterrupcionRegistro>();
                }

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

            if (swipeItem.BindingContext is not InterrupcionRegistro reg)
                return;

            // Ya está enviado
            if (reg.Enviado)
            {
                await DisplayAlert("Info", "Este registro ya fue enviado.", "OK");
                return;
            }

            var confirmar = await DisplayAlert(
                "Enviar registro",
                "¿Desea enviar este registro al servidor?",
                "Sí", "No");

            if (!confirmar)
                return;

            try
            {
                var ok = await EnviarRegistroAlServidorAsync(reg);

                if (ok)
                {
                    reg.Enviado = true;
                    await GuardarHistorialAsync();

                    // refrescar lista
                    cvHistorial.ItemsSource = null;
                    cvHistorial.ItemsSource = _lista;

                    await DisplayAlert("Listo",
                        "El registro se envió correctamente.",
                        "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error",
                    $"No se pudo enviar el registro.\n{ex.Message}",
                    "OK");
            }
        }

        // ================== ENVIAR AL SERVIDOR (igual que botón Enviar) ==================
        private async Task<bool> EnviarRegistroAlServidorAsync(InterrupcionRegistro reg)
        {
            // Foto a Base64
            string? fotoBase64 = reg.FotoBase64;

            if (string.IsNullOrWhiteSpace(fotoBase64) && reg.FotoBytes?.Length > 0)
                fotoBase64 = Convert.ToBase64String(reg.FotoBytes);

            var modelo = new InterrupcionMovilRequest
            {
                CodigoReclamo = reg.CodigoReclamo ?? "",
                Usuario = reg.Usuario ?? (SesionMovil.Usuario ?? ""),
                FechaHoraAtencion = reg.Fecha,

                ClasificacionReclamo = reg.ClasificacionReclamo ?? "",
                NaturalezaInterrupcion = reg.NaturalezaInterrupcion ?? "",
                EquipoReposicion = reg.EquipoReposicion ?? "",
                FasesReposicion = reg.FasesReposicion ?? "",
                DetalleAlumbrado = reg.DetalleAlumbrado ?? "",
                DetalleRiesgo = reg.DetalleRiesgo ?? "",
                // ✅ aquí:
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
                await DisplayAlert("Servidor",
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

            if (swipeItem.BindingContext is not InterrupcionRegistro reg)
                return;

            var confirmar = await DisplayAlert(
                "Eliminar registro",
                "¿Desea eliminar este registro del historial offline?",
                "Sí", "No");

            if (!confirmar)
                return;

            _lista.Remove(reg);
            await GuardarHistorialAsync();

            // refrescar CollectionView
            cvHistorial.ItemsSource = null;
            cvHistorial.ItemsSource = _lista;
        }


        // ================== GUARDAR HISTORIAL ==================
        private async Task GuardarHistorialAsync()
        {
            var json = JsonSerializer.Serialize(
                _lista,
                new JsonSerializerOptions { WriteIndented = true });

            await File.WriteAllTextAsync(_archivoPendientesPath, json);
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            // Ir directamente a la página de Órdenes asignadas
            await Shell.Current.GoToAsync("///MisOtPage");
        }

    }
}
