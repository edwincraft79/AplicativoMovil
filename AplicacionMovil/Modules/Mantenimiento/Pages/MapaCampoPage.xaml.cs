using AplicacionMovil.Services;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AplicacionMovil.Modules.Mantenimiento.Pages;

[QueryProperty(nameof(OtId), "OtId")]
[QueryProperty(nameof(CodigoOT), "CodigoOT")]
public partial class MapaCampoPage : ContentPage
{
    public int OtId { get; set; }
    public string CodigoOT { get; set; } = "";

    private List<FeatureDto> _features = new();
    private FeatureDto? _featureSeleccionado;

    public MapaCampoPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        lblOtInfo.Text = $"OT {CodigoOT}";
        await CargarMapaAsync();
    }

    private async Task CargarMapaAsync()
    {
        try
        {
            await ApiClient.ApplyBearerAsync();
            var resp = await ApiClient.Http.GetAsync($"api/mantenimiento-movil/ot/{OtId}/features");
            if (!resp.IsSuccessStatusCode) { await DisplayAlert("Error", "No se pudieron cargar los puntos.", "OK"); return; }

            var json = await resp.Content.ReadAsStringAsync();
            _features = JsonSerializer.Deserialize<List<FeatureDto>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();

            var ejecutados = _features.Count(f => f.Estado == "Ejecutado");
            lblContador.Text = $"{ejecutados}/{_features.Count} pts";

            var html = GenerarHtmlMapa(_features);
            mapaWebView.Source = new HtmlWebViewSource { Html = html };
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error de red: {ex.Message}", "OK");
        }
    }

    private string GenerarHtmlMapa(List<FeatureDto> features)
    {
        var featuresJson = JsonSerializer.Serialize(features, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var conCoords = features.Where(f => f.Latitud.HasValue && f.Longitud.HasValue).ToList();
        double centerLat = conCoords.Any() ? conCoords.Average(f => f.Latitud!.Value) : -15.8402;
        double centerLon = conCoords.Any() ? conCoords.Average(f => f.Longitud!.Value) : -70.0219;

        return $$"""
<!DOCTYPE html>
<html>
<head>
<meta charset="utf-8"/>
<meta name="viewport" content="width=device-width, initial-scale=1.0, maximum-scale=1.0"/>
<link rel="stylesheet" href="https://unpkg.com/leaflet@1.9.4/dist/leaflet.css"/>
<style>
  html,body,#map { margin:0;padding:0;width:100%;height:100%;font-family:sans-serif; }
  .marker-label { font-size:11px;font-weight:bold;white-space:nowrap; }
  .info-panel { position:absolute;top:10px;right:10px;background:white;
    border-radius:12px;padding:10px 14px;box-shadow:0 2px 10px rgba(0,0,0,.2);
    font-size:12px;z-index:1000;max-width:180px; }
  .legend-dot { display:inline-block;width:12px;height:12px;border-radius:50%;margin-right:6px; }
</style>
</head>
<body>
<div id="map"></div>
<div class="info-panel">
  <div><span class="legend-dot" style="background:#22C55E"></span>Ejecutado</div>
  <div><span class="legend-dot" style="background:#3B82F6"></span>Pendiente</div>
  <div style="margin-top:4px;color:#64748B">Toca un punto para registrar</div>
</div>
<script src="https://unpkg.com/leaflet@1.9.4/dist/leaflet.js"></script>
<script>
const features = {{featuresJson}};
const map = L.map('map').setView([{{centerLat}}, {{centerLon}}], 15);

L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
  attribution: '© OpenStreetMap'
}).addTo(map);

features.forEach(f => {
  const color = f.estado === 'Ejecutado' ? '#22C55E' : '#3B82F6';
  const weight = f.estado === 'Ejecutado' ? 3 : 2;

  if (f.tipo === 'Tramo' && f.geometria) {
    try {
      const geo = JSON.parse(f.geometria);
      L.geoJSON(geo, { style: { color, weight: 4, opacity: 0.8 } })
        .on('click', () => seleccionar(f))
        .addTo(map)
        .bindTooltip(f.nombre, { permanent: false });
    } catch(e) {}
  } else if (f.latitud && f.longitud) {
    const circle = L.circleMarker([f.latitud, f.longitud], {
      radius: 10, color, fillColor: color,
      fillOpacity: 0.85, weight
    }).addTo(map);
    circle.bindTooltip(f.nombre, { permanent: false, direction: 'top' });
    circle.on('click', () => seleccionar(f));
  }
});

function seleccionar(f) {
  window.location.href = 'maui://feature?id=' + f.id
    + '&nombre=' + encodeURIComponent(f.nombre)
    + '&tipo=' + encodeURIComponent(f.tipo)
    + '&estado=' + encodeURIComponent(f.estado);
}

if (features.length > 0) {
  const conCoords = features.filter(f => f.latitud && f.longitud);
  if (conCoords.length > 0) {
    const group = L.featureGroup(
      conCoords.map(f => L.circleMarker([f.latitud, f.longitud]))
    );
    map.fitBounds(group.getBounds().pad(0.2));
  }
}
</script>
</body>
</html>
""";
    }

    private void OnMapaNavigating(object sender, WebNavigatingEventArgs e)
    {
        if (!e.Url.StartsWith("maui://feature")) return;

        e.Cancel = true;

        var uri = new Uri(e.Url);
        var query = uri.Query.TrimStart('?')
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Split('='))
            .Where(p => p.Length == 2)
            .ToDictionary(p => Uri.UnescapeDataString(p[0]), p => Uri.UnescapeDataString(p[1]));

        int featureId = query.TryGetValue("id", out var idStr) && int.TryParse(idStr, out var fid) ? fid : 0;
        var nombre = query.GetValueOrDefault("nombre", "");
        var tipo   = query.GetValueOrDefault("tipo", "Punto");
        var estado = query.GetValueOrDefault("estado", "Pendiente");

        _featureSeleccionado = _features.FirstOrDefault(f => f.Id == featureId)
            ?? new FeatureDto { Id = featureId, Nombre = nombre, Tipo = tipo, Estado = estado };

        MostrarPanelFeature(_featureSeleccionado);
    }

    private void MostrarPanelFeature(FeatureDto f)
    {
        lblFeatureNombre.Text = f.Nombre;
        lblFeatureTipo.Text   = $"{f.Tipo} · Toca para registrar";

        if (f.Estado == "Ejecutado")
        {
            lblEstadoFeature.Text = "✓ Ejecutado";
            lblEstadoFeature.TextColor = Color.FromArgb("#22C55E");
            frameEstadoFeature.BackgroundColor = Color.FromArgb("#F0FDF4");
        }
        else
        {
            lblEstadoFeature.Text = "⏳ Pendiente";
            lblEstadoFeature.TextColor = Color.FromArgb("#F59E0B");
            frameEstadoFeature.BackgroundColor = Color.FromArgb("#FFFBEB");
        }

        panelFeature.IsVisible = true;
    }

    private async void OnRegistrarItemsClicked(object sender, EventArgs e)
    {
        if (_featureSeleccionado == null) return;
        panelFeature.IsVisible = false;

        await Shell.Current.GoToAsync(nameof(RegistroItemCampoPage), new Dictionary<string, object>
        {
            ["OtId"]         = OtId,
            ["CodigoOT"]     = CodigoOT,
            ["FeatureId"]    = _featureSeleccionado.Id,
            ["FeatureNombre"]= _featureSeleccionado.Nombre,
            ["FeatureTipo"]  = _featureSeleccionado.Tipo,
            ["EstadoActual"] = _featureSeleccionado.Estado
        });
    }

    private void OnCerrarPanelClicked(object sender, EventArgs e)
        => panelFeature.IsVisible = false;

    private async void OnActualizarClicked(object sender, EventArgs e)
    {
        panelFeature.IsVisible = false;
        await CargarMapaAsync();
    }

    private async void OnListaClicked(object sender, EventArgs e)
        => await DisplayAlert("Puntos de campo",
            string.Join("\n", _features.Select(f => $"{(f.Estado == "Ejecutado" ? "✓" : "○")} {f.Nombre} ({f.Tipo})")),
            "Cerrar");

    private async void OnVolverClicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("..");

    // ── DTO ────────────────────────────────────────────────────────────────
    private class FeatureDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = "";
        public string Tipo { get; set; } = "Punto";
        public double? Latitud { get; set; }
        public double? Longitud { get; set; }
        public string? Geometria { get; set; }
        public string? Descripcion { get; set; }
        public string Estado { get; set; } = "Pendiente";
    }
}
