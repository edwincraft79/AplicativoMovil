using AplicacionMovil.Core.Models;
using AplicacionMovil.Services;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices.Sensors;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AplicacionMovil.Modules.Reclamos.Pages;

public partial class MisOtPage : ContentPage
{
    public ObservableCollection<OtItemVm> Ots { get; } = new();

    private bool _loaded;
    public string? Refresh { get; set; }

    public MisOtPage()
    {
        InitializeComponent();
        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (!SesionMovil.EstaLogueado)
            await SesionMovil.RestaurarAsync();

        if (!SesionMovil.EstaLogueado)
        {
            await Shell.Current.GoToAsync("//LoginPage");
            return;
        }

        // 🔄 Forzar refresh cuando vuelves desde Registro
        if (Refresh == "1")
        {
            Refresh = null;
            _loaded = true;
            await CargarOtsAsync();
            return;
        }

        if (_loaded) return;
        _loaded = true;

        await CargarOtsAsync();
    }

    private async Task CargarOtsAsync()
    {
        try
        {
            lblContadorOts.Text = "Cargando...";
            await ApiClient.ApplyBearerAsync();

            var usuario = Uri.EscapeDataString(SesionMovil.Usuario ?? "");
            var resp = await ApiClient.Http.GetAsync($"api/ordenes/mis-ot?usuario={usuario}");

            var ct = resp.Content.Headers.ContentType?.ToString() ?? "";
            var body = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
            {
                lblContadorOts.Text = "0 OTs";
                await DisplayAlert("Error", $"Error HTTP {(int)resp.StatusCode}", "OK");
                return;
            }

            if (!ct.Contains("application/json", StringComparison.OrdinalIgnoreCase))
            {
                lblContadorOts.Text = "0 OTs";
                await DisplayAlert("Error", "Respuesta no JSON.", "OK");
                return;
            }

            var data = JsonSerializer.Deserialize<List<OtItemVm>>(
                body,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            ) ?? new();

            Ots.Clear();
            foreach (var it in data)
                Ots.Add(it);

            lblContadorOts.Text = Ots.Count == 0
                ? "0 OTs"
                : $"{Ots.Count} OT{(Ots.Count != 1 ? "s" : "")}";
        }
        catch (Exception ex)
        {
            lblContadorOts.Text = "0 OTs";
            await DisplayAlert("Error", $"No se pudieron cargar las OTs: {ex.Message}", "OK");
        }
        finally
        {
            refreshView.IsRefreshing = false;
        }
    }

    private async void OnRefreshOts(object sender, EventArgs e)
        => await CargarOtsAsync();

    private async void OnVolverInicioClicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("//HomePage");

    private async void OnHistorialClicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("HistorialPage");

    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        try { await SesionMovil.CerrarSesionAsync(); } catch { }
        await Shell.Current.GoToAsync("//LoginPage");
    }
    private async void OnVerMapa(object sender, EventArgs e)
    {
        // Serializar las OTs actuales y pasarlas como parámetro
        var json = System.Text.Json.JsonSerializer.Serialize(Ots.ToList());
        var encoded = Uri.EscapeDataString(json);
        await Shell.Current.GoToAsync($"{nameof(OtMapPage)}?otsJson={encoded}");
    }

    private async void OnVerMapaReclamos(object sender, EventArgs e)
        => await Shell.Current.GoToAsync($"{nameof(OtMapPage)}?tipo=reclamos");

    private async void OnVerMapaDeficiencias(object sender, EventArgs e)
        => await Shell.Current.GoToAsync($"{nameof(OtMapPage)}?tipo=deficiencias");

    private async void OnRutaClicked(object sender, EventArgs e)
    {
        if (sender is not Button btn || btn.BindingContext is not OtItemVm ot)
            return;

        if (!ot.Latitud.HasValue || !ot.Longitud.HasValue)
        {
            await DisplayAlert("Ruta", "Esta OT no tiene coordenadas.", "OK");
            return;
        }

        var lat = ot.Latitud.Value;
        var lon = ot.Longitud.Value;

        if (lat < -90 || lat > 90 || lon < -180 || lon > 180)
        {
            await DisplayAlert("Ruta", "Coordenadas fuera de rango.", "OK");
            return;
        }

        try
        {
            var location = new Location(lat, lon);

            var options = new MapLaunchOptions
            {
                Name = $"{ot.Sucursal} - {ot.CodigoOt}",
                NavigationMode = NavigationMode.Driving
            };

            await Map.OpenAsync(location, options);
        }
        catch (Exception)
        {
            // Fallback: abrir por URL
            var url = $"https://www.google.com/maps?q={lat.ToString(System.Globalization.CultureInfo.InvariantCulture)},{lon.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
            await Launcher.OpenAsync(url);
        }
    }

    private async void OnEjecutarClicked(object sender, EventArgs e)
    {
        if (sender is not Button btn || btn.BindingContext is not OtItemVm ot)
            return;

        var ruta = $"{nameof(RegistroInterrupcionesPage)}" +
                   $"?CodigoReclamo={Uri.EscapeDataString(ot.CodigoReclamo ?? "")}" +
                   $"&Sucursal={Uri.EscapeDataString(ot.Sucursal ?? "")}" +
                   $"&TipoReclamo={Uri.EscapeDataString(ot.TipoReclamo ?? "")}" +
                   $"&NombreReclamante={Uri.EscapeDataString(ot.NombreReclamante ?? "")}" +
                   $"&Telefono={Uri.EscapeDataString(ot.Telefono ?? "")}" +
                   $"&Descripcion={Uri.EscapeDataString(ot.Descripcion ?? "")}" +
                   $"&Latitud={ot.Latitud?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? ""}" +
                   $"&Longitud={ot.Longitud?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? ""}" +
                   $"&CodigoOt={Uri.EscapeDataString(ot.CodigoOt ?? "")}";

        await Shell.Current.GoToAsync(ruta);
    }
}

public class OtItemVm
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("codigoOt")]
    public string CodigoOt { get; set; } = "";

    [JsonPropertyName("codigoReclamo")]
    public string? CodigoReclamo { get; set; }

    [JsonPropertyName("sucursal")]
    public string? Sucursal { get; set; }

    [JsonPropertyName("fechaHoraReclamo")]
    public string? FechaHoraReclamo { get; set; }

    [JsonPropertyName("tipoReclamo")]
    public string? TipoReclamo { get; set; }

    [JsonPropertyName("estado")]
    public string Estado { get; set; } = "";

    [JsonPropertyName("nombreReclamante")]
    public string? NombreReclamante { get; set; }

    [JsonPropertyName("telefono")]
    public string? Telefono { get; set; }

    [JsonPropertyName("descripcion")]
    public string? Descripcion { get; set; }

    [JsonPropertyName("latitud")]
    public double? Latitud { get; set; }

    [JsonPropertyName("longitud")]
    public double? Longitud { get; set; }
}
