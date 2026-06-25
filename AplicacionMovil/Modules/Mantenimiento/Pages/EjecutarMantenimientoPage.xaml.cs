using AplicacionMovil.Services;
using Microsoft.Maui.Devices.Sensors;
using System.Text;
using System.Text.Json;

namespace AplicacionMovil.Modules.Mantenimiento.Pages;

[QueryProperty(nameof(OtId), "OtId")]
[QueryProperty(nameof(TipoMantenimiento), "TipoMantenimiento")]
[QueryProperty(nameof(CodigoOT), "CodigoOT")]
[QueryProperty(nameof(Nombre), "Nombre")]
[QueryProperty(nameof(ReferenciaUbicacion), "ReferenciaUbicacion")]
[QueryProperty(nameof(DefinicionProblema), "DefinicionProblema")]
public partial class EjecutarMantenimientoPage : ContentPage
{
    public int OtId { get; set; }
    public string TipoMantenimiento { get; set; } = "";
    public string CodigoOT { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string ReferenciaUbicacion { get; set; } = "";
    public string DefinicionProblema { get; set; } = "";

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
    }

    private async void OnObtenerGpsClicked(object sender, EventArgs e)
    {
        try
        {
            btnGps.IsEnabled = false;
            btnGps.Text      = "📍 Obteniendo...";

            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();

            if (status != PermissionStatus.Granted)
            {
                await DisplayAlert("Permiso denegado", "Se necesita permiso de ubicación.", "OK");
                return;
            }

            lblGps.Text      = "Obteniendo GPS...";
            lblGps.TextColor = Color.FromArgb("#0EA5E9");

            var request  = new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(15));
            var location = await Geolocation.Default.GetLocationAsync(request);

            if (location == null)
            {
                lblGps.Text        = "No se pudo obtener GPS. Intente de nuevo.";
                lblGps.TextColor   = Color.FromArgb("#DC2626");
                lblPrecision.Text  = "";
                return;
            }

            _gpsLat       = location.Latitude;
            _gpsLon       = location.Longitude;
            _gpsPrecision = location.Accuracy;

            lblGps.Text       = $"📌 {_gpsLat.Value:F6}, {_gpsLon.Value:F6}";
            lblGps.TextColor  = Color.FromArgb("#4C1D95");
            lblPrecision.Text = $"📏 Precisión: {_gpsPrecision?.ToString("F1") ?? "?"} m";

            if (_gpsPrecision > 30)
                await DisplayAlert("Precisión baja",
                    $"La precisión es de {_gpsPrecision:F1}m. Se recomienda menos de 30m.", "OK");
        }
        catch (Exception ex)
        {
            lblGps.Text      = $"Error GPS: {ex.Message}";
            lblGps.TextColor = Color.FromArgb("#DC2626");
        }
        finally
        {
            btnGps.IsEnabled = true;
            btnGps.Text      = "📍 Mi GPS";
        }
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
