using AplicacionMovil.Modules.Calidad.Models;
using AplicacionMovil.Core.Models;       // SesionMovil
using AplicacionMovil.Services;          // ApiClient
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices.Sensors;
using System.Collections.ObjectModel;
using System.Text.Json;

namespace AplicacionMovil.Modules.Calidad.Pages;

public partial class MisOtCalidadPage : ContentPage
{
    // ── Colecciones ───────────────────────────────────────────────────
    private readonly List<CalidadOtModel> _todasLasOts = new();
    public ObservableCollection<CalidadOtModel> OtsFiltradas { get; } = new();

    private bool _loaded;
    private string _filtroActual = "TODOS";   // "TODOS" | "RURAL" | "URBANO"

    public MisOtCalidadPage()
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

        await CargarOtsAsync();
    }

    // ── CARGA DE OTs DESDE /api/calidad-movil/asignadas ───────────────
    private async Task CargarOtsAsync()
    {
        try
        {
            lblContadorOts.Text = "Cargando...";
            frameVacio.IsVisible = false;

            await ApiClient.ApplyBearerAsync();

            var resp = await ApiClient.Http.GetAsync("api/calidad-movil/asignadas");
            var ct = resp.Content.Headers.ContentType?.ToString() ?? "";
            var body = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
            {
                lblContadorOts.Text = "0 OTs";
                await DisplayAlert("Error", $"HTTP {(int)resp.StatusCode}: {body}", "OK");
                return;
            }

            if (!ct.Contains("application/json", StringComparison.OrdinalIgnoreCase))
            {
                lblContadorOts.Text = "0 OTs";
                await DisplayAlert("Error", "Respuesta inesperada del servidor.", "OK");
                return;
            }

            var data = JsonSerializer.Deserialize<List<CalidadOtModel>>(
                body,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            ) ?? new();

            _todasLasOts.Clear();
            _todasLasOts.AddRange(data);

            AplicarFiltro(_filtroActual);
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

    // ── FILTROS ───────────────────────────────────────────────────────
    private void AplicarFiltro(string filtro)
    {
        _filtroActual = filtro;

        var filtradas = filtro == "TODOS"
            ? _todasLasOts
            : _todasLasOts.Where(o =>
                o.Modalidad.Equals(filtro, StringComparison.OrdinalIgnoreCase)).ToList();

        OtsFiltradas.Clear();
        foreach (var ot in filtradas)
            OtsFiltradas.Add(ot);

        // Contador
        var total = OtsFiltradas.Count;
        lblContadorOts.Text = total == 0 ? "0 OTs" : $"{total} OT{(total != 1 ? "s" : "")}";

        // Vacío
        frameVacio.IsVisible = total == 0;

        // Estilos de botones de filtro
        ActualizarBotonesFiltro(filtro);
    }

    private void ActualizarBotonesFiltro(string filtro)
    {
        // Reset todos
        btnFiltroTodos.BackgroundColor = Color.FromArgb("#D1FAE5");
        btnFiltroTodos.TextColor = Color.FromArgb("#065F46");
        btnFiltroRural.BackgroundColor = Color.FromArgb("#D1FAE5");
        btnFiltroRural.TextColor = Color.FromArgb("#065F46");
        btnFiltroUrbano.BackgroundColor = Color.FromArgb("#DBEAFE");
        btnFiltroUrbano.TextColor = Color.FromArgb("#1E40AF");

        // Activo
        switch (filtro)
        {
            case "TODOS":
                btnFiltroTodos.BackgroundColor = Color.FromArgb("#10B981");
                btnFiltroTodos.TextColor = Colors.White;
                break;
            case "RURAL":
                btnFiltroRural.BackgroundColor = Color.FromArgb("#10B981");
                btnFiltroRural.TextColor = Colors.White;
                break;
            case "URBANO":
                btnFiltroUrbano.BackgroundColor = Color.FromArgb("#3B82F6");
                btnFiltroUrbano.TextColor = Colors.White;
                break;
        }
    }

    // ── EVENTOS DE FILTRO ─────────────────────────────────────────────
    private void OnFiltroTodos(object sender, EventArgs e) => AplicarFiltro("TODOS");
    private void OnFiltroRural(object sender, EventArgs e) => AplicarFiltro("RURAL");
    private void OnFiltroUrbano(object sender, EventArgs e) => AplicarFiltro("URBANO");

    // ── EVENTOS DE CABECERA ───────────────────────────────────────────
    private async void OnRefreshOts(object sender, EventArgs e)
    {
        _loaded = false;
        await CargarOtsAsync();
    }

    private async void OnRefreshClicked(object sender, EventArgs e)
    {
        _loaded = false;
        await CargarOtsAsync();
    }

    private async void OnVolverInicioClicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("//HomePage");

    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        try { await SesionMovil.CerrarSesionAsync(); } catch { }
        await Shell.Current.GoToAsync("//LoginPage");
    }

    // ── RUTA GPS ──────────────────────────────────────────────────────
    private async void OnRutaClicked(object sender, EventArgs e)
    {
        if (sender is not Button btn || btn.BindingContext is not CalidadOtModel ot)
            return;

        if (!ot.Latitud.HasValue || !ot.Longitud.HasValue)
        {
            await DisplayAlert("Ruta", "Esta OT no tiene coordenadas GPS.", "OK");
            return;
        }

        var lat = ot.Latitud.Value;
        var lon = ot.Longitud.Value;

        try
        {
            var location = new Location(lat, lon);
            var options = new MapLaunchOptions
            {
                Name = $"{ot.CodSed} - {ot.Modalidad}",
                NavigationMode = NavigationMode.Driving
            };
            await Map.OpenAsync(location, options);
        }
        catch
        {
            var url = $"https://www.google.com/maps?q=" +
                      $"{lat.ToString(System.Globalization.CultureInfo.InvariantCulture)}," +
                      $"{lon.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
            await Launcher.OpenAsync(url);
        }
    }

    // ── EJECUTAR MEDICIÓN ─────────────────────────────────────────────
    private async void OnEjecutarClicked(object sender, EventArgs e)
    {
        if (sender is not Button btn || btn.BindingContext is not CalidadOtModel ot)
            return;

        // Navegar a la página de ejecución pasando los datos necesarios
        var ruta = $"{nameof(EjecutarCalidadPage)}" +
                   $"?OtId={ot.OtId}" +
                   $"&Modalidad={Uri.EscapeDataString(ot.Modalidad)}" +
                   $"&CodSed={Uri.EscapeDataString(ot.CodSed ?? "")}" +
                   $"&Suministro={Uri.EscapeDataString(ot.Suministro ?? "")}" +
                   $"&NombreCliente={Uri.EscapeDataString(ot.NombreCliente ?? "")}" +
                   $"&Periodo={Uri.EscapeDataString(ot.Periodo ?? "")}" +
                   $"&Latitud={ot.Latitud?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? ""}" +
                   $"&Longitud={ot.Longitud?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? ""}";

        await Shell.Current.GoToAsync(ruta);
    }
}
