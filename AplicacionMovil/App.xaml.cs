using AplicacionMovil.Data;
using AplicacionMovil.Services;

namespace AplicacionMovil
{
    public partial class App : Application
    {
        private bool _redirigiendoPor401 = false;

        public App(IAuthEvents events)   // 👈 ahora entra por DI
        {
            InitializeComponent();

            MainPage = new AppShell();

            // ✅ Evento global cuando el API responde 401 (token vencido)
            events.SesionExpirada += async () =>
            {
                if (_redirigiendoPor401) return;
                _redirigiendoPor401 = true;

                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await Shell.Current.DisplayAlert("Sesión expirada",
                        "Tu sesión venció. Inicia sesión nuevamente.", "OK");

                    // Limpia tu sesión (si tu SesionMovil ya lo hace, perfecto)
                    try { await SesionMovil.CerrarSesionAsync(); } catch { }

                    await Shell.Current.GoToAsync("//LoginPage");
                    _redirigiendoPor401 = false;
                });
            };

            // ✅ Tu lógica de arranque se mantiene
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                var ok = await SesionMovil.RestaurarAsync();

                if (ok && SesionMovil.EstaLogueado)
                {
                    await ApiClient.ApplyBearerAsync();   // ✅
                    await Shell.Current.GoToAsync("//MisOtPage");
                }
                else
                {
                    await Shell.Current.GoToAsync("//LoginPage");
                }
            });
        }
    }
}
