using AplicacionMovil.Services;          // ApiClient
using Microsoft.Maui.Devices.Sensors;
using System.Text;
using System.Text.Json;

namespace AplicacionMovil.Modules.Calidad.Pages;

[QueryProperty(nameof(OtId), "OtId")]
[QueryProperty(nameof(Modalidad), "Modalidad")]
[QueryProperty(nameof(CodSed), "CodSed")]
[QueryProperty(nameof(Suministro), "Suministro")]
[QueryProperty(nameof(NombreCliente), "NombreCliente")]
[QueryProperty(nameof(Periodo), "Periodo")]
[QueryProperty(nameof(LatitudStr), "Latitud")]
[QueryProperty(nameof(LongitudStr), "Longitud")]
public partial class EjecutarCalidadPage : ContentPage
{
    // ── QueryProperties ───────────────────────────────────────────────
    public long OtId { get; set; }
    public string Modalidad { get; set; } = "";
    public string CodSed { get; set; } = "";
    public string Suministro { get; set; } = "";
    public string NombreCliente { get; set; } = "";
    public string Periodo { get; set; } = "";
    public string LatitudStr { get; set; } = "";
    public string LongitudStr { get; set; } = "";

    // ── Estado interno ────────────────────────────────────────────────
    private double? _gpsLat;
    private double? _gpsLon;
    private double? _gpsPrecision;
    private readonly List<string> _fotosBase64 = new();

    public EjecutarCalidadPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Poblar labels de solo lectura
        lblSubtitulo.Text = $"{CodSed} · {Periodo}";
        lblModalidad.Text = Modalidad;
        lblModalidad.TextColor = Modalidad == "RURAL"
            ? Color.FromArgb("#065F46")
            : Color.FromArgb("#1E40AF");

        lblCodSed.Text = CodSed;
        lblSuministro.Text = Suministro;
        lblCliente.Text = NombreCliente;
    }

    // ── GPS ───────────────────────────────────────────────────────────
    private async void OnObtenerGpsClicked(object sender, EventArgs e)
    {
        try
        {
            lblGps.Text = "Obteniendo GPS...";
            lblGps.TextColor = Color.FromArgb("#0EA5E9");

            var request = new GeolocationRequest(GeolocationAccuracy.High, TimeSpan.FromSeconds(10));
            var location = await Geolocation.GetLocationAsync(request);

            if (location != null)
            {
                _gpsLat = location.Latitude;
                _gpsLon = location.Longitude;
                _gpsPrecision = location.Accuracy;

                lblGps.Text = $"📍 Lat: {_gpsLat.Value:F6}\n" +
                              $"    Lon: {_gpsLon.Value:F6}\n" +
                              $"    Precisión: {_gpsPrecision?.ToString("F1") ?? "?"} m";
                lblGps.TextColor = Color.FromArgb("#065F46");
            }
            else
            {
                lblGps.Text = "No se pudo obtener GPS. Intente de nuevo.";
                lblGps.TextColor = Color.FromArgb("#DC2626");
            }
        }
        catch (Exception ex)
        {
            lblGps.Text = $"Error GPS: {ex.Message}";
            lblGps.TextColor = Color.FromArgb("#DC2626");
        }
    }

    // ── FOTOS ─────────────────────────────────────────────────────────
    private async void OnTomarFotoClicked(object sender, EventArgs e)
    {
        if (_fotosBase64.Count >= 5)
        {
            await DisplayAlert("Fotos", "Máximo 5 fotos por OT.", "OK");
            return;
        }

        try
        {
            FileResult? foto = null;

            var accion = await DisplayActionSheet(
                "Agregar foto", "Cancelar", null,
                "📷 Cámara", "🖼️ Galería");

            if (accion == "📷 Cámara")
                foto = await MediaPicker.Default.CapturePhotoAsync();
            else if (accion == "🖼️ Galería")
                foto = await MediaPicker.Default.PickPhotoAsync();

            if (foto == null) return;

            using var stream = await foto.OpenReadAsync();
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            var b64 = Convert.ToBase64String(ms.ToArray());

            _fotosBase64.Add(b64);
            ActualizarMiniaturas(ms.ToArray());
            lblContadorFotos.Text = $"{_fotosBase64.Count}/5";
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"No se pudo capturar la foto: {ex.Message}", "OK");
        }
    }

    private void ActualizarMiniaturas(byte[] bytes)
    {
        var img = new Image
        {
            Source = ImageSource.FromStream(() => new MemoryStream(bytes)),
            WidthRequest = 72,
            HeightRequest = 72,
            Aspect = Aspect.AspectFill,
            Margin = new Thickness(0, 4, 6, 4)
        };
        flxFotos.Add(img);
    }

    // ── ENVIAR ────────────────────────────────────────────────────────
    private async void OnEnviarClicked(object sender, EventArgs e)
    {
        // Validaciones
        if (string.IsNullOrWhiteSpace(txtDescripcion.Text))
        {
            await DisplayAlert("Validación", "Ingresa una descripción de lo observado.", "OK");
            return;
        }

        if (pkrResultado.SelectedIndex < 0)
        {
            await DisplayAlert("Validación", "Selecciona un resultado.", "OK");
            return;
        }

        btnEnviar.IsEnabled = false;
        actIndicator.IsVisible = true;
        actIndicator.IsRunning = true;

        try
        {
            await ApiClient.ApplyBearerAsync();

            var payload = new
            {
                otId = OtId,
                modalidad = Modalidad,
                fechaHoraAtencion = DateTime.Now,
                descripcion = txtDescripcion.Text.Trim(),
                resultado = pkrResultado.Items[pkrResultado.SelectedIndex],
                latitud = _gpsLat,
                longitud = _gpsLon,
                precisionGps = _gpsPrecision,
                fotosBase64 = _fotosBase64
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var resp = await ApiClient.Http.PostAsync("api/calidad-movil/ejecutar", content);
            var body = await resp.Content.ReadAsStringAsync();

            if (resp.IsSuccessStatusCode)
            {
                await DisplayAlert("✅ Éxito",
                    "Medición registrada correctamente.", "OK");
                await Shell.Current.GoToAsync("..");   // volver a MisOtCalidadPage
            }
            else
            {
                await DisplayAlert("Error",
                    $"No se pudo registrar: {body}", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error de red: {ex.Message}", "OK");
        }
        finally
        {
            btnEnviar.IsEnabled = true;
            actIndicator.IsVisible = false;
            actIndicator.IsRunning = false;
        }
    }

    // ── VOLVER ────────────────────────────────────────────────────────
    private async void OnVolverClicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("..");
}
