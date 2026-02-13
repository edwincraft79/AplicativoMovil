using AplicacionMovil.Data;   // donde está InterrupcionRegistro
using Microsoft.Maui.ApplicationModel;   // arriba del archivo, si no está ya
using Microsoft.Maui.Devices;            // para DeviceInfo
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
using AplicacionMovil.Models;



namespace AplicacionMovil.Pages
{


    // Estos nombres deben coincidir con los parámetros que mandas en GoToAsync
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
        // Propiedades que vienen desde la OT
        public string CodigoReclamo { get; set; }
        public string Sucursal { get; set; }
        public string TipoReclamo { get; set; }
        public string NombreReclamante { get; set; }
        public string Telefono { get; set; }
        public string Descripcion { get; set; }

        // Las coordenadas vienen como texto en la query
        public string LatitudStr { get; set; }
        public string LongitudStr { get; set; }

        // Opcional: si quieres tenerlas parseadas
        private double? _latitud;
        private double? _longitud;

        private const string API_URL = "https://elpuregistro.com/api/InterrupcionesMovil/registrar";

        private const double PrecisionObjetivoMetros = 30.0;

        // Tamaño máximo permitido (4 MB)
        private const long MaxPhotoSizeBytes = 4 * 1024 * 1024;

        private readonly HttpClient _http = new();

        // Foto actual en memoria
        private byte[] _fotoBytes;
        private string? _fotoBase64; // ✅ nuevo
        private string _fotoNombre;

        // GPS actual (donde está el técnico, no el reclamo)
        private double? _latitudActual;
        private double? _longitudActual;
        private double? _precisionActual;

        // Archivo para guardar pendientes offline
        private readonly string _archivoPendientesPath;

        private string? _rutaFotoLocal;

        private const string API_BASE = "https://elpuregistro.com";

        public RegistroInterrupcionesPage()
        {
            InitializeComponent();
            _archivoPendientesPath = Path.Combine(
                FileSystem.AppDataDirectory,
                "interrupciones_pendientes.json");
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            try
            {
                // === Cabecera del reclamo (solo lectura) ===
                txtCodigoReclamo.Text = CodigoReclamo ?? string.Empty;
                txtSucursal.Text = Sucursal ?? string.Empty;
                txtDescripcion.Text = Descripcion ?? string.Empty;
                txtNombreReclamante.Text = NombreReclamante ?? string.Empty;
                txtTelefono.Text = Telefono ?? string.Empty;

                // GPS de referencia del reclamo
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

                // Fecha y hora de atención por defecto: ahora
                var ahora = DateTime.Now;
                dpFecha.Date = ahora.Date;
                tpHora.Time = ahora.TimeOfDay;

                // Preseleccionar clasificación según el TipoReclamo
                PreseleccionarClasificacionDesdeTipo();
            }
            catch
            {
                // Si algo falla, no queremos que reviente la página
            }
        }

        /// <summary>
        /// Calcula la clasificación (índice del picker) en base al texto de TipoReclamo.
        /// Ej: 
        ///  - "Interrupción suministro eléctrico" -> índice 0
        ///  - "Deficiencia en alumbrado público" -> índice 1
        ///  - "Riesgo eléctrico"                 -> índice 2
        /// </summary>
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

