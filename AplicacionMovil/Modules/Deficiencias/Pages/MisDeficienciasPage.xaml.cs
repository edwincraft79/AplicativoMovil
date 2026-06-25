using AplicacionMovil.Services;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices.Sensors;
using System.Collections.ObjectModel;
using System.Text.Json;
using AplicacionMovil.Modules.Deficiencias.Models;
using AplicacionMovil.Core.Models;

namespace AplicacionMovil.Modules.Deficiencias.Pages;
[QueryProperty(nameof(Refresh), "refresh")]
public partial class MisDeficienciasPage : ContentPage
{
    public ObservableCollection<DeficienciaItemVm> Deficiencias { get; } = new();

    private bool _loaded;
    public string? Refresh { get; set; }

    public MisDeficienciasPage()
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
            await CargarAsync();
            return;
        }

        await CargarAsync();
        _loaded = true;
    }


    private async Task CargarAsync()
    {
        try
        {
            lblContadorDeficiencias.Text = "Cargando...";
            await ApiClient.ApplyBearerAsync();

            var resp = await ApiClient.Http.GetAsync("api/movil/ot/deficiencias");
            var ct = resp.Content.Headers.ContentType?.ToString() ?? "";
            var body = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
            {
                await DisplayAlert("Error", $"Error HTTP {(int)resp.StatusCode}", "OK");
                lblContadorDeficiencias.Text = "0 Deficiencias";
                return;
            }

            if (!ct.Contains("application/json", StringComparison.OrdinalIgnoreCase))
            {
                await DisplayAlert("Error", "Respuesta no JSON (posible login/HTML).", "OK");
                lblContadorDeficiencias.Text = "0 Deficiencias";
                return;
            }

            var data = JsonSerializer.Deserialize<List<DeficienciaItemVm>>(
                body,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            ) ?? new();

            Deficiencias.Clear();
            foreach (var it in data) Deficiencias.Add(it);

            // Actualizar contador
            lblContadorDeficiencias.Text = Deficiencias.Count == 0
                ? "0 Deficiencias"
                : $"{Deficiencias.Count} Deficiencia{(Deficiencias.Count != 1 ? "s" : "")}";
        }
        catch (Exception ex)
        {
            lblContadorDeficiencias.Text = "0 Deficiencias";
            await DisplayAlert("Deficiencias", ex.Message, "OK");
        }
        finally
        {
            refreshView.IsRefreshing = false;
        }
    }

    private async void OnVolverInicioClicked(object sender, EventArgs e)
    => await Shell.Current.GoToAsync("//HomePage");

    private async void OnHistorialClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(HistorialDeficienciasPage));
    }

    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        try { await SesionMovil.CerrarSesionAsync(); } catch { }
        await Shell.Current.GoToAsync("//LoginPage");
    }

    private async void OnVerMapa(object sender, EventArgs e)
        => await Shell.Current.GoToAsync(nameof(DeficienciasMapPage));

    private async void OnRutaClicked(object sender, EventArgs e)
    => OnRuta(sender, e);

    private async void OnMapaItemClicked(object sender, EventArgs e)
        => OnMapa(sender, e);

    private async void OnRefresh(object sender, EventArgs e)
    {
        await CargarAsync();
        refreshView.IsRefreshing = false;
    }

    private async void OnRuta(object sender, EventArgs e)
    {
        if (sender is not Button btn || btn.BindingContext is not DeficienciaItemVm d)
            return;

        if (!d.Latitud.HasValue || !d.Longitud.HasValue)
        {
            await DisplayAlert("Ruta", "Esta deficiencia no tiene coordenadas convertidas.", "OK");
            return;
        }

        var url = $"https://www.google.com/maps?q={d.Latitud.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)},{d.Longitud.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
        await Launcher.OpenAsync(url);
    }

    private async void OnMapa(object sender, EventArgs e)
    {
        // si quieres abrir un mapa tipo Leaflet con pins:
        await Shell.Current.GoToAsync(nameof(DeficienciasMapPage));
    }

    private async void OnEjecutarClicked(object sender, EventArgs e)
    {
        if (sender is not Button btn || btn.BindingContext is not DeficienciaItemVm def)
            return;

        var ruta = $"{nameof(RegistroDeficienciasPage)}" +
                   $"?DeficienciaId={def.Id}" +
                   $"&CodigoDeficiencia={Uri.EscapeDataString(def.CodigoDeficiencia ?? "")}" +
                   $"&UnidadZonal={Uri.EscapeDataString(def.UnidadZonal ?? "")}" +
                   $"&Alimentador={Uri.EscapeDataString(def.Alimentador ?? "")}" +
                   $"&CodigoTipificacion={Uri.EscapeDataString(def.CodigoTipificacion ?? "")}" +
                   $"&TipificacionTexto={Uri.EscapeDataString(def.TipificacionTexto ?? "")}" +
                   $"&Prioridad={Uri.EscapeDataString(def.Prioridad ?? "")}" +
                   $"&EstadoSubsanacion={Uri.EscapeDataString(def.EstadoSubsanacion ?? "")}" +
                   $"&FechaDenuncia={Uri.EscapeDataString(def.FechaDenuncia?.ToString("O") ?? "")}" +
                   $"&Latitud={def.Latitud?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? ""}" +
                   $"&Longitud={def.Longitud?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? ""}";

        await Shell.Current.GoToAsync(ruta);
    }

}
