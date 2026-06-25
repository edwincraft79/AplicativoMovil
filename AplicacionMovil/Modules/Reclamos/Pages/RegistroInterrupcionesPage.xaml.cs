using AplicacionMovil.Services;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Media;
using Microsoft.Maui.Storage;
using SkiaSharp;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using AplicacionMovil.Modules.Reclamos.Data;
using AplicacionMovil.Modules.Reclamos.Models;
using AplicacionMovil.Core.Models;

namespace AplicacionMovil.Modules.Reclamos.Pages
{
    [QueryProperty(nameof(CodigoReclamo), nameof(CodigoReclamo))]
    [QueryProperty(nameof(Sucursal), nameof(Sucursal))]
    [QueryProperty(nameof(TipoReclamo), nameof(TipoReclamo))]
    [QueryProperty(nameof(NombreReclamante), nameof(NombreReclamante))]
    [QueryProperty(nameof(Telefono), nameof(Telefono))]
    [QueryProperty(nameof(Descripcion), nameof(Descripcion))]
    [QueryProperty(nameof(LatitudStr), "Latitud")]
    [QueryProperty(nameof(LongitudStr), "Longitud")]

    public partial class RegistroInterrupcionesPage : ContentPage
    {
        public string CodigoReclamo { get; set; }
        public string Sucursal { get; set; }
        public string TipoReclamo { get; set; }
        public string NombreReclamante { get; set; }
        public string Telefono { get; set; }
        public string Descripcion { get; set; }
        public string LatitudStr { get; set; }
        public string LongitudStr { get; set; }

        private double? _latitud;
        private double? _longitud;
        private const string API_URL = "https://elpuregistro.com/api/InterrupcionesMovil/registrar";
        private const double PrecisionObjetivoMetros = 30.0;
        private const long MaxPhotoSizeBytes = 4 * 1024 * 1024;
        private readonly HttpClient _http = new();
        private byte[] _fotoBytes;
        private string? _fotoBase64;
        private string _fotoNombre;
        private double? _latitudActual;
        private double? _longitudActual;
        private double? _precisionActual;
        private string? _rutaFotoLocal;
        private const string API_BASE = "https://elpuregistro.com";
        private readonly DatabaseService _db = new DatabaseService();

        public RegistroInterrupcionesPage()
        {
            InitializeComponent();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            try
            {
                txtCodigoReclamo.Text = CodigoReclamo ?? string.Empty;
                txtSucursal.Text = Sucursal ?? string.Empty;
                txtDescripcion.Text = Descripcion ?? string.Empty;
                txtNombreReclamante.Text = NombreReclamante ?? string.Empty;
                txtTelefono.Text = Telefono ?? string.Empty;

                _latitud = null;
                _longitud = null;

                if (!string.IsNullOrWhiteSpace(LatitudStr) &&
                    double.TryParse(LatitudStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var lat))
                {
                    _latitud = lat;
                }

                if (!string.IsNullOrWhiteSpace(LongitudStr) &&
                    double.TryParse(LongitudStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var lng))
                {
                    _longitud = lng;
                }

                if (_latitud.HasValue && _longitud.HasValue)
                {
                    lblGpsReclamo.Text = $"{_latitud.Value}, {_longitud.Value}";
                }
                else
                {
                    lblGpsReclamo.Text = "Sin coordenadas del reclamo";
                }

                var ahora = DateTime.Now;
                dpFecha.Date = ahora.Date;
                tpHora.Time = ahora.TimeOfDay;

                PreseleccionarClasificacionDesdeTipo();
            }
            catch
            {
            }
        }

        private void PreseleccionarClasificacionDesdeTipo()
        {
            if (string.IsNullOrWhiteSpace(TipoReclamo))
            {
                pkClasificacion.SelectedIndex = -1;
                MostrarPanelesPorClasificacion();
                return;
            }

            var tr = TipoReclamo.ToLowerInvariant();

            if (tr.Contains("interrupción") || tr.Contains("interrupcion"))
            {
                pkClasificacion.SelectedIndex = 0;
            }
            else if (tr.Contains("alumbrado"))
            {
                pkClasificacion.SelectedIndex = 1;
            }
            else if (tr.Contains("riesgo"))
            {
                pkClasificacion.SelectedIndex = 2;
            }
            else
            {
                pkClasificacion.SelectedIndex = -1;
            }

            MostrarPanelesPorClasificacion();
        }