            // Mostrar/ocultar paneles acorde al valor seleccionado
            MostrarPanelesPorClasificacion();
        }

        // ========== EVENTO DEL PICKER (arregla el error del XAML) ==========
        private void OnClasificacionChanged(object sender, EventArgs e)
        {
            MostrarPanelesPorClasificacion();
        }

        /// <summary>
        /// Encender/apagar los paneles según el item seleccionado del picker.
        /// index 0 = Interrupción
        /// index 1 = Alumbrado
        /// index 2 = Riesgo
        /// </summary>
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

        // ========== RESTO DE EVENTOS (puedes pegar tu lógica anterior) ==========

        private async void OnTomarFotoClicked(object sender, EventArgs e)
        {
            if (!await AsegurarPermisoCamaraAsync())
                return;

            FileResult photo;
            try
            {
                photo = await MediaPicker.Default.CapturePhotoAsync();
            }
            catch
            {
                return; // usuario canceló
            }

            if (photo == null)
                return;

            using var stream = await photo.OpenReadAsync();

            // 1️⃣ Comprimir y redimensionar (como ya tienes)
            var bytesOptimizada = await RedimensionarYComprimirFotoAsync(stream);

            // 2️⃣ Añadir fecha/hora usando SkiaSharp
            var bytesConFecha = AgregarFechaHora(bytesOptimizada);

            // Guardar para enviar al servidor
            _fotoBytes = bytesConFecha;
            _fotoBase64 = Convert.ToBase64String(_fotoBytes);

            // Mostrar preview
            imgFoto.Source = ImageSource.FromStream(() => new MemoryStream(_fotoBytes));
            lblFotoSize.Text = $"Foto lista ({_fotoBytes.Length / 1024} KB)";
        }



        private async void OnObtenerGpsClicked(object sender, EventArgs e)
        {
            const double objetivoPrecision = 30.0;  // metros
            const int maxIntentos = 5;              // intentos máximos

            try
            {
                // 1) Verificar / solicitar permisos de ubicación
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

                // 2) Deshabilitar botón y mostrar mensaje de búsqueda
                btnGps.IsEnabled = false;
                lblPrecision.Text = "Buscando ubicación...";
                lblCoords.Text = "Obteniendo coordenadas, espere por favor...";

                Location mejorLocation = null;

                // 3) Intentar varias veces hasta lograr la precisión objetivo o agotar intentos
                for (int intento = 1; intento <= maxIntentos; intento++)
                {
                    var request = new GeolocationRequest(
                        GeolocationAccuracy.Best,
                        TimeSpan.FromSeconds(8));

                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

                    var location = await Geolocation.GetLocationAsync(request, cts.Token);
                    if (location == null)
                    {
                        // No obtuvo nada en este intento, probamos nuevamente
                        await Task.Delay(2000);
                        continue;
                    }

                    mejorLocation = location;

                    _latitudActual = location.Latitude;
                    _longitudActual = location.Longitude;
                    _precisionActual = location.Accuracy;

                    // Actualizar UI en cada intento
                    lblCoords.Text = $"Lat: {_latitudActual:F6}, Lon: {_longitudActual:F6}";
                    lblPrecision.Text =
                        $"Precisión: ±{_precisionActual:F1} m (intento {intento}/{maxIntentos})";

                    // Si ya estamos dentro del objetivo de 30 m, salimos del bucle
                    if (_precisionActual <= objetivoPrecision)
                        break;

                    // Si aún no llegamos, esperamos un poco antes del siguiente intento
                    await Task.Delay(2000);
                }

                // 4) Validar resultado final
                if (mejorLocation == null)
                {
                    await DisplayAlert("GPS", "No se pudo obtener la ubicación.", "OK");
                    lblPrecision.Text = "Precisión: --";
                    lblCoords.Text = "Lat/Lon aún no guardadas";
                    return;
                }

                // Mensaje final dependiendo de si se llegó o no al objetivo de 30 m
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
                // 5) Siempre reactivar el botón
                btnGps.IsEnabled = true;
            }
        }


        private async void OnEnviarClicked(object sender, EventArgs e)
        {
            try
            {
                btnEnviar.IsEnabled = false;

                // ===== VALIDACIONES BÁSICAS =====
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

                // 🔹 Fecha y hora: siempre se usan (ya vienen por defecto de los controles)
                var fechaSeleccionada = dpFecha.Date;
                var horaSeleccionada = tpHora.Time;
                var fechaEjecucion = fechaSeleccionada + horaSeleccionada;
                fechaEjecucion = DateTime.SpecifyKind(fechaEjecucion, DateTimeKind.Local);

                // 🔴 Descripción SIEMPRE es obligatoria
                if (string.IsNullOrWhiteSpace(txtDescripcionSol.Text))
                {
                    var msg = esDesestimado
                        ? "Indique la razón del desestimado en la descripción."
                        : "Ingrese la descripción de la solución.";
                    await DisplayAlert("Aviso", msg, "OK");
                    return;
                }

                // =========================
                // CASO 1: RECLAMO DESESTIMADO
                // =========================
                if (esDesestimado)
                {
                    // No exigimos GPS, foto ni detalles de clasificación.
                    // Solo usamos fecha/hora + descripción (ya validadas arriba).
                }
                // =========================
                // CASO 2: RECLAMO NORMAL (NO DESESTIMADO)
                // =========================
                else
                {
                    // Clasificación obligatoria
                    if (pkClasificacion.SelectedItem == null)
                    {
                        await DisplayAlert("Aviso", "Seleccione la clasificación del reclamo.", "OK");
                        return;
                    }

                    // Validaciones específicas según panel visible
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

                    // GPS obligatorio
                    if (_latitudActual == null || _longitudActual == null)
                    {
                        await DisplayAlert("Aviso", "Debe obtener primero el GPS.", "OK");
                        return;
                    }

                    // 📸 FOTO obligatoria
                    if (_fotoBytes == null || _fotoBytes.Length == 0)
                    {
                        await DisplayAlert("Aviso", "Debe tomar una foto de campo antes de enviar.", "OK");
                        return;
                    }
                }

                // ===== FOTO A BASE64 (si existe) =====
                string? fotoBase64 = _fotoBase64;

                // ===== CONSTRUIR MODELO EXACTO QUE ESPERA EL SERVIDOR =====
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

                // === ENVÍO AL SERVIDOR ===
                var url = API_URL;
                var json = JsonSerializer.Serialize(modelo);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                using var http = new HttpClient();
                var response = await http.PostAsync(url, content);
                var body = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    // ✅ 1) Guardar también en el historial local como ENVIADO
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
                        Enviado = true   // 🟢 ENVIADO AL SERVIDOR
                    };

                    await GuardarEnHistorialAsync(registroHistorial);

                    // ✅ 2) Mensaje igual que tenías
                    var mensaje = chkDesestimado.IsChecked
                        ? "La información fue enviada correctamente y la OT fue marcada como DESESTIMADA."
                        : "La información fue enviada correctamente y la OT fue marcada como ATENDIDA.";

                    await DisplayAlert("Atención Registrada", mensaje, "OK");
                    await Navigation.PopAsync();   // volver a Mis OT
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
            }
        }




        private async void OnGuardarClicked(object sender, EventArgs e)
        {
            // Evitar doble click
            btnGuardar.IsEnabled = false;

            try
            {
                // 1) Validar datos mínimos
                if (string.IsNullOrWhiteSpace(txtCodigoReclamo.Text))
                {
                    await DisplayAlert("Validación",
                        "No se encontró el código de reclamo de la orden de trabajo.",
                        "OK");
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtDescripcionSol.Text))
                {
                    await DisplayAlert("Validación",
                        "Debe ingresar la descripción de la solución.",
                        "OK");
                    return;
                }

                // Clasificación obligatoria
                if (pkClasificacion.SelectedItem == null)
                {
                    await DisplayAlert("Validación",
                        "Seleccione la clasificación del reclamo.",
                        "OK");
                    return;
                }

                // 2) Construir fecha/hora de atención
                var fechaSeleccionada = dpFecha.Date;
                var horaSeleccionada = tpHora.Time;
                var fechaAtencion = fechaSeleccionada + horaSeleccionada;
                fechaAtencion = DateTime.SpecifyKind(fechaAtencion, DateTimeKind.Local);

                // 3) Campos específicos según panel visible
                string? naturaleza = null;
                string? equipo = null;
                string? fases = null;
                string? detalleAlumbrado = null;
                string? detalleRiesgo = null;

                if (panelInterrupcion.IsVisible)
                {
                    naturaleza = pkNaturaleza.SelectedItem?.ToString();
                    equipo = pkEquipo.SelectedItem?.ToString();
                    fases = pkFases.SelectedItem?.ToString();
                }

                if (panelAlumbrado.IsVisible)
                {
                    detalleAlumbrado = pkAlumbrado.SelectedItem?.ToString();
                }

                if (panelRiesgo.IsVisible)
                {
                    detalleRiesgo = pkRiesgo.SelectedItem?.ToString();
                }

                // 4) Armar el modelo (sin null en strings)
                var modelo = new InterrupcionRegistro
                {
                    CodigoReclamo = txtCodigoReclamo.Text?.Trim() ?? string.Empty,
                    Usuario = SesionMovil.Usuario ?? string.Empty,
                    Fecha = fechaAtencion,
                    ClasificacionReclamo = pkClasificacion.SelectedItem?.ToString() ?? string.Empty,

                    NaturalezaInterrupcion = naturaleza ?? string.Empty,
                    EquipoReposicion = equipo ?? string.Empty,
                    FasesReposicion = fases ?? string.Empty,
                    DetalleAlumbrado = detalleAlumbrado ?? string.Empty,
                    DetalleRiesgo = detalleRiesgo ?? string.Empty,

                    DescripcionSolucion = txtDescripcionSol.Text?.Trim() ?? string.Empty,

                    FotoBytes = _fotoBytes,
                    FotoBase64 = _fotoBase64,
                    Latitud = _latitudActual,
                    Longitud = _longitudActual,
                    PrecisionGps = _precisionActual,

                    Enviado = false   // 🔴 OFFLINE / PENDIENTE
                };

                // ✅ Guardar usando el nuevo método
                await GuardarEnHistorialAsync(modelo);

                await DisplayAlert("Guardado",
                    "Registro guardado en el historial offline.",
                    "OK");

                // Opcional: limpiar campos de trabajo de campo (no los de la OT)
                // LimpiarFormularioDeCampo();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error",
                    $"No se pudo guardar el registro offline.\nDetalle: {ex.Message}",
                    "OK");
            }
            finally
            {
                btnGuardar.IsEnabled = true;
            }
        }


        // ✅ MÉTODO GENERAL PARA GUARDAR EN EL HISTORIAL LOCAL
        private async Task GuardarEnHistorialAsync(InterrupcionRegistro nuevo)
        {
            List<InterrupcionRegistro> lista;

            if (File.Exists(_archivoPendientesPath))
            {
                var json = await File.ReadAllTextAsync(_archivoPendientesPath);
                lista = JsonSerializer.Deserialize<List<InterrupcionRegistro>>(json)
                        ?? new List<InterrupcionRegistro>();
            }
            else
            {
                lista = new List<InterrupcionRegistro>();
            }

            lista.Add(nuevo);

            var jsonNuevo = JsonSerializer.Serialize(
                lista,
                new JsonSerializerOptions { WriteIndented = true });

            await File.WriteAllTextAsync(_archivoPendientesPath, jsonNuevo);
        }



        private async void OnHistorialClicked(object sender, EventArgs e)
        {
            // Navegación ABSOLUTA al elemento de Shell
            await Shell.Current.GoToAsync("///HistorialPage");
            // (también puedes usar "//HistorialPage"; con "///" limpias toda la pila)
        }

        private async Task<bool> AsegurarPermisoCamaraAsync()
        {
            var status = await Permissions.CheckStatusAsync<Permissions.Camera>();

            if (status != PermissionStatus.Granted)
            {
                // Explicación opcional antes de pedir permiso
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

        // -----------------------------------------------------------------------------
        // MÉTODO: Redimensionar y Comprimir Foto Async (Siempre < 4 MB)
        // -----------------------------------------------------------------------------
        private async Task<byte[]> RedimensionarYComprimirFotoAsync(Stream originalStream)
        {
            try
            {
                using var inputStream = new MemoryStream();
                await originalStream.CopyToAsync(inputStream);
                inputStream.Position = 0;

                // Cargar imagen con SkiaSharp
                using var skBitmap = SKBitmap.Decode(inputStream);

                if (skBitmap == null)
                    throw new Exception("Error al decodificar la imagen");

                int maxWidth = 1280;
                int maxHeight = 1280;

                // Calcular nuevo tamaño manteniendo proporciones
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

                // Compresión JPG al 70%
                var data = image.Encode(SKEncodedImageFormat.Jpeg, 70);

                return data.ToArray();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"No se pudo comprimir la foto: {ex.Message}", "OK");
                return null;
            }
        }

        private async Task ObtenerUbicacionConObjetivoAsync()
        {
            lblPrecision.Text = "Buscando ubicación...";
            lblCoords.Text = "Obteniendo coordenadas...";

            var request = new GeolocationRequest(
                GeolocationAccuracy.Best,
                TimeSpan.FromSeconds(10));

            Location? mejor = null;
            double mejorPrecision = double.MaxValue;

            int maxIntentos = 5;

            for (int intento = 1; intento <= maxIntentos; intento++)
            {
                Location? location = null;

                try
                {
                    location = await Geolocation.Default.GetLocationAsync(request);
                }
                catch
                {
                    // errores silenciosos por ahora (sin internet, etc.)
                }

                if (location == null)
                    continue;

                var precision = location.Accuracy ?? double.MaxValue;

                // Guardamos la mejor lectura
                if (precision < mejorPrecision)
                {
                    mejor = location;
                    mejorPrecision = precision;
                }

                lblPrecision.Text = $"Precisión: ±{precision:0.0} m  (intento {intento}/{maxIntentos})";
                lblCoords.Text = $"Lat: {location.Latitude:F6}, Lon: {location.Longitude:F6}";

                _latitudActual = location.Latitude;
                _longitudActual = location.Longitude;
                _precisionActual = precision;

                // ✅ Ya cumplimos el objetivo
                if (precision <= PrecisionObjetivoMetros)
                {
                    lblPrecision.Text = $"Precisión: ±{precision:0.0} m (objetivo cumplido)";
                    return;
                }

                // Esperamos un poco antes del siguiente intento
                await Task.Delay(1500);
            }

            if (mejor != null)
            {
                // No se llegó a < 30 m pero usamos la mejor lectura
                lblPrecision.Text = $"Precisión: ±{mejorPrecision:0.0} m (mejor lectura)";
                lblCoords.Text = $"Lat: {mejor.Latitude:F6}, Lon: {mejor.Longitude:F6}";
                _latitudActual = mejor.Latitude;
                _longitudActual = mejor.Longitude;
                _precisionActual = mejorPrecision;

                await DisplayAlert(
                    "Ubicación",
                    "No se logró llegar a una precisión menor a 30 m,\n" +
                    "pero se usará la mejor lectura disponible.",
                    "OK");
            }
            else
            {
                await DisplayAlert(
                    "Ubicación",
                    "No se pudo obtener la ubicación. Intente nuevamente.",
                    "OK");
            }
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            // Un paso atrás en la pila de navegación
            await Shell.Current.GoToAsync("..");

            // Si en algún momento ves pantalla en blanco,
            // puedes usar "../.." para subir dos niveles:
            // await Shell.Current.GoToAsync("../..");
        }


        private byte[] AgregarFechaHora(byte[] inputBytes)
            {
                using var ms = new MemoryStream(inputBytes);
                using var bitmap = SKBitmap.Decode(ms);

                var info = new SKImageInfo(bitmap.Width, bitmap.Height);
                using var surface = SKSurface.Create(info);
                var canvas = surface.Canvas;

                // Dibujar foto original
                using (var img = SKImage.FromBitmap(bitmap))
                    canvas.DrawImage(img, 0, 0);

                // Texto fecha/hora
                string texto = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
                float textSize = bitmap.Width / 25f;

                var paint = new SKPaint
            {
                Color = SKColors.White,
                TextSize = textSize,
                IsAntialias = true,
                Typeface = SKTypeface.FromFamilyName("Arial"),
            };

            // Fondo oscuro semitransparente
            var paintFondo = new SKPaint
            {
                Color = new SKColor(0, 0, 0, 160),
                Style = SKPaintStyle.Fill
            };

            // Medir texto
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

                // Convertir de vuelta a JPG
                using var output = new MemoryStream();
                using var finalImage = surface.Snapshot();
                using var data = finalImage.Encode(SKEncodedImageFormat.Jpeg, 80);

                data.SaveTo(output);
                return output.ToArray();
        }

        private void OnDesestimadoChanged(object sender, CheckedChangedEventArgs e)
        {
            bool desestimado = e.Value;

            // Solo deshabilitamos los paneles de la clasificación
            panelInterrupcion.IsEnabled = !desestimado;
            panelAlumbrado.IsEnabled = !desestimado;
            panelRiesgo.IsEnabled = !desestimado;

            // Opcional: limpiar selecciones cuando se marca como desestimado
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
