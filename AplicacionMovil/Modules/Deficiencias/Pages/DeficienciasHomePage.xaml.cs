using System;
using Microsoft.Maui.Controls;

namespace AplicacionMovil.Modules.Deficiencias.Pages;

    public partial class DeficienciasHomePage : ContentPage
    {
        public DeficienciasHomePage()
        {
            InitializeComponent();
            Shell.SetNavBarIsVisible(this, false);
        }

        private async void OnBack(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//HomePage");
        }

        private async void OnVerOt(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//MisDeficienciasPage");
        }

        private async void OnIdentificarDeficiencias(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync(nameof(RegistroInspeccionDefPage));
        }
    }
