using AplicacionMovil.Modules.Deficiencias.Data;
using AplicacionMovil.Services;
using Microsoft.Maui.Media;
using System.Text.Json;
using System.Text;
using AplicacionMovil.Core.Models;

namespace AplicacionMovil.Modules.Deficiencias.Pages;

[QueryProperty(nameof(DeficienciaId), "DeficienciaId")]
[QueryProperty(nameof(CodigoDeficiencia), "CodigoDeficiencia")]
[QueryProperty(nameof(UnidadZonal), "UnidadZonal")]
[QueryProperty(nameof(Alimentador), "Alimentador")]
[QueryProperty(nameof(CodigoTipificacion), "CodigoTipificacion")]
[QueryProperty(nameof(TipificacionTexto), "TipificacionTexto")]
[QueryProperty(nameof(Prioridad), "Prioridad")]
[QueryProperty(nameof(EstadoSubsanacion), "EstadoSubsanacion")]
[QueryProperty(nameof(FechaDenuncia), "FechaDenuncia")]
[QueryProperty(nameof(Latitud), "Latitud")]
[QueryProperty(nameof(Longitud), "Longitud")]
public partial class RegistroDeficienciasPage : ContentPage
{
    // Propiedades de query parameters
    public string? DeficienciaId { get; set; }
    public string? CodigoDeficiencia { get; set; }
    public string? UnidadZonal { get; set; }
    public string? Alimentador { get; set; }
    public string? CodigoTipificacion { get; set; }
    public string? TipificacionTexto { get; set; }
    public string? Prioridad { get; set; }
    public string? EstadoSubsanacion { get; set; }
    public string? FechaDenuncia { get; set; }
    public string? Latitud { get; set; }
    public string? Longitud { get; set; }

    // Datos del formulario
    private byte[]? _fotoBytes;
    private double? _latitudSubsanacion;
    private double? _longitudSubsanacion;
    private readonly DatabaseService _db = new DatabaseService();

