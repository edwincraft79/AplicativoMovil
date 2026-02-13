using AplicacionMovil.Data;
using Microsoft.Maui.Controls;
using System;
using AplicacionMovil.Services;

namespace AplicacionMovil.Pages
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        private async void OnOtClicked(object sender, EventArgs e)
        {
            // Ir al listado de OT (ruta absoluta del Shell)
            await Shell.Current.GoToAsync("//MisOtPage");
        }

        private async void OnActividadesClicked(object sender, EventArgs e)
        {
            await DisplayAlert("Info", "Módulo aún no habilitado.", "OK");
        }

        private async void OnHistorialClicked(object sender, EventArgs e)
        {
            await DisplayAlert("Info", "Historial aún no disponible.", "OK");
        }

        private async void OnLogoutClicked(object sender, EventArgs e)
        {
            await SesionMovil.CerrarSesionAsync();
            await Shell.Current.GoToAsync("//LoginPage");
        }
    }
}