        private void OnClasificacionChanged(object sender, EventArgs e)
        {
            MostrarPanelesPorClasificacion();
        }

        private void MostrarPanelesPorClasificacion()
        {
            panelInterrupcion.IsVisible = false;
            panelAlumbrado.IsVisible = false;
            panelRiesgo.IsVisible = false;

            switch (pkClasificacion.SelectedIndex)
            {
                case 0:
                    panelInterrupcion.IsVisible = true;
                    break;
                case 1:
                    panelAlumbrado.IsVisible = true;
                    break;
                case 2:
                    panelRiesgo.IsVisible = true;
                    break;
            }
        }

        private async void OnTomarFotoClicked(object sender, EventArgs e)
        {
            try
            {
                btnTomarFoto.IsEnabled = false;
                btnTomarFoto.Text = "📸 Abriendo cámara...";

                if (!await AsegurarPermisoCamaraAsync())
                    return;

                FileResult photo;
                try
                {
                    photo = await MediaPicker.Default.CapturePhotoAsync();
                }
                catch
                {
                    return;
                }

                if (photo == null)
                    return;

                btnTomarFoto.Text = "⏳ Procesando foto...";

                using var stream = await photo.OpenReadAsync();
                var bytesOptimizada = await RedimensionarYComprimirFotoAsync(stream);
                var bytesConFecha = AgregarFechaHora(bytesOptimizada);

                _fotoBytes = bytesConFecha;
                _fotoBase64 = Convert.ToBase64String(_fotoBytes);

                imgFoto.Source = ImageSource.FromStream(() => new MemoryStream(_fotoBytes));
                lblFotoSize.Text = $"Foto lista ({_fotoBytes.Length / 1024} KB)";

                btnTomarFoto.Text = "✓ Foto capturada";
                await Task.Delay(1500);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"No se pudo procesar la foto: {ex.Message}", "OK");
            }
            finally
            {
                btnTomarFoto.IsEnabled = true;
                btnTomarFoto.Text = "📸 Tomar Foto";
            }
        }

        private async void OnObtenerGpsClicked(object sender, EventArgs e)
        {
            const double objetivoPrecision = 30.0;
            const int maxIntentos = 5;

            try
            {
                var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
                if (status != PermissionStatus.Granted)
                {
                    status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                    if (status != PermissionStatus.Granted)
                    {
                        await DisplayAlert("Permisos",
                            "Se necesita permiso de ubicación para obtener el GPS.", "OK");
                        return;
                    }
                }

                btnGps.IsEnabled = false;
                btnGps.Text = "📍 Obteniendo ubicación...";
                lblPrecision.Text = "Buscando ubicación...";
                lblCoords.Text = "Obteniendo coordenadas, espere por favor...";

                Location mejorLocation = null;

                for (int intento = 1; intento <= maxIntentos; intento++)
                {
                    var request = new GeolocationRequest(
                        GeolocationAccuracy.Best,
                        TimeSpan.FromSeconds(8));

                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

                    var location = await Geolocation.GetLocationAsync(request, cts.Token);
                    if (location == null)
                    {
                        await Task.Delay(2000);
                        continue;
                    }

                    mejorLocation = location;

                    _latitudActual = location.Latitude;
                    _longitudActual = location.Longitude;
                    _precisionActual = location.Accuracy;

                    lblCoords.Text = $"Lat: {_latitudActual:F6}, Lon: {_longitudActual:F6}";
                    lblPrecision.Text =
                        $"Precisión: ±{_precisionActual:F1} m (intento {intento}/{maxIntentos})";

                    if (_precisionActual <= objetivoPrecision)
                        break;

                    await Task.Delay(2000);
                }

                if (mejorLocation == null)
                {
                    await DisplayAlert("GPS", "No se pudo obtener la ubicación.", "OK");
                    lblPrecision.Text = "Precisión: --";
                    lblCoords.Text = "Lat/Lon aún no guardadas";
                    return;
                }

                if (_precisionActual <= objetivoPrecision)
                {
                    await DisplayAlert("GPS",
                        $"Ubicación lista con precisión de ±{_precisionActual:F1} m.",
                        "OK");
                }
                else
                {
                    await DisplayAlert("GPS",
                        $"Ubicación obtenida con precisión de ±{_precisionActual:F1} m " +
                        $"(objetivo ≤ {objetivoPrecision} m).",
                        "OK");
                }
            }
            catch (FeatureNotSupportedException)
            {
                await DisplayAlert("GPS", "El dispositivo no soporta GPS.", "OK");
            }
            catch (PermissionException)
            {
                await DisplayAlert("Permisos",
                    "No hay permisos para acceder a la ubicación.", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error",
                    $"No se pudo obtener el GPS: {ex.Message}", "OK");
            }
            finally
            {
                btnGps.IsEnabled = true;
                btnGps.Text = "📍 Obtener Ubicación";
            }
        }