    public RegistroDeficienciasPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        CargarDatosDeficiencia();
    }

    private void CargarDatosDeficiencia()
    {
        // Datos de solo lectura
        txtDeficienciaId.Text = DeficienciaId ?? "";
        txtCodigoDeficiencia.Text = CodigoDeficiencia ?? "-";
        txtUnidadZonal.Text = UnidadZonal ?? "-";
        txtAlimentador.Text = Alimentador ?? "-";

        // Tipificación completa (código + texto)
        if (!string.IsNullOrWhiteSpace(CodigoTipificacion))
        {
            if (!string.IsNullOrWhiteSpace(TipificacionTexto))
            {
                txtTipificacion.Text = $"{CodigoTipificacion} - {TipificacionTexto}";
            }
            else
            {
                txtTipificacion.Text = CodigoTipificacion;
            }
        }
        else
        {
            txtTipificacion.Text = "-";
        }

        lblPrioridad.Text = Prioridad ?? "-";
        lblEstado.Text = MapearEstado(EstadoSubsanacion);

        // Fecha denuncia
        if (!string.IsNullOrWhiteSpace(FechaDenuncia) &&
            DateTime.TryParse(FechaDenuncia, out var fecha))
        {
            lblFechaDenuncia.Text = fecha.ToString("dd/MM/yyyy HH:mm");
        }
        else
        {
            lblFechaDenuncia.Text = "--/--/----";
        }

        // GPS de la deficiencia
        if (!string.IsNullOrWhiteSpace(Latitud) &&
            !string.IsNullOrWhiteSpace(Longitud) &&
            double.TryParse(Latitud, System.Globalization.NumberStyles.Any,
                          System.Globalization.CultureInfo.InvariantCulture, out var lat) &&
            double.TryParse(Longitud, System.Globalization.NumberStyles.Any,
                          System.Globalization.CultureInfo.InvariantCulture, out var lon))
        {
            lblGpsDeficiencia.Text = $"📍 {lat:F6}, {lon:F6}";
        }
        else
        {
            lblGpsDeficiencia.Text = "Sin coordenadas";
        }

        // Establecer fecha y hora actual para subsanación
        dpFechaSubsanacion.Date = DateTime.Today;
        tpHoraSubsanacion.Time = DateTime.Now.TimeOfDay;
    }

    private string MapearEstado(string? estado)
    {
        return (estado ?? "").Trim() switch
        {
            "0" => "Por subsanar",
            "1" => "Preventiva",
            "2" => "Definitiva",
            _ => "Sin estado"
        };
    }

    private async Task<int> GuardarLocalAsync()
    {
        // 1) Guardar foto en archivo
        var fotosDir = Path.Combine(FileSystem.AppDataDirectory, "deficiencias_fotos");
        Directory.CreateDirectory(fotosDir);

        var codigo = (txtCodigoDeficiencia.Text ?? "SINCOD").Trim();
        var fotoPath = Path.Combine(fotosDir, $"def_{codigo}_{DateTime.Now:yyyyMMdd_HHmmss}.jpg");
        await File.WriteAllBytesAsync(fotoPath, _fotoBytes!);

        // 2) Construir ejecución SQLite
        var ejecucion = new EjecucionDeficiencia
        {
            CodigoDeficiencia = codigo,
            IdDeficiencia = int.TryParse(txtDeficienciaId.Text, out var idDef) ? idDef : 0,
            IdUsuario = Preferences.Get("UserId", 0),

            FechaEjecucion = dpFechaSubsanacion.Date.Add(tpHoraSubsanacion.Time),
            FechaCreacion = DateTime.Now,

            Latitud = _latitudSubsanacion,
            Longitud = _longitudSubsanacion,
            Precision = null,

            Observaciones = txtActividadesSubsanacion.Text?.Trim() ?? "",
            EstadoSubsanacion = "Ejecutado",

            UsuarioEjecucion = Preferences.Get("UserName", SesionMovil.Usuario ?? ""),
            UnidadZonal = txtUnidadZonal.Text?.Trim() ?? "",
            Alimentador = txtAlimentador.Text?.Trim() ?? "",

            Sincronizado = false,
            Fotos = new List<string> { fotoPath }
        };

        var id = await _db.GuardarEjecucionOfflineAsync(ejecucion);
        return id;
    }

    private async void OnTomarFotoClicked(object sender, EventArgs e)
    {
        try
        {
            // Solicitar permisos
            var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.Camera>();
                if (status != PermissionStatus.Granted)
                {
                    await DisplayAlert("Permiso denegado",
                        "Se necesita permiso de cámara para tomar fotos.", "OK");
                    return;
                }
            }

            // Verificar disponibilidad
            if (!MediaPicker.Default.IsCaptureSupported)
            {
                await DisplayAlert("No disponible",
                    "La cámara no está disponible en este dispositivo.", "OK");
                return;
            }

            // Tomar foto
            var photo = await MediaPicker.Default.CapturePhotoAsync(new MediaPickerOptions
            {
                Title = "Foto de subsanación"
            });

            if (photo == null) return;

            // Leer bytes
            using var stream = await photo.OpenReadAsync();
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            _fotoBytes = memoryStream.ToArray();

            // Verificar tamaño
            var sizeMB = _fotoBytes.Length / 1024.0 / 1024.0;
            if (sizeMB > 4)
            {
                await DisplayAlert("Foto muy grande",
                    $"La foto pesa {sizeMB:F2} MB. El máximo es 4 MB.", "OK");
                _fotoBytes = null;
                return;
            }

            // Mostrar imagen
            imgFoto.Source = ImageSource.FromStream(() => new MemoryStream(_fotoBytes));
            lblFotoSize.Text = $"📁 {sizeMB:F2} MB";
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error",
                $"No se pudo tomar la foto: {ex.Message}", "OK");
        }
    }

    private async void OnObtenerGpsClicked(object sender, EventArgs e)
    {
        try
        {
            btnGps.IsEnabled = false;
            btnGps.Text = "📍 Obteniendo ubicación...";

            // Solicitar permisos
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                if (status != PermissionStatus.Granted)
                {
                    await DisplayAlert("Permiso denegado",
                        "Se necesita permiso de ubicación.", "OK");
                    return;
                }
            }

            // Obtener ubicación
            var request = new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(10));
            var location = await Geolocation.Default.GetLocationAsync(request);

            if (location != null)
            {
                _latitudSubsanacion = location.Latitude;
                _longitudSubsanacion = location.Longitude;

                var precision = location.Accuracy ?? 0;
                lblPrecision.Text = $"📏 Precisión: {precision:F1} metros";
                lblCoords.Text = $"📌 {_latitudSubsanacion:F6}, {_longitudSubsanacion:F6}";

                if (precision > 30)
                {
                    await DisplayAlert("Precisión baja",
                        $"La precisión es de {precision:F1}m. Se recomienda menos de 30m.", "OK");
                }
            }
            else
            {
                await DisplayAlert("Error", "No se pudo obtener la ubicación.", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error GPS",
                $"No se pudo obtener la ubicación: {ex.Message}", "OK");
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
            if (!ValidarFormulario()) return;

            btnEnviar.IsEnabled = false;
            btnEnviar.Text = "⏳ Guardando...";

            // 1) Guardar siempre local
            var idLocal = await GuardarLocalAsync();
            if (idLocal <= 0)
            {
                await DisplayAlert("Error", "No se pudo guardar localmente.", "OK");
                return;
            }

            btnEnviar.Text = "⏳ Enviando...";

            // 2) Intentar enviar usando el MISMO mecanismo del historial (rutas)
            var ejecucion = await _db.ObtenerEjecucionPorCodigoAsync((txtCodigoDeficiencia.Text ?? "").Trim());
            if (ejecucion == null)
            {
                await DisplayAlert("Error", "No se encontró el registro guardado.", "OK");
                return;
            }


            var fotosPaths = ejecucion.Fotos ?? new List<string>();

            var fotosBase64 = new List<string>();
            foreach (var path in fotosPaths)
            {
                if (!File.Exists(path)) continue;
                var bytes = await File.ReadAllBytesAsync(path);
                fotosBase64.Add(Convert.ToBase64String(bytes));
            }

            var fotos = await _db.ObtenerFotosPorEjecucionAsync(ejecucion.Id);
            ejecucion.Fotos = fotos.Select(f => f.RutaLocal).ToList();

            var api = new ApiService();
            var (ok, msg) = await api.EnviarSubsanacionAsync(ejecucion);

            if (ok)
            {
                await _db.MarcarEjecucionComoSincronizadaAsync(ejecucion.Id);

                await DisplayAlert("Listo", "Enviado y sincronizado.", "OK");
                await Shell.Current.GoToAsync("..", true, new Dictionary<string, object>
                {
                    ["Refresh"] = "1"
                });
            }
            else
            {
                await DisplayAlert("Error envío", msg, "OK");
                await DisplayAlert("Pendiente",
                    "Se guardó localmente, pero no se pudo enviar. Queda pendiente para sincronizar luego.",
                    "OK");
                await Shell.Current.GoToAsync("..", true, new Dictionary<string, object>
                {
                    ["Refresh"] = "1"
                });
            }

        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"No se pudo enviar: {ex.Message}", "OK");
        }
        finally
        {
            btnEnviar.IsEnabled = true;
            btnEnviar.Text = "✓ Enviar";
        }
    }


    private async void OnGuardarClicked(object sender, EventArgs e)
    {
        try
        {
            if (!ValidarFormulario()) return;

            btnGuardar.IsEnabled = false;
            btnGuardar.Text = "⏳ Guardando...";

            var id = await GuardarLocalAsync();

            if (id > 0)
            {
                await DisplayAlert("Guardado", $"Subsanación guardada offline (Id={id}).", "OK");

                // ← ESTE ES EL CAMBIO: navegar de vuelta y forzar refresh
                await Shell.Current.GoToAsync("..", true, new Dictionary<string, object>
                {
                    ["Refresh"] = "1"
                });
            }
            else
            {
                await DisplayAlert("Error", "No se pudo guardar localmente.", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"No se pudo guardar: {ex.Message}", "OK");
        }
        finally
        {
            btnGuardar.IsEnabled = true;
            btnGuardar.Text = "💾 Guardar Offline";
        }
    }



    private bool ValidarFormulario()
    {
        if (string.IsNullOrWhiteSpace(txtActividadesSubsanacion.Text))
        {
            DisplayAlert("Validación",
                "Debe describir las actividades de subsanación.", "OK");
            return false;
        }

        if (_fotoBytes == null)
        {
            DisplayAlert("Validación",
                "Debe tomar una foto de la subsanación.", "OK");
            return false;
        }

        if (!_latitudSubsanacion.HasValue || !_longitudSubsanacion.HasValue)
        {
            DisplayAlert("Validación",
                "Debe obtener la ubicación GPS.", "OK");
            return false;
        }

        return true;
    }

    private async void OnHistorialClicked(object sender, EventArgs e)
    {
        try
        {
            // Navegar usando la ruta registrada en AppShell
            await Shell.Current.GoToAsync(nameof(HistorialDeficienciasPage));
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al abrir historial: {ex.Message}", "OK");
        }
    }


    private async void OnBackClicked(object sender, EventArgs e)
    {
        try
        {
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error",
                $"No se pudo volver: {ex.Message}", "OK");
        }
    }
}


