using AplicacionMovil.Services;
using Microsoft.Maui.Devices.Sensors;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace AplicacionMovil.Modules.Mantenimiento.Pages;

[QueryProperty(nameof(OtId), "OtId")]
[QueryProperty(nameof(CodigoOT), "CodigoOT")]
[QueryProperty(nameof(FeatureId), "FeatureId")]
[QueryProperty(nameof(FeatureNombre), "FeatureNombre")]
[QueryProperty(nameof(FeatureTipo), "FeatureTipo")]
[QueryProperty(nameof(EstadoActual), "EstadoActual")]
[QueryProperty(nameof(Lat), "Lat")]
[QueryProperty(nameof(Lon), "Lon")]
public partial class RegistroItemCampoPage : ContentPage
{
    public int OtId { get; set; }
    public string CodigoOT { get; set; } = "";
    public int FeatureId { get; set; }
    public string FeatureNombre { get; set; } = "";
    public string FeatureTipo { get; set; } = "Punto";
    public string EstadoActual { get; set; } = "Pendiente";

    // Vienen del mapa de confirmación (ConfirmarPuntoCampoPage), como string por QueryProperty.
    // Si no llegan (se entra directo a esta página), queda disponible el botón "Capturar GPS".
    public string? Lat { get; set; }
    public string? Lon { get; set; }

    private double? _gpsLat;
    private double? _gpsLon;
    private string? _fotoAntesB64;
    private string? _fotoDespuesB64;

    private readonly ObservableCollection<ItemPlanVm> _itemsPlan = new();
    private readonly List<ItemExtraVm> _itemsExtra = new();

    public RegistroItemCampoPage()
    {
        InitializeComponent();
        colItemsPlan.ItemsSource = _itemsPlan;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        lblTitulo.Text    = FeatureNombre;
        lblSubtitulo.Text = $"{FeatureTipo} · OT {CodigoOT} · Estado: {EstadoActual}";

        if (double.TryParse(Lat, NumberStyles.Float, CultureInfo.InvariantCulture, out var lat) &&
            double.TryParse(Lon, NumberStyles.Float, CultureInfo.InvariantCulture, out var lon))
        {
            _gpsLat = lat;
            _gpsLon = lon;
            lblGps.Text = $"📍 {lat:F6}, {lon:F6}  (confirmado en el mapa)";
            lblGps.TextColor = Color.FromArgb("#22C55E");
        }

        await CargarItemsPlanAsync();
    }

    private async Task CargarItemsPlanAsync()
    {
        try
        {
            await ApiClient.ApplyBearerAsync();
            var resp = await ApiClient.Http.GetAsync($"api/mantenimiento-movil/ot/{OtId}/items-precargados");
            if (!resp.IsSuccessStatusCode) return;

            var json = await resp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            _itemsPlan.Clear();
            if (doc.RootElement.TryGetProperty("materiales", out var mats))
            {
                foreach (var m in mats.EnumerateArray())
                {
                    _itemsPlan.Add(new ItemPlanVm
                    {
                        Id             = m.GetProperty("id").GetInt32(),
                        Tipo           = "material",
                        Descripcion    = m.GetProperty("descripcion").GetString() ?? "",
                        Unidad         = m.GetProperty("unidad").GetString() ?? "",
                        CantidadPlan   = m.GetProperty("cantidadPlan").GetDecimal(),
                        PrecioUnitario = m.GetProperty("precioUnitario").GetDecimal(),
                        CantidadReal   = ""
                    });
                }
            }
            if (doc.RootElement.TryGetProperty("manoObra", out var mo))
            {
                foreach (var m in mo.EnumerateArray())
                {
                    _itemsPlan.Add(new ItemPlanVm
                    {
                        Id             = m.GetProperty("id").GetInt32(),
                        Tipo           = "mano_obra",
                        Descripcion    = m.GetProperty("descripcion").GetString() ?? "",
                        Unidad         = m.GetProperty("unidad").GetString() ?? "",
                        CantidadPlan   = m.GetProperty("cantidadPlan").GetDecimal(),
                        PrecioUnitario = m.GetProperty("precioUnitario").GetDecimal(),
                        CantidadReal   = ""
                    });
                }
            }
        }
        catch { /* silencioso: los ítems del plan son opcionales */ }
    }