        private async void OnEnviarClicked(object sender, EventArgs e)
        {
            try
            {
                btnEnviar.IsEnabled = false;
                btnEnviar.Text = "⏳ Validando...";

                if (string.IsNullOrWhiteSpace(txtCodigoReclamo.Text))
                {
                    await DisplayAlert("Aviso", "Ingrese el código de reclamo.", "OK");
                    return;
                }

                var usuario = SesionMovil.Usuario;
                if (string.IsNullOrWhiteSpace(usuario))
                {
                    await DisplayAlert("Aviso", "No se encontró el usuario logueado.", "OK");
                    return;
                }

                bool esDesestimado = chkDesestimado.IsChecked;

                var fechaSeleccionada = dpFecha.Date;
                var horaSeleccionada = tpHora.Time;
                var fechaEjecucion = fechaSeleccionada + horaSeleccionada;
                fechaEjecucion = DateTime.SpecifyKind(fechaEjecucion, DateTimeKind.Local);

                if (string.IsNullOrWhiteSpace(txtDescripcionSol.Text))
                {
                    var msg = esDesestimado
                        ? "Indique la razón del desestimado en la descripción."
                        : "Ingrese la descripción de la solución.";
                    await DisplayAlert("Aviso", msg, "OK");
                    return;
                }

                if (esDesestimado)
                {
                }
                else
                {
                    if (pkClasificacion.SelectedItem == null)
                    {
                        await DisplayAlert("Aviso", "Seleccione la clasificación del reclamo.", "OK");
                        return;
                    }

                    if (panelInterrupcion.IsVisible)
                    {
                        if (pkNaturaleza.SelectedItem == null)
                        {
                            await DisplayAlert("Aviso", "Seleccione la naturaleza de la interrupción.", "OK");
                            return;
                        }
                        if (pkEquipo.SelectedItem == null)
                        {
                            await DisplayAlert("Aviso", "Seleccione el equipo de reposición.", "OK");
                            return;
                        }
                        if (pkFases.SelectedItem == null)
                        {
                            await DisplayAlert("Aviso", "Seleccione la fase de reposición.", "OK");
                            return;
                        }
                    }

                    if (panelAlumbrado.IsVisible && pkAlumbrado.SelectedItem == null)
                    {
                        await DisplayAlert("Aviso", "Seleccione el detalle de alumbrado público.", "OK");
                        return;
                    }

                    if (panelRiesgo.IsVisible && pkRiesgo.SelectedItem == null)
                    {
                        await DisplayAlert("Aviso", "Seleccione el detalle del riesgo eléctrico.", "OK");
                        return;
                    }

                    if (_latitudActual == null || _longitudActual == null)
                    {
                        await DisplayAlert("Aviso", "Debe obtener primero el GPS.", "OK");
                        return;
                    }

                    if (_fotoBytes == null || _fotoBytes.Length == 0)
                    {
                        await DisplayAlert("Aviso", "Debe tomar una foto de campo antes de enviar.", "OK");
                        return;
                    }
                }

                string? fotoBase64 = _fotoBase64;

                var modelo = new InterrupcionMovilRequest
                {
                    CodigoReclamo = txtCodigoReclamo.Text.Trim(),
                    Usuario = usuario,
                    FechaHoraAtencion = fechaEjecucion,

                    ClasificacionReclamo = pkClasificacion.SelectedItem?.ToString() ?? "",
                    NaturalezaInterrupcion = panelInterrupcion.IsVisible ? pkNaturaleza.SelectedItem?.ToString() ?? "" : "",
                    EquipoReposicion = panelInterrupcion.IsVisible ? pkEquipo.SelectedItem?.ToString() ?? "" : "",
                    FasesReposicion = panelInterrupcion.IsVisible ? pkFases.SelectedItem?.ToString() ?? "" : "",
                    DetalleAlumbrado = panelAlumbrado.IsVisible ? pkAlumbrado.SelectedItem?.ToString() ?? "" : "",
                    DetalleRiesgo = panelRiesgo.IsVisible ? pkRiesgo.SelectedItem?.ToString() ?? "" : "",
                    DescripcionSolucion = txtDescripcionSol.Text ?? "",

                    FotoBase64 = fotoBase64,
                    Latitud = _latitudActual,
                    Longitud = _longitudActual,
                    PrecisionGps = _precisionActual,

                    Desestimado = esDesestimado,
                };

                btnEnviar.Text = "⏳ Enviando al servidor...";
                var url = API_URL;
                var json = JsonSerializer.Serialize(modelo);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                using var http = new HttpClient();
                var response = await http.PostAsync(url, content);
                var body = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    btnEnviar.Text = "✓ Enviado con éxito";

                    var registroHistorial = new InterrupcionRegistro
                    {
                        CodigoReclamo = txtCodigoReclamo.Text?.Trim() ?? string.Empty,
                        Usuario = usuario,
                        Fecha = fechaEjecucion,
                        ClasificacionReclamo = pkClasificacion.SelectedItem?.ToString() ?? string.Empty,
                        NaturalezaInterrupcion = panelInterrupcion.IsVisible ? pkNaturaleza.SelectedItem?.ToString() ?? "" : "",
                        EquipoReposicion = panelInterrupcion.IsVisible ? pkEquipo.SelectedItem?.ToString() ?? "" : "",
                        FasesReposicion = panelInterrupcion.IsVisible ? pkFases.SelectedItem?.ToString() ?? "" : "",
                        DetalleAlumbrado = panelAlumbrado.IsVisible ? pkAlumbrado.SelectedItem?.ToString() ?? "" : "",
                        DetalleRiesgo = panelRiesgo.IsVisible ? pkRiesgo.SelectedItem?.ToString() ?? "" : "",
                        DescripcionSolucion = txtDescripcionSol.Text?.Trim() ?? string.Empty,
                        FotoBytes = _fotoBytes,
                        FotoBase64 = _fotoBase64,
                        Latitud = _latitudActual,
                        Longitud = _longitudActual,
                        PrecisionGps = _precisionActual,
                        Enviado = true
                    };

                    await GuardarLocalAsync(enviado: true);

                    // ✅ ELIMINAR DE ACTIVIDADES PENDIENTES
                    await _db.EliminarReclamoPendienteAsync(txtCodigoReclamo.Text.Trim());

                    var mensaje = chkDesestimado.IsChecked
                        ? "La información fue enviada correctamente y la OT fue marcada como DESESTIMADA. Se eliminó de actividades pendientes."
                        : "La información fue enviada correctamente y la OT fue marcada como ATENDIDA. Se eliminó de actividades pendientes.";

                    await DisplayAlert("Atención Registrada", mensaje, "OK");
                    await Navigation.PopAsync();
                }
                else
                {
                    await DisplayAlert(
                        "Error del servidor",
                        $"Código: {(int)response.StatusCode} ({response.StatusCode})\n\n{body}",
                        "OK");
                }
            }
            catch (HttpRequestException ex)
            {
                await DisplayAlert("Conexión",
                    $"No se pudo conectar al servidor.\nDetalle: {ex.Message}",
                    "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
            finally
            {
                btnEnviar.IsEnabled = true;
                btnEnviar.Text = "✓ Enviar";
            }
        }

        private async Task<int> GuardarLocalAsync(bool enviado)
        {
            _rutaFotoLocal = null;

            if (_fotoBytes != null && _fotoBytes.Length > 0)
            {
                var fotosDir = Path.Combine(FileSystem.AppDataDirectory, "reclamos_fotos");
                Directory.CreateDirectory(fotosDir);

                var cod = (txtCodigoReclamo.Text ?? "SINCOD").Trim();
                var fotoPath = Path.Combine(fotosDir, $"rec_{cod}_{DateTime.Now:yyyyMMdd_HHmmss}.jpg");
                await File.WriteAllBytesAsync(fotoPath, _fotoBytes);
                _rutaFotoLocal = fotoPath;
            }

            var fechaAtencion = dpFecha.Date + tpHora.Time;
            fechaAtencion = DateTime.SpecifyKind(fechaAtencion, DateTimeKind.Local);

            var ejecucion = new EjecucionReclamoOt
            {
                CodigoReclamo = (txtCodigoReclamo.Text ?? "").Trim(),
                Usuario = SesionMovil.Usuario ?? "",
                FechaHoraAtencion = fechaAtencion,

                ClasificacionReclamo = pkClasificacion.SelectedItem?.ToString() ?? "",
                NaturalezaInterrupcion = panelInterrupcion.IsVisible ? pkNaturaleza.SelectedItem?.ToString() ?? "" : "",
                EquipoReposicion = panelInterrupcion.IsVisible ? pkEquipo.SelectedItem?.ToString() ?? "" : "",
                FasesReposicion = panelInterrupcion.IsVisible ? pkFases.SelectedItem?.ToString() ?? "" : "",
                DetalleAlumbrado = panelAlumbrado.IsVisible ? pkAlumbrado.SelectedItem?.ToString() ?? "" : "",
                DetalleRiesgo = panelRiesgo.IsVisible ? pkRiesgo.SelectedItem?.ToString() ?? "" : "",

                DescripcionSolucion = txtDescripcionSol.Text?.Trim() ?? "",

                Latitud = _latitudActual,
                Longitud = _longitudActual,
                PrecisionGps = _precisionActual,

                Desestimado = chkDesestimado.IsChecked,

                UsuarioEjecucion = Preferences.Get("UserName", SesionMovil.Usuario ?? ""),
                FechaCreacion = DateTime.Now,
                Sincronizado = enviado
            };

            var id = await _db.GuardarReclamoOtOfflineAsync(ejecucion);

            if (!string.IsNullOrWhiteSpace(_rutaFotoLocal))
                await _db.GuardarFotoReclamoOtAsync(id, _rutaFotoLocal);

            return id;
        }

        private async void OnGuardarClicked(object sender, EventArgs e)
        {
            try
            {
                btnGuardar.IsEnabled = false;
                btnGuardar.Text = "⏳ Guardando...";

                if (string.IsNullOrWhiteSpace(txtCodigoReclamo.Text))
                {
                    await DisplayAlert("Validación", "No se encontró el código de reclamo.", "OK");
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtDescripcionSol.Text))
                {
                    await DisplayAlert("Validación", "Debe ingresar la descripción.", "OK");
                    return;
                }

                if (pkClasificacion.SelectedItem == null)
                {
                    await DisplayAlert("Validación", "Seleccione la clasificación.", "OK");
                    return;
                }

                var id = await GuardarLocalAsync(enviado: false);

                btnGuardar.Text = "✓ Guardado";
                await DisplayAlert("Guardado", $"Registro guardado offline (Id={id}).", "OK");

                // ← AGREGAR esta línea para volver al listado y forzar refresh:
                await Shell.Current.GoToAsync("//MisOtPage?refresh=1");

            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"No se pudo guardar offline.\n{ex.Message}", "OK");
            }
            finally
            {
                btnGuardar.IsEnabled = true;
                btnGuardar.Text = "💾 Guardar Offline";
            }
        }

        private async void OnHistorialClicked(object sender, EventArgs e)
        {
            try
            {
                await Shell.Current.GoToAsync("HistorialPage");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error",
                    $"No se pudo abrir el historial: {ex.Message}", "OK");
            }
        }

        private async Task<bool> AsegurarPermisoCamaraAsync()
        {
            var status = await Permissions.CheckStatusAsync<Permissions.Camera>();

            if (status != PermissionStatus.Granted)
            {
                if (status == PermissionStatus.Denied &&
                    DeviceInfo.Platform == DevicePlatform.Android &&
                    Permissions.ShouldShowRationale<Permissions.Camera>())
                {
                    await DisplayAlert(
                        "Permisos",
                        "La aplicación necesita usar la cámara para adjuntar la foto de campo.",
                        "OK");
                }

                status = await Permissions.RequestAsync<Permissions.Camera>();
            }

            if (status != PermissionStatus.Granted)
            {
                await DisplayAlert("Permisos",
                    "No hay permisos para usar la cámara.",
                    "OK");
                return false;
            }

            return true;
        }

        private async Task<bool> AsegurarPermisoUbicacionAsync()
        {
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();

            if (status != PermissionStatus.Granted)
            {
                if (status == PermissionStatus.Denied &&
                    DeviceInfo.Platform == DevicePlatform.Android &&
                    Permissions.ShouldShowRationale<Permissions.LocationWhenInUse>())
                {
                    await DisplayAlert(
                        "Permisos",
                        "La aplicación necesita acceso a la ubicación para registrar el GPS de la atención.",
                        "OK");
                }

                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            }

            if (status != PermissionStatus.Granted)
            {
                await DisplayAlert("Permisos",
                    "No hay permisos para acceder a la ubicación.",
                    "OK");
                return false;
            }

            return true;
        }

        private async Task<byte[]> RedimensionarYComprimirFotoAsync(Stream originalStream)
        {
            try
            {
                using var inputStream = new MemoryStream();
                await originalStream.CopyToAsync(inputStream);
                inputStream.Position = 0;

                using var skBitmap = SKBitmap.Decode(inputStream);

                if (skBitmap == null)
                    throw new Exception("Error al decodificar la imagen");

                int maxWidth = 1280;
                int maxHeight = 1280;

                float ratioX = (float)maxWidth / skBitmap.Width;
                float ratioY = (float)maxHeight / skBitmap.Height;
                float ratio = Math.Min(ratioX, ratioY);

                int newWidth = (int)(skBitmap.Width * ratio);
                int newHeight = (int)(skBitmap.Height * ratio);

                using var resizedBitmap = skBitmap.Resize(
                    new SKImageInfo(newWidth, newHeight),
                    SKFilterQuality.Medium);

                if (resizedBitmap == null)
                    throw new Exception("Error al redimensionar la imagen");

                using var image = SKImage.FromBitmap(resizedBitmap);

                var data = image.Encode(SKEncodedImageFormat.Jpeg, 70);

                return data.ToArray();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"No se pudo comprimir la foto: {ex.Message}", "OK");
                return null;
            }
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("..");
        }

        private byte[] AgregarFechaHora(byte[] inputBytes)
        {
            using var ms = new MemoryStream(inputBytes);
            using var bitmap = SKBitmap.Decode(ms);

            var info = new SKImageInfo(bitmap.Width, bitmap.Height);
            using var surface = SKSurface.Create(info);
            var canvas = surface.Canvas;

            using (var img = SKImage.FromBitmap(bitmap))
                canvas.DrawImage(img, 0, 0);

            string texto = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
            float textSize = bitmap.Width / 25f;

            var paint = new SKPaint
            {
                Color = SKColors.White,
                TextSize = textSize,
                IsAntialias = true,
                Typeface = SKTypeface.FromFamilyName("Arial"),
            };

            var paintFondo = new SKPaint
            {
                Color = new SKColor(0, 0, 0, 160),
                Style = SKPaintStyle.Fill
            };

            var bounds = new SKRect();
            paint.MeasureText(texto, ref bounds);

            float margin = 20;
            float x = margin;
            float y = bitmap.Height - margin;

            var rect = new SKRect(
                x - 10,
                y + bounds.Top - 10,
                x + bounds.Width + 10,
                y + bounds.Bottom + 10
            );

            canvas.DrawRect(rect, paintFondo);
            canvas.DrawText(texto, x, y, paint);

            canvas.Flush();

            using var output = new MemoryStream();
            using var finalImage = surface.Snapshot();
            using var data = finalImage.Encode(SKEncodedImageFormat.Jpeg, 80);

            data.SaveTo(output);
            return output.ToArray();
        }

        private void OnDesestimadoChanged(object sender, CheckedChangedEventArgs e)
        {
            bool desestimado = e.Value;

            panelInterrupcion.IsEnabled = !desestimado;
            panelAlumbrado.IsEnabled = !desestimado;
            panelRiesgo.IsEnabled = !desestimado;

            if (desestimado)
            {
                pkNaturaleza.SelectedIndex = -1;
                pkEquipo.SelectedIndex = -1;
                pkFases.SelectedIndex = -1;
                pkAlumbrado.SelectedIndex = -1;
                pkRiesgo.SelectedIndex = -1;
            }
        }
    }
}
