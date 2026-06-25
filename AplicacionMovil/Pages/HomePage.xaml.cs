using AplicacionMovil.Core.Models;
using AplicacionMovil.Modules.Reclamos.Pages;
using AplicacionMovil.Modules.Deficiencias.Pages;
using AplicacionMovil.Modules.Calidad.Pages;
using AplicacionMovil.Modules.Mantenimiento.Pages;

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

        await SesionMovil.RestaurarAsync();

        lblOperador.Text = $"Operador: {SesionMovil.Usuario ?? "-"}";
    }

    private async void OnGoReclamosClicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync($"//{nameof(MisOtPage)}");

    private async void OnGoDeficienciasClicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync(nameof(MisDeficienciasPage));

    private async void OnGoCalidadClicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync(nameof(MisOtCalidadPage));

    private async void OnGoInspeccionClicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync(nameof(RegistroInspeccionDefPage));

    private async void OnGoMantenimientoClicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync(nameof(MisOtMantenimientoPage));

    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        await SesionMovil.CerrarSesionAsync();
        await Shell.Current.GoToAsync("//LoginPage");
    }
}
