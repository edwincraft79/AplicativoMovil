namespace AplicacionMovil.Pages;

public partial class CalidadProductoHomePage : ContentPage
{
    public CalidadProductoHomePage()
    {
        InitializeComponent();
        Shell.SetNavBarIsVisible(this, false);
    }

    private async void OnVerOtCalidad(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("Calidad/OT");
    }

    private async void OnActividadesLevantamiento(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("Calidad/Levantamiento");
    }

    private async void OnBack(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//HomePage");
    }
}
