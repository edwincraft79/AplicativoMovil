using AplicacionMovil.Services;
using Microsoft.Maui.Devices.Sensors;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace AplicacionMovil.Modules.Mantenimiento.Pages;

[QueryProperty(nameof(OtId), "OtId")]
[QueryProperty(nameof(TipoMantenimiento), "TipoMantenimiento")]
[QueryProperty(nameof(CodigoOT), "CodigoOT")]
[QueryProperty(nameof(Nombre), "Nombre")]
[QueryProperty(nameof(ReferenciaUbicacion), "ReferenciaUbicacion")]
[QueryProperty(nameof(DefinicionProblema), "DefinicionProblema")]
[QueryProperty(nameof(Lat), "Lat")]
[QueryProperty(nameof(Lon), "Lon")]
public partial class EjecutarMantenimientoPage : ContentPage
{
    public int OtId { get; set; }
    public string TipoMantenimiento { get; set; } = "";
    public string CodigoOT { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string ReferenciaUbicacion { get; set; } = "";
    public string DefinicionProblema { get; set; } = "";

    // Llegan al volver de ConfirmarPuntoCampoPage (mapa con red primaria/secundaria).
    public string? Lat { get; set; }
    public string? Lon { get; set; }

    private double? _gpsLat;
    private double? _gpsLon;
    private double? _gpsPrecision;
    private readonly List<string> _fotosBase64 = new();

    public EjecutarMantenimientoPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        lblSubtitulo.Text = $"{CodigoOT} · {TipoMantenimiento}";
        lblTipo.Text = TipoMantenimiento;
        lblTipo.TextColor = TipoMantenimiento.StartsWith("C", StringComparison.OrdinalIgnoreCase)
            ? Color.FromArgb("#991B1B")
            : Color.FromArgb("#4C1D95");

        lblNombre.Text            = Nombre;
        lblReferenciaUbicacion.Text = ReferenciaUbicacion;
        lblDefinicionProblema.Text  = DefinicionProblema;

        if (double.TryParse(Lat, NumberStyles.Float, CultureInfo.InvariantCulture, out var lat) &&
            double.TryParse(Lon, NumberStyles.Float, CultureInfo.InvariantCulture, out var lon))
        {
            _gpsLat = lat;
            _gpsLon = lon;
            _gpsPrecision = null;
            lblGps.Text       = $"📌 {lat:F6}, {lon:F6}";
            lblGps.TextColor  = Color.FromArgb("#4C1D95");
            lblPrecision.Text = "Ubicación confirmada en el mapa";
        }
    }

    private async void OnObtenerGpsClicked(object sender, EventArgs e)
    {
        // Abre el mapa (red primaria + secundaria) para ubicar el punto con precisión,
        // igual que en Suministros, en vez de capturar el GPS crudo del dispositivo a ciegas.
        var latIni = _gpsLat?.ToString(CultureInfo.InvariantCulture) ?? "";
        var lonIni = _gpsLon?.ToString(CultureInfo.InvariantCulture) ?? "";

        await Shell.Current.GoToAsync(nameof(ConfirmarPuntoCampoPage), new Dictionary<string, object>
        {
            ["OtId"] = OtId,
            ["CodigoOT"] = CodigoOT,
            ["ModoRetorno"] = "ejecucion",
            ["LatInicial"] = latIni,
            ["LonInicial"] = lonIni,
        });
    }

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
            using var ms     = new MemoryStream();
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
            WidthRequest  = 72,
            HeightRequest = 72,
            Aspect        = Aspect.AspectFill,
            Margin        = new Thickness(0, 4, 6, 4)
        };
        flxFotos.Add(img);
    }

    private async void OnEnviarClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtObservaciones.Text))
        {
            await DisplayAlert("Validación", "Ingresa las observaciones del trabajo realizado.", "OK");
            return;
        }

        if (pkrResultado.SelectedIndex < 0)
        {
            await DisplayAlert("Validación", "Selecciona el resultado del trabajo.", "OK");
            return;
        }

        btnEnviar.IsEnabled      = false;
        actIndicator.IsVisible   = true;
        actIndicator.IsRunning   = true;

        try
        {
            await ApiClient.ApplyBearerAsync();

            var payload = new
            {
                otId             = OtId,
                tipoMantenimiento = TipoMantenimiento,
                fechaHoraAtencion = DateTime.Now,
                resultado        = pkrResultado.Items[pkrResultado.SelectedIndex],
                observaciones    = txtObservaciones.Text.Trim(),
                latitud          = _gpsLat,
                longitud         = _gpsLon,
                precisionGps     = _gpsPrecision,
                fotosBase64      = _fotosBase64
            };

            var json    = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var resp = await ApiClient.Http.PostAsync("api/mantenimiento-movil/ejecutar", content);
            var body = await resp.Content.ReadAsStringAsync();

            if (resp.IsSuccessStatusCode)
            {
                await DisplayAlert("✅ Éxito", "Trabajo registrado correctamente.", "OK");
                await Shell.Current.GoToAsync("..");
            }
            else
            {
                await DisplayAlert("Error", $"No se pudo registrar: {body}", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error de red: {ex.Message}", "OK");
        }
        finally
        {
            btnEnviar.IsEnabled    = true;
            actIndicator.IsVisible = false;
            actIndicator.IsRunning = false;
        }
    }

    private async void OnVolverClicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("..");
}
