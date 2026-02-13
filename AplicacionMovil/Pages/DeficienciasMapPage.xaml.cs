using AplicacionMovil.Services;
using Microsoft.Maui.ApplicationModel;
using System.Globalization;
using System.Text.Json;

namespace AplicacionMovil.Pages
{
    public partial class DeficienciasMapPage : ContentPage
    {
        public DeficienciasMapPage()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            try
            {
                await ApiClient.ApplyBearerAsync();

                // ✅ IMPORTANTE: link de DEFICIENCIAS
                var resp = await ApiClient.Http.GetAsync("api/movil-mapa/link-deficiencias");
                var body = await resp.Content.ReadAsStringAsync();

                if (!resp.IsSuccessStatusCode)
                {
                    var msg = body.Length > 200 ? body.Substring(0, 200) : body;
                    await DisplayAlert("Mapa", $"No se pudo generar el link.\nHTTP {(int)resp.StatusCode}\n{msg}", "OK");
                    return;
                }

                var r = JsonSerializer.Deserialize<MapLinkVm>(body,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (r == null || string.IsNullOrWhiteSpace(r.url))
                {
                    await DisplayAlert("Mapa", "No se pudo generar el link del mapa.", "OK");
                    return;
                }

                string finalUrl = r.url;

                // GPS opcional (no bloquea el mapa)
                var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
                if (status != PermissionStatus.Granted)
                    status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();

                if (status == PermissionStatus.Granted)
                {
                    try
                    {
                        var loc = await Geolocation.GetLocationAsync(
                            new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10)))
                            ?? await Geolocation.GetLastKnownLocationAsync();

                        if (loc != null)
                        {
                            finalUrl = AppendQuery(finalUrl, "myLat", loc.Latitude.ToString(CultureInfo.InvariantCulture));
                            finalUrl = AppendQuery(finalUrl, "myLng", loc.Longitude.ToString(CultureInfo.InvariantCulture));
                        }
                    }
                    catch { }
                }

                MapaWeb.Source = new UrlWebViewSource { Url = finalUrl };
            }
            catch (Exception ex)
            {
                await DisplayAlert("Mapa", "Error cargando mapa: " + ex.Message, "OK");
            }
        }

        // ✅ Intercepta elpu://route para abrir Google Maps fuera del WebView
        private async void MapaWeb_Navigating(object sender, WebNavigatingEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(e.Url)) return;

                if (e.Url.StartsWith("elpu://route", StringComparison.OrdinalIgnoreCase))
                {
                    e.Cancel = true;

                    var uri = new Uri(e.Url);
                    var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
                    var url = query.Get("url");

                    if (!string.IsNullOrWhiteSpace(url))
                        await Launcher.Default.OpenAsync(url);

                    return;
                }

                if (e.Url.Contains("google.com/maps", StringComparison.OrdinalIgnoreCase))
                {
                    e.Cancel = true;
                    await Launcher.Default.OpenAsync(e.Url);
                }
            }
            catch { }
        }

        private static string AppendQuery(string url, string key, string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return url;
            var sep = url.Contains("?") ? "&" : "?";
            return url + sep + Uri.EscapeDataString(key) + "=" + Uri.EscapeDataString(value);
        }

        public class MapLinkVm
        {
            public string url { get; set; } = "";
        }
    }
}
