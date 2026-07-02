using AplicacionMovil.Services;
using Microsoft.Maui.Devices.Sensors;
using System.Globalization;
using System.Text.Json;

namespace AplicacionMovil.Modules.Mantenimiento.Pages;

[QueryProperty(nameof(OtId), "OtId")]
[QueryProperty(nameof(CodigoOT), "CodigoOT")]
[QueryProperty(nameof(FeatureId), "FeatureId")]
[QueryProperty(nameof(FeatureNombre), "FeatureNombre")]
[QueryProperty(nameof(FeatureTipo), "FeatureTipo")]
[QueryProperty(nameof(EstadoActual), "EstadoActual")]
[QueryProperty(nameof(LatInicial), "LatInicial")]
[QueryProperty(nameof(LonInicial), "LonInicial")]
[QueryProperty(nameof(ModoRetorno), "ModoRetorno")]
public partial class ConfirmarPuntoCampoPage : ContentPage
{
    public int OtId { get; set; }
    public string CodigoOT { get; set; } = "";
    public int FeatureId { get; set; }
    public string FeatureNombre { get; set; } = "";
    public string FeatureTipo { get; set; } = "Punto";
    public string EstadoActual { get; set; } = "Pendiente";

    // Llegan como string porque Shell no pasa nulls de forma confiable en QueryProperty.
    public string? LatInicial { get; set; }
    public string? LonInicial { get; set; }

    // "ejecucion" = viene de EjecutarMantenimientoPage y se debe volver ahí con el punto
    // confirmado (preserva lo que el técnico ya llenó). Vacío/otro = flujo original por
    // feature GIS, continúa hacia RegistroItemCampoPage.
    public string? ModoRetorno { get; set; }

    private double _lat;
    private double _lon;
    private string? _sedActiva;

    public ConfirmarPuntoCampoPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await UbicarYCargarMapaAsync();
    }

    private async Task UbicarYCargarMapaAsync()
    {
        var latIni = 0d;
        var lonIni = 0d;
        var tieneCoordInicial =
            double.TryParse(LatInicial, NumberStyles.Float, CultureInfo.InvariantCulture, out latIni) &&
            double.TryParse(LonInicial, NumberStyles.Float, CultureInfo.InvariantCulture, out lonIni) &&
            latIni != 0 && lonIni != 0;

        if (tieneCoordInicial)
        {
            _lat = latIni;
            _lon = lonIni;
        }
        else
        {
            lblEstadoMapa.Text = "Obteniendo ubicación...";
            try
            {
                var req = new GeolocationRequest(GeolocationAccuracy.High, TimeSpan.FromSeconds(10));
                var loc = await Geolocation.GetLocationAsync(req);
                if (loc != null)
                {
                    _lat = loc.Latitude;
                    _lon = loc.Longitude;
                }
                else
                {
                    _lat = -15.8402; // fallback: Puno
                    _lon = -70.0219;
                }
            }
            catch
            {
                _lat = -15.8402;
                _lon = -70.0219;
            }
        }

        await CargarMapaAsync();
    }

    private async Task CargarMapaAsync()
    {
        lblEstadoMapa.Text = "Cargando red primaria de referencia...";

        // bbox ~800m alrededor del punto (red primaria: capa liviana, siempre visible)
        const double d = 0.008;
        var bbox = $"{(_lon - d).ToString(CultureInfo.InvariantCulture)},{(_lat - d).ToString(CultureInfo.InvariantCulture)}," +
                   $"{(_lon + d).ToString(CultureInfo.InvariantCulture)},{(_lat + d).ToString(CultureInfo.InvariantCulture)}";

        await ApiClient.ApplyBearerAsync();

        var mtTramosJson = await FetchRawAsync($"api/gis/tramos-mt-bbox?bbox={Uri.EscapeDataString(bbox)}");
        var sedJson = await FetchRawAsync($"api/gis/subestaciones-bbox?bbox={Uri.EscapeDataString(bbox)}");

        var html = GenerarHtmlMapa(_lat, _lon, mtTramosJson, sedJson);
        mapaWebView.Source = new HtmlWebViewSource { Html = html };

        lblEstadoMapa.Text = "Toca una subestación (⚡) para ver su red secundaria";
    }

    private async Task<string> FetchRawAsync(string url)
    {
        try
        {
            var resp = await ApiClient.Http.GetAsync(url);
            if (!resp.IsSuccessStatusCode) return "null";
            return await resp.Content.ReadAsStringAsync();
        }
        catch { return "null"; }
    }

    private static string GenerarHtmlMapa(double lat, double lon, string mtTramosJson, string sedJson)
    {
        var latInv = lat.ToString(CultureInfo.InvariantCulture);
        var lonInv = lon.ToString(CultureInfo.InvariantCulture);

        return $$"""
<!DOCTYPE html>
<html>
<head>
<meta charset="utf-8"/>
<meta name="viewport" content="width=device-width, initial-scale=1.0, maximum-scale=1.0"/>
<link rel="stylesheet" href="https://unpkg.com/leaflet@1.9.4/dist/leaflet.css"/>
<style>
  html,body,#map { margin:0;padding:0;width:100%;height:100%;font-family:sans-serif; }
  .sed-triangle {
    width: 0; height: 0;
    border-left: 9px solid transparent;
    border-right: 9px solid transparent;
    border-bottom: 16px solid #ff4fa3; /* Distribución: tiene red secundaria */
  }
  .sed-triangle-util {
    width: 0; height: 0;
    border-left: 9px solid transparent;
    border-right: 9px solid transparent;
    border-bottom: 16px solid #7c3aed; /* Utilización: no tiene red secundaria */
  }
</style>
</head>
<body>
<div id="map"></div>
<script src="https://unpkg.com/leaflet@1.9.4/dist/leaflet.js"></script>
<script>
const mtTramos = {{mtTramosJson}};
const seds = {{sedJson}};

const map = L.map('map').setView([{{latInv}}, {{lonInv}}], 18);

const capaCalles = L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
  maxZoom: 20, attribution: '© OpenStreetMap'
}).addTo(map);

const capaSatelital = L.tileLayer('https://server.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{z}/{y}/{x}', {
  maxZoom: 20, attribution: 'Tiles © Esri'
});

L.control.layers({ 'Calles': capaCalles, 'Satélite': capaSatelital }, {}, { position: 'topright', collapsed: true }).addTo(map);

// ── Red primaria (MT): siempre visible, capa liviana (mismo color que el GIS web) ──
if (mtTramos) {
  try { L.geoJSON(mtTramos, { style: { color: '#ff0000', weight: 2, opacity: 0.9 } }).addTo(map); } catch (e) {}
}

let capaSecundaria = null;

if (seds) {
  try {
    L.geoJSON(seds, {
      pointToLayer: (f, ll) => {
        const uso = ((f?.properties?.sed_uso_se) ?? "").toString().trim().toLowerCase();
        const esUtilizacion = uso.includes("utiliz");
        const cls = esUtilizacion ? "sed-triangle-util" : "sed-triangle";
        const m = L.marker(ll, {
          icon: L.divIcon({ className: "", html: `<div class="${cls}"></div>`, iconSize: [18, 16], iconAnchor: [9, 16] })
        });
        const cod = f.properties.sed_cod__1;
        m.bindTooltip((f.properties.sed_etq__1 || cod || 'SED') + (esUtilizacion ? ' (utilización, sin red secundaria)' : ''), { direction: 'top' });
        m.on('click', () => {
          if (esUtilizacion) {
            window.location.href = 'maui://sed-sin-bt?cod=' + encodeURIComponent(cod || '');
          } else {
            window.location.href = 'maui://sed?cod=' + encodeURIComponent(cod || '');
          }
        });
        return m;
      }
    }).addTo(map);
  } catch (e) {}
}

function mostrarRedSecundaria(tramosRaw, estructurasRaw) {
  if (capaSecundaria) { map.removeLayer(capaSecundaria); capaSecundaria = null; }
  const grupo = L.layerGroup();
  // Mismos colores que el GIS web: Tramo BT #2563eb, Estr. BT relleno #8b5a2b / borde #5a3a1a
  try { L.geoJSON(JSON.parse(tramosRaw), { style: { color: '#2563eb', weight: 3, opacity: 0.9 } }).addTo(grupo); } catch (e) {}
  try {
    L.geoJSON(JSON.parse(estructurasRaw), {
      pointToLayer: (f, ll) => L.circleMarker(ll, { radius: 4, color: '#5a3a1a', fillColor: '#8b5a2b', weight: 1.5, fillOpacity: .9 })
    }).addTo(grupo);
  } catch (e) {}
  grupo.addTo(map);
  capaSecundaria = grupo;
  try { map.fitBounds(grupo.getBounds().pad(0.15)); } catch (e) {}
}

function ocultarRedSecundaria() {
  if (capaSecundaria) { map.removeLayer(capaSecundaria); capaSecundaria = null; }
}

let puntoNuevo = L.marker([{{latInv}}, {{lonInv}}], { draggable: true })
  .addTo(map)
  .bindPopup('Arrastra a la ubicación exacta');

function moverPuntoA(lat, lon) {
  puntoNuevo.setLatLng([lat, lon]);
  map.panTo([lat, lon]);
}

function confirmarPunto() {
  const p = puntoNuevo.getLatLng();
  window.location.href = 'maui://confirmar?lat=' + p.lat.toFixed(6) + '&lon=' + p.lng.toFixed(6);
}
</script>
</body>
</html>
""";
    }

    private async void OnUsarUbicacionClicked(object sender, EventArgs e)
    {
        try
        {
            var req = new GeolocationRequest(GeolocationAccuracy.High, TimeSpan.FromSeconds(10));
            var loc = await Geolocation.GetLocationAsync(req);
            if (loc == null) return;

            var lat = loc.Latitude.ToString(CultureInfo.InvariantCulture);
            var lon = loc.Longitude.ToString(CultureInfo.InvariantCulture);
            await mapaWebView.EvaluateJavaScriptAsync($"moverPuntoA({lat},{lon})");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error GPS", ex.Message, "OK");
        }
    }

    private async void OnConfirmarClicked(object sender, EventArgs e)
        => await mapaWebView.EvaluateJavaScriptAsync("confirmarPunto()");

    private async void OnVolverClicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("..");

    private async void OnMapaNavigating(object sender, WebNavigatingEventArgs e)
    {
        if (e.Url.StartsWith("maui://sed-sin-bt"))
        {
            e.Cancel = true;
            await DisplayAlert("Sin red secundaria",
                "Esta subestación es de utilización: no tiene red secundaria (BT) digitalizada.", "OK");
            return;
        }

        if (e.Url.StartsWith("maui://sed"))
        {
            e.Cancel = true;
            var query = ParseQuery(e.Url);
            var cod = query.GetValueOrDefault("cod", "");
            if (string.IsNullOrWhiteSpace(cod)) return;

            if (_sedActiva == cod)
            {
                // Ya está activa: la toca de nuevo para ocultarla (no repetir la descarga)
                _sedActiva = null;
                lblEstadoMapa.Text = "Toca una subestación (⚡) para ver su red secundaria";
                await mapaWebView.EvaluateJavaScriptAsync("ocultarRedSecundaria()");
                return;
            }

            _sedActiva = cod;
            lblEstadoMapa.Text = $"Cargando red secundaria de {cod}...";

            try
            {
                await ApiClient.ApplyBearerAsync();
                var bundleJson = await FetchRawAsync(
                    $"api/gis/subestaciones-bundle?sedCod={Uri.EscapeDataString(cod)}&includeBt=true");
                using var doc = JsonDocument.Parse(bundleJson);
                if (doc.RootElement.TryGetProperty("bt", out var bt))
                {
                    var tramos = bt.TryGetProperty("tramos", out var t) ? t.GetString() ?? "null" : "null";
                    var estructuras = bt.TryGetProperty("estructuras", out var es) ? es.GetString() ?? "null" : "null";

                    var tramosJs = JsonSerializer.Serialize(tramos);
                    var estructurasJs = JsonSerializer.Serialize(estructuras);
                    await mapaWebView.EvaluateJavaScriptAsync($"mostrarRedSecundaria({tramosJs}, {estructurasJs})");
                }
                lblEstadoMapa.Text = $"Red secundaria de {cod} cargada — toca de nuevo para ocultarla";
            }
            catch (Exception ex)
            {
                lblEstadoMapa.Text = "No se pudo cargar la red secundaria";
                await DisplayAlert("Error", ex.Message, "OK");
            }
            return;
        }

        if (!e.Url.StartsWith("maui://confirmar")) return;

        e.Cancel = true;
        {
            var query = ParseQuery(e.Url);

            var lat = double.TryParse(query.GetValueOrDefault("lat"), NumberStyles.Float, CultureInfo.InvariantCulture, out var la) ? la : _lat;
            var lon = double.TryParse(query.GetValueOrDefault("lon"), NumberStyles.Float, CultureInfo.InvariantCulture, out var lo) ? lo : _lon;

            if (ModoRetorno == "ejecucion")
            {
                // Vuelve a EjecutarMantenimientoPage (misma instancia: conserva resultado,
                // observaciones y fotos ya ingresados) con el punto confirmado.
                var latStr = lat.ToString(CultureInfo.InvariantCulture);
                var lonStr = lon.ToString(CultureInfo.InvariantCulture);
                await Shell.Current.GoToAsync($"..?Lat={Uri.EscapeDataString(latStr)}&Lon={Uri.EscapeDataString(lonStr)}");
                return;
            }

            await Shell.Current.GoToAsync(nameof(RegistroItemCampoPage), new Dictionary<string, object>
            {
                ["OtId"] = OtId,
                ["CodigoOT"] = CodigoOT,
                ["FeatureId"] = FeatureId,
                ["FeatureNombre"] = FeatureNombre,
                ["FeatureTipo"] = FeatureTipo,
                ["EstadoActual"] = EstadoActual,
                ["Lat"] = lat,
                ["Lon"] = lon,
            });
        }
    }

    private static Dictionary<string, string> ParseQuery(string url)
    {
        var uri = new Uri(url);
        return uri.Query.TrimStart('?')
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Split('='))
            .Where(p => p.Length == 2)
            .ToDictionary(p => Uri.UnescapeDataString(p[0]), p => Uri.UnescapeDataString(p[1]));
    }
}
