using AplicacionMovil.Data;

namespace AplicacionMovil.Pages;

public partial class HomePage : ContentPage
{
    public HomePage()
    {
        InitializeComponent();
        Shell.SetNavBarIsVisible(this, false);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Asegura sesión cargada (por si reabre app)
        await SesionMovil.RestaurarAsync();

        lblOperador.Text = $"Operador: {SesionMovil.Usuario ?? "-"}";
        lblZonal.Text = $"Zonal: {SesionMovil.Zona ?? "-"}";
    }

    private async void OnGoReclamosClicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("//MisOtPage");

    private async void OnGoDeficienciasClicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("//DeficienciasHomePage");

    private async void OnGoCalidadClicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("//CalidadProductoHomePage");

    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        await SesionMovil.CerrarSesionAsync(); // si no existe, te lo creo luego
        await Shell.Current.GoToAsync("//LoginPage");
    }
}
