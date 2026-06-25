using AplicacionMovil.Core.Models;
using AplicacionMovil.Services;
using System.Globalization;
using AplicacionMovil.Modules.Deficiencias.Models;
using AplicacionMovil.Modules.Deficiencias.Data;

namespace AplicacionMovil.Modules.Deficiencias.Pages;

public partial class RegistroInspeccionDefPage : ContentPage
{
    private readonly DatabaseService _db = new();
    private readonly ApiService _api = new();

    private decimal? _lat;
    private decimal? _lon;
    private decimal? _prec;
    private string? _rutaFotoLocal;
    private byte[]? _fotoBytes;   // ✅ bytes en memoria para mostrar la imagen

    public RegistroInspeccionDefPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        CargarPreferencias();
    }

    private void CargarPreferencias()
    {
        var unidadZonalGuardada = Preferences.Get("UnidadZonal", string.Empty);
        if (!string.IsNullOrEmpty(unidadZonalGuardada))
        {
            var index = pickerUnidadZonal.Items.IndexOf(unidadZonalGuardada);
            if (index >= 0)
                pickerUnidadZonal.SelectedIndex = index;
        }
        pickerCodigoDenunciante.SelectedIndex = 0;
    }

    private void OnUnidadZonalChanged(object sender, EventArgs e)
    {
        if (pickerUnidadZonal.SelectedIndex >= 0)
            Preferences.Set("UnidadZonal", pickerUnidadZonal.Items[pickerUnidadZonal.SelectedIndex]);
    }

    // ─── GPS ───────────────────────────────────────────────────────────────────
    private async void OnGpsClicked(object sender, EventArgs e)
    {
        try
        {
            btnGps.IsEnabled = false;
            btnGps.Text = "📍 Obteniendo ubicación...";

            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();

            if (status != PermissionStatus.Granted)
            {
                await DisplayAlert("Permiso denegado", "Se necesita permiso de ubicación.", "OK");
                return;
            }

            var req = new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(15));
            var loc = await Geolocation.Default.GetLocationAsync(req);

            if (loc == null)
            {
                lblGps.Text = "No se pudo obtener ubicación.";
                lblPrecision.Text = "📏 Precisión: --";
                return;
            }

            _lat = (decimal)loc.Latitude;
            _lon = (decimal)loc.Longitude;
            _prec = (decimal)(loc.Accuracy ?? 0);

            lblPrecision.Text = $"📏 Precisión: {_prec:F1} metros";
            lblGps.Text = $"📌 {_lat:F6}, {_lon:F6}";

            if (_prec > 30)
                await DisplayAlert("Precisión baja",
                    $"La precisión es de {_prec:F1}m. Se recomienda menos de 30m.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("GPS", ex.Message, "OK");
        }
        finally
        {
            btnGps.IsEnabled = true;
            btnGps.Text = "📍 Obtener GPS";
        }
    }

    private bool TryGetInt(string s, out int v) => int.TryParse((s ?? "").Trim(), out v);

    private bool TryGetDecimal(string s, out decimal v)
    {
        s = (s ?? "").Trim();
        if (string.IsNullOrWhiteSpace(s)) { v = 0; return false; }
        if (s.Contains(',') && !s.Contains('.')) s = s.Replace(',', '.');
        return decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out v);
    }

    // ─── TOMAR FOTO ────────────────────────────────────────────────────────────
    private async void OnTomarFotoClicked(object sender, EventArgs e)
    {
        try
        {
            var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
            if (status != PermissionStatus.Granted)
                status = await Permissions.RequestAsync<Permissions.Camera>();

            if (status != PermissionStatus.Granted)
            {
                await DisplayAlert("Permiso denegado",
                    "Se necesita permiso de cámara para tomar fotos.", "OK");
                return;
            }

            var photo = await MediaPicker.Default.CapturePhotoAsync(
                new MediaPickerOptions { Title = "Foto de inspección" });

            if (photo == null) return;

            // ✅ 1. Leer TODO en MemoryStream (evita que el FileStream se cierre antes de renderizar)
            using var readStream = await photo.OpenReadAsync();
            using var ms = new MemoryStream();
            await readStream.CopyToAsync(ms);
            _fotoBytes = ms.ToArray();

            // Verificar tamaño máximo 4 MB
            var sizeMB = _fotoBytes.Length / 1024.0 / 1024.0;
            if (sizeMB > 4)
            {
                await DisplayAlert("Foto muy grande",
                    $"La foto pesa {sizeMB:F2} MB. El máximo es 4 MB.", "OK");
                _fotoBytes = null;
                return;
            }

            // ✅ 2. Guardar en disco desde los bytes ya cargados
            var ext = Path.GetExtension(photo.FileName);
            if (string.IsNullOrWhiteSpace(ext)) ext = ".jpg";
            var fileName = $"insp_{DateTime.Now:yyyyMMdd_HHmmss}{ext}";
            var newFile = Path.Combine(FileSystem.AppDataDirectory, fileName);
            await File.WriteAllBytesAsync(newFile, _fotoBytes);
            _rutaFotoLocal = newFile;

            // ✅ 3. Mostrar desde archivo en disco — más confiable en Android que FromStream
            imgPreview.Source = null;
            await Task.Delay(80);
            imgPreview.Source = ImageSource.FromFile(newFile);
            imgPreview.IsVisible = true;

            lblFotoInfo.Text = $"📁 {sizeMB:F2} MB";
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    // ─── GUARDAR OFFLINE ───────────────────────────────────────────────────────
    private async void OnGuardarOfflineClicked(object sender, EventArgs e)
    {
        if (pickerUnidadZonal.SelectedIndex < 0)
        { await DisplayAlert("Validación", "Debe seleccionar Unidad Zonal.", "OK"); return; }

        if (pickerCodigoDenunciante.SelectedIndex < 0)
        { await DisplayAlert("Validación", "Debe seleccionar Código Denunciante.", "OK"); return; }

        if (pickerNivelTension.SelectedIndex < 0)
        { await DisplayAlert("Validación", "Debe seleccionar Nivel de Tensión.", "OK"); return; }

        var unidadZonal = pickerUnidadZonal.Items[pickerUnidadZonal.SelectedIndex];
        var codigoDenunciante = pickerCodigoDenunciante.SelectedIndex + 1;
        var nivelTension = pickerNivelTension.Items[pickerNivelTension.SelectedIndex];

        var ins = new InspeccionDeficiencia
        {
            UnidadZonal = unidadZonal,
            CodigoDenunciante = codigoDenunciante,
            NivelTension = nivelTension,
            CodigoTipificacion = null,
            Observaciones = txtObservaciones.Text,
            FechaInspeccion = DateTime.Now,
            UtmEste = 0,
            UtmNorte = 0,
            Latitud = _lat ?? 0,
            Longitud = _lon ?? 0,
            PrecisionGpsM = _prec,
            RutaFoto = _rutaFotoLocal,
            Sincronizado = false
        };

        await _db.GuardarInspeccionAsync(ins);
        await DisplayAlert("OK", "Inspección guardada offline.", "OK");
    }

    // ─── ENVIAR ────────────────────────────────────────────────────────────────
    private async void OnEnviarClicked(object sender, EventArgs e)
    {
        if (pickerUnidadZonal.SelectedIndex < 0)
        { await DisplayAlert("Validación", "Debe seleccionar Unidad Zonal.", "OK"); return; }

        if (pickerCodigoDenunciante.SelectedIndex < 0)
        { await DisplayAlert("Validación", "Debe seleccionar Código Denunciante.", "OK"); return; }

        if (pickerNivelTension.SelectedIndex < 0)
        { await DisplayAlert("Validación", "Debe seleccionar Nivel de Tensión.", "OK"); return; }

        if (!_lat.HasValue || !_lon.HasValue)
        { await DisplayAlert("Validación", "Primero obtén el GPS antes de enviar.", "OK"); return; }

        var unidadZonal = pickerUnidadZonal.Items[pickerUnidadZonal.SelectedIndex];
        var codigoDenunciante = pickerCodigoDenunciante.SelectedIndex + 1;
        var nivelTension = pickerNivelTension.Items[pickerNivelTension.SelectedIndex];

        var req = new InspeccionDefCreateRequest
        {
            UnidadZonal = unidadZonal,
            CodigoDenunciante = codigoDenunciante,
            NivelTension = nivelTension,
            CodigoTipificacion = null,
            Observaciones = txtObservaciones.Text,
            FechaInspeccion = DateTime.Now,
            UtmEste = 0,
            UtmNorte = 0,
            Latitud = _lat,
            Longitud = _lon,
            PrecisionGpsM = _prec,
            RutaFoto = _rutaFotoLocal
        };

        var (ok, msg, idServidor) = await _api.EnviarInspeccionAsync(req);

        if (!ok)
        { await DisplayAlert("Error", msg, "OK"); return; }

        await DisplayAlert("OK", $"Enviado. Id servidor: {idServidor}", "OK");
    }

    // ─── NAVEGACIÓN ────────────────────────────────────────────────────────────
    private async void OnBackClicked(object sender, EventArgs e)
    {
        try { await Shell.Current.GoToAsync(".."); }
        catch (Exception ex) { await DisplayAlert("Error", $"No se pudo volver: {ex.Message}", "OK"); }
    }

    private async void OnHistorialClicked(object sender, EventArgs e)
    {
        try { await Shell.Current.GoToAsync(nameof(HistorialDeficienciasPage)); }
        catch (Exception ex) { await DisplayAlert("Error", $"Error al abrir historial: {ex.Message}", "OK"); }
    }
}
