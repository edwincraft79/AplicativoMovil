using AplicacionMovil.Modules.Mantenimiento.Models;
using AplicacionMovil.Core.Models;
using AplicacionMovil.Services;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices.Sensors;
using System.Collections.ObjectModel;
using System.Text.Json;

namespace AplicacionMovil.Modules.Mantenimiento.Pages;

public partial class MisOtMantenimientoPage : ContentPage
{
    private readonly List<MantenimientoOtModel> _todasLasOts = new();
    public ObservableCollection<MantenimientoOtModel> OtsFiltradas { get; } = new();

    private bool _loaded;
    private string _filtroActual = "TODOS";

    public MisOtMantenimientoPage()
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

    private async Task CargarOtsAsync()
    {
        try
        {
            lblContadorOts.Text = "Cargando...";
            frameVacio.IsVisible = false;

            await ApiClient.ApplyBearerAsync();

            var resp = await ApiClient.Http.GetAsync("api/mantenimiento-movil/asignadas");
            var ct   = resp.Content.Headers.ContentType?.ToString() ?? "";
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

            var data = JsonSerializer.Deserialize<List<MantenimientoOtModel>>(
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

    private void AplicarFiltro(string filtro)
    {
        _filtroActual = filtro;

        var filtradas = filtro == "TODOS"
            ? _todasLasOts
            : _todasLasOts.Where(o =>
                o.TipoMantenimiento.StartsWith(
                    filtro == "PREVENTIVO" ? "P" : "C",
                    StringComparison.OrdinalIgnoreCase)).ToList();

        OtsFiltradas.Clear();
        foreach (var ot in filtradas)
            OtsFiltradas.Add(ot);

        var total = OtsFiltradas.Count;
        lblContadorOts.Text = total == 0 ? "0 OTs" : $"{total} OT{(total != 1 ? "s" : "")}";
        frameVacio.IsVisible = total == 0;

        ActualizarBotonesFiltro(filtro);
    }

    private void ActualizarBotonesFiltro(string filtro)
    {
        btnFiltroTodos.BackgroundColor = Color.FromArgb("#EDE9FE");
        btnFiltroTodos.TextColor       = Color.FromArgb("#4C1D95");
        btnFiltroPrev.BackgroundColor  = Color.FromArgb("#EDE9FE");
        btnFiltroPrev.TextColor        = Color.FromArgb("#4C1D95");
        btnFiltroCorr.BackgroundColor  = Color.FromArgb("#FEE2E2");
        btnFiltroCorr.TextColor        = Color.FromArgb("#991B1B");

        switch (filtro)
        {
            case "TODOS":
                btnFiltroTodos.BackgroundColor = Color.FromArgb("#7C3AED");
                btnFiltroTodos.TextColor       = Colors.White;
                break;
            case "PREVENTIVO":
                btnFiltroPrev.BackgroundColor  = Color.FromArgb("#7C3AED");
                btnFiltroPrev.TextColor        = Colors.White;
                break;
            case "CORRECTIVO":
                btnFiltroCorr.BackgroundColor  = Color.FromArgb("#DC2626");
                btnFiltroCorr.TextColor        = Colors.White;
                break;
        }
    }

    private void OnFiltroTodos(object sender, EventArgs e)      => AplicarFiltro("TODOS");
    private void OnFiltroPreventivo(object sender, EventArgs e) => AplicarFiltro("PREVENTIVO");
    private void OnFiltroCorrectivo(object sender, EventArgs e) => AplicarFiltro("CORRECTIVO");

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

    private async void OnRutaClicked(object sender, EventArgs e)
    {
        if (sender is not Button btn || btn.BindingContext is not MantenimientoOtModel ot)
            return;

        await DisplayAlert("Ruta", $"OT: {ot.CodigoOT}\nUbicación: {ot.ReferenciaUbicacion ?? "Sin referencia"}", "OK");
    }

    private async void OnMapaCampoClicked(object sender, EventArgs e)
    {
        if (sender is not Button btn || btn.BindingContext is not MantenimientoOtModel ot)
            return;

        await Shell.Current.GoToAsync(nameof(MapaCampoPage), new Dictionary<string, object>
        {
            ["OtId"]    = ot.OtId,
            ["CodigoOT"]= ot.CodigoOT
        });
    }

    private async void OnEjecutarClicked(object sender, EventArgs e)
    {
        if (sender is not Button btn || btn.BindingContext is not MantenimientoOtModel ot)
            return;

        var ruta = $"{nameof(EjecutarMantenimientoPage)}" +
                   $"?OtId={ot.OtId}" +
                   $"&TipoMantenimiento={Uri.EscapeDataString(ot.TipoMantenimiento)}" +
                   $"&CodigoOT={Uri.EscapeDataString(ot.CodigoOT)}" +
                   $"&Nombre={Uri.EscapeDataString(ot.Nombre)}" +
                   $"&ReferenciaUbicacion={Uri.EscapeDataString(ot.ReferenciaUbicacion ?? "")}" +
                   $"&DefinicionProblema={Uri.EscapeDataString(ot.DefinicionProblema ?? "")}";

        await Shell.Current.GoToAsync(ruta);
    }
}
