using AplicacionMovil.Data;
using AplicacionMovil.Services;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices.Sensors;
using System.Collections.ObjectModel;
using System.Text.Json;
using AplicacionMovil.Models;

namespace AplicacionMovil.Pages;

public partial class MisDeficienciasPage : ContentPage
{
    public ObservableCollection<DeficienciaItemVm> Deficiencias { get; } = new();

    private bool _loaded;

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

        if (_loaded) return;
        _loaded = true;

        await CargarAsync();
    }

    private async Task CargarAsync()
    {
        try
        {
            lblEstado.Text = "Cargando...";
            await ApiClient.ApplyBearerAsync();

            var resp = await ApiClient.Http.GetAsync("api/movil/ot/deficiencias");
            var ct = resp.Content.Headers.ContentType?.ToString() ?? "";
            var body = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
            {
                lblEstado.Text = $"Error HTTP {(int)resp.StatusCode}";
                return;
            }

            if (!ct.Contains("application/json", StringComparison.OrdinalIgnoreCase))
            {
                lblEstado.Text = "Respuesta no JSON (posible login/HTML).";
                return;
            }

            var data = JsonSerializer.Deserialize<List<DeficienciaItemVm>>(
                body,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            ) ?? new();

            Deficiencias.Clear();
            foreach (var it in data) Deficiencias.Add(it);

            lblEstado.Text = Deficiencias.Count == 0
                ? "No tienes deficiencias asignadas."
                : $"Deficiencias asignadas: {Deficiencias.Count}";
        }
        catch (Exception ex)
        {
            lblEstado.Text = "Error cargando deficiencias.";
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
        // Si aún no tienes historial de deficiencias:
        await DisplayAlert("Historial", "Pendiente de implementar.", "OK");
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
}