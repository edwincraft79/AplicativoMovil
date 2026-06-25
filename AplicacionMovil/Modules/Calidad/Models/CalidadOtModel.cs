using System.Text.Json.Serialization;

namespace AplicacionMovil.Modules.Calidad.Models;

/// <summary>
/// Mapea el CalidadOtAsignadaDto que devuelve /api/calidad-movil/asignadas
/// </summary>
public class CalidadOtModel
{
    [JsonPropertyName("otId")]
    public long OtId { get; set; }

    [JsonPropertyName("padronId")]
    public long PadronId { get; set; }

    [JsonPropertyName("modalidad")]
    public string Modalidad { get; set; } = "";   // "RURAL" | "URBANO"

    [JsonPropertyName("estadoOt")]
    public string? EstadoOt { get; set; }

    [JsonPropertyName("fechaAsignacion")]
    public DateTime? FechaAsignacion { get; set; }

    [JsonPropertyName("operadorMovil")]
    public string? OperadorMovil { get; set; }

    // ── Datos del padrón ─────────────────────────────────────────────
    [JsonPropertyName("codSed")]
    public string? CodSed { get; set; }

    [JsonPropertyName("sucursal")]
    public string? Sucursal { get; set; }

    [JsonPropertyName("periodo")]
    public string? Periodo { get; set; }

    [JsonPropertyName("campana")]
    public long? Campana { get; set; }

    [JsonPropertyName("estado")]
    public string? Estado { get; set; }

    // ── Suministro(s) ─────────────────────────────────────────────────
    [JsonPropertyName("suministro")]
    public string? Suministro { get; set; }

    [JsonPropertyName("nombreCliente")]
    public string? NombreCliente { get; set; }

    [JsonPropertyName("suministroCola")]
    public string? SuministroCola { get; set; }      // solo rural

    [JsonPropertyName("nombreClienteCola")]
    public string? NombreClienteCola { get; set; }   // solo rural

    [JsonPropertyName("alimentador")]
    public string? Alimentador { get; set; }         // solo urbano

    // ── Compensación ─────────────────────────────────────────────────
    [JsonPropertyName("compPeriodo")]
    public double? CompPeriodo { get; set; }

    [JsonPropertyName("compAcumulada")]
    public double? CompAcumulada { get; set; }

    [JsonPropertyName("afectadosTotal")]
    public int? AfectadosTotal { get; set; }

    // ── GPS ───────────────────────────────────────────────────────────
    [JsonPropertyName("latitud")]
    public double? Latitud { get; set; }

    [JsonPropertyName("longitud")]
    public double? Longitud { get; set; }

    // ── Helpers de presentación ───────────────────────────────────────
    /// <summary>Icono según modalidad para mostrar en la tarjeta.</summary>
    [JsonIgnore]
    public string IconoModalidad => Modalidad == "RURAL" ? "🌾" : "🏘️";

    /// <summary>Color de la barra superior de la tarjeta.</summary>
    [JsonIgnore]
    public Color ColorModalidad => Modalidad == "RURAL"
        ? Color.FromArgb("#10B981")   // verde
        : Color.FromArgb("#3B82F6");  // azul

    /// <summary>Suministro principal para mostrar (cabecera o único).</summary>
    [JsonIgnore]
    public string SuministroPrincipal => Suministro ?? "-";

    /// <summary>Compensación formateada.</summary>
    [JsonIgnore]
    public string CompPeriodoTexto => CompPeriodo.HasValue
        ? $"S/ {CompPeriodo.Value:N2}"
        : "—";

    /// <summary>Fecha de asignación formateada.</summary>
    [JsonIgnore]
    public string FechaTexto => FechaAsignacion.HasValue
        ? FechaAsignacion.Value.ToLocalTime().ToString("dd/MM/yyyy HH:mm")
        : "—";
}