    // ── Ítems extra ───────────────────────────────────────────────────────

    private void OnAgregarInstalacionClicked(object s, EventArgs e) => AgregarItemExtra("instalacion");
    private void OnAgregarRetiroClicked(object s, EventArgs e)      => AgregarItemExtra("retiro");
    private void OnAgregarManoObraClicked(object s, EventArgs e)    => AgregarItemExtra("mano_obra");

    private void AgregarItemExtra(string categoria)
    {
        var vm = new ItemExtraVm { Categoria = categoria };
        _itemsExtra.Add(vm);

        var colorFondo = categoria switch
        {
            "instalacion" => Color.FromArgb("#EFF6FF"),
            "retiro"      => Color.FromArgb("#FFF1F2"),
            _             => Color.FromArgb("#FFFBEB")
        };

        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = new GridLength(70) },
                new ColumnDefinition { Width = new GridLength(36) }
            },
            ColumnSpacing = 6,
            Padding = new Thickness(0, 2),
            BackgroundColor = colorFondo
        };

        var entDescripcion = new Entry
        {
            Placeholder = $"Descripción ({categoria})",
            FontSize = 12,
            BindingContext = vm
        };
        entDescripcion.TextChanged += (_, a) => vm.Descripcion = a.NewTextValue;

        var entCantidad = new Entry
        {
            Placeholder = "Cant.",
            Keyboard = Keyboard.Numeric,
            FontSize = 12,
            HorizontalTextAlignment = TextAlignment.Center,
            BindingContext = vm
        };
        entCantidad.TextChanged += (_, a) =>
        {
            if (decimal.TryParse(a.NewTextValue, out var v)) vm.Cantidad = v;
        };

        var btnQuitar = new Button
        {
            Text = "✕", FontSize = 12,
            TextColor = Colors.White,
            BackgroundColor = Color.FromArgb("#94A3B8"),
            CornerRadius = 6, HeightRequest = 32, WidthRequest = 32
        };
        btnQuitar.Clicked += (_, _) =>
        {
            _itemsExtra.Remove(vm);
            stackItemsExtra.Remove(grid);
        };

        grid.Add(entDescripcion, 0);
        grid.Add(entCantidad,    1);
        grid.Add(btnQuitar,      2);

        stackItemsExtra.Add(grid);
    }

    // ── GPS ───────────────────────────────────────────────────────────────

    private async void OnCapturarGpsClicked(object s, EventArgs e)
    {
        try
        {
            lblGps.Text      = "Obteniendo GPS...";
            lblGps.TextColor = Color.FromArgb("#0EA5E9");

            var req = new GeolocationRequest(GeolocationAccuracy.High, TimeSpan.FromSeconds(10));
            var loc = await Geolocation.GetLocationAsync(req);
            if (loc != null)
            {
                _gpsLat = loc.Latitude;
                _gpsLon = loc.Longitude;
                lblGps.Text = $"📍 {_gpsLat:F6}, {_gpsLon:F6}  ±{loc.Accuracy?.ToString("F0")} m";
                lblGps.TextColor = Color.FromArgb("#22C55E");
            }
            else
            {
                lblGps.Text = "No se pudo obtener GPS";
                lblGps.TextColor = Color.FromArgb("#EF4444");
            }
        }
        catch (Exception ex)
        {
            lblGps.Text = $"Error: {ex.Message}";
            lblGps.TextColor = Color.FromArgb("#EF4444");
        }
    }

    // ── Fotos ─────────────────────────────────────────────────────────────

    private async void OnFotoAntesClicked(object s, EventArgs e)
        => _fotoAntesB64 = await CapturarFotoAsync(imgAntes);

    private async void OnFotoDespuesClicked(object s, EventArgs e)
        => _fotoDespuesB64 = await CapturarFotoAsync(imgDespues);

    private async Task<string?> CapturarFotoAsync(Image imgPreview)
    {
        try
        {
            var accion = await DisplayActionSheet("Foto", "Cancelar", null, "📷 Cámara", "🖼️ Galería");
            FileResult? foto = null;
            if (accion == "📷 Cámara") foto = await MediaPicker.Default.CapturePhotoAsync();
            else if (accion == "🖼️ Galería") foto = await MediaPicker.Default.PickPhotoAsync();
            if (foto == null) return null;

            using var stream = await foto.OpenReadAsync();
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            var bytes = ms.ToArray();

            imgPreview.Source = ImageSource.FromStream(() => new MemoryStream(bytes));
            imgPreview.IsVisible = true;

            return Convert.ToBase64String(bytes);
        }
        catch { return null; }
    }

    // ── Guardar ───────────────────────────────────────────────────────────

    private async void OnGuardarClicked(object s, EventArgs e)
    {
        var itemsFinales = new List<object>();

        // Ítems del plan con cantidad real ingresada
        foreach (var item in _itemsPlan)
        {
            if (!decimal.TryParse(item.CantidadReal, out var cant) || cant <= 0) continue;
            itemsFinales.Add(new
            {
                categoria      = item.Tipo == "mano_obra" ? "mano_obra" : "instalacion",
                descripcion    = item.Descripcion,
                cantidad       = cant,
                precioUnitario = item.PrecioUnitario,
                idReservaMaterial = item.Tipo == "material" ? (int?)item.Id : null,
                idManoObra     = item.Tipo == "mano_obra"  ? (int?)item.Id : null
            });
        }

        // Ítems extra libres
        foreach (var item in _itemsExtra.Where(i => !string.IsNullOrWhiteSpace(i.Descripcion) && i.Cantidad > 0))
        {
            itemsFinales.Add(new
            {
                categoria      = item.Categoria,
                descripcion    = item.Descripcion,
                cantidad       = item.Cantidad,
                precioUnitario = 0m,
                idReservaMaterial = (int?)null,
                idManoObra     = (int?)null
            });
        }

        if (!itemsFinales.Any() && string.IsNullOrWhiteSpace(txtObservacion.Text))
        {
            await DisplayAlert("Validación", "Ingresa al menos un ítem o una observación.", "OK");
            return;
        }

        btnGuardar.IsEnabled   = false;
        actIndicator.IsVisible = true;
        actIndicator.IsRunning = true;

        try
        {
            await ApiClient.ApplyBearerAsync();

            var payload = new
            {
                idOT          = OtId,
                idGeoFeature  = FeatureId,
                estado        = "Ejecutado",
                observacion   = txtObservacion.Text?.Trim(),
                latitud       = _gpsLat,
                longitud      = _gpsLon,
                fotoAntesBase64   = _fotoAntesB64,
                fotoDespuesBase64 = _fotoDespuesB64,
                items = itemsFinales
            };

            var json    = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var resp    = await ApiClient.Http.PostAsync("api/mantenimiento-movil/campo/registro", content);
            var body    = await resp.Content.ReadAsStringAsync();

            if (resp.IsSuccessStatusCode)
            {
                await DisplayAlert("✅ Guardado", "Registro enviado correctamente.", "OK");
                await Shell.Current.GoToAsync("..");
            }
            else
            {
                await DisplayAlert("Error", $"No se pudo guardar: {body}", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error de red", ex.Message, "OK");
        }
        finally
        {
            btnGuardar.IsEnabled   = true;
            actIndicator.IsVisible = false;
            actIndicator.IsRunning = false;
        }
    }

    private async void OnVolverClicked(object s, EventArgs e)
        => await Shell.Current.GoToAsync("..");

    // ── ViewModels locales ────────────────────────────────────────────────

    private class ItemPlanVm
    {
        public int Id { get; set; }
        public string Tipo { get; set; } = "";
        public string Descripcion { get; set; } = "";
        public string Unidad { get; set; } = "";
        public decimal CantidadPlan { get; set; }
        public decimal PrecioUnitario { get; set; }
        public string CantidadReal { get; set; } = "";
    }

    private class ItemExtraVm
    {
        public string Categoria { get; set; } = "";
        public string Descripcion { get; set; } = "";
        public decimal Cantidad { get; set; }
    }
}
