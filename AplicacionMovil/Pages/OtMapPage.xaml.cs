using Microsoft.Maui.Controls;
using System.Text.Json;
using AplicacionMovil.Services;
using Microsoft.Maui.ApplicationModel;
using System.Globalization;

namespace AplicacionMovil.Pages
{
    [QueryProperty(nameof(Tipo), "tipo")]
    public partial class OtMapPage : ContentPage
    {
        public string Tipo { get; set; } = "reclamos";

        public OtMapPage()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            try
            {
                await ApiClient.ApplyBearerAsync();

                var tipo = (Tipo ?? "reclamos").Trim().ToLowerInvariant();
                var endpoint = (tipo == "deficiencias")
                    ? "api/movil-mapa/link-deficiencias"
                    : "api/movil-mapa/link-reclamos";

                var resp = await ApiClient.Http.GetAsync(endpoint);
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

                // GPS opcional (no bloquea)
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

        private static string AppendQuery(string url, string key, string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return url;
            var sep = url.Contains("?") ? "&" : "?";
            return url + sep + Uri.EscapeDataString(key) + "=" + Uri.EscapeDataString(value);
        }

        private async void MapaWeb_Navigating(object sender, WebNavigatingEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(e.Url)) return;

                // scheme interno
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

    }

    public class MapLinkVm
    {
        public string url { get; set; } = "";
    }
}
