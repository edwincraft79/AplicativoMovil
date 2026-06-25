using System.Text.Json.Serialization;

namespace AplicacionMovil.Modules.Mantenimiento.Models;

public class MantenimientoOtModel
{
    [JsonPropertyName("otId")]
    public int OtId { get; set; }

    [JsonPropertyName("codigoOT")]
    public string CodigoOT { get; set; } = "";

    [JsonPropertyName("nombre")]
    public string Nombre { get; set; } = "";

    /// <summary>Preventivo | Correctivo | Predictivo</summary>
    [JsonPropertyName("tipoMantenimiento")]
    public string TipoMantenimiento { get; set; } = "";

    /// <summary>Alta | Media | Baja</summary>
    [JsonPropertyName("tipoRed")]
    public string? TipoRed { get; set; }

    [JsonPropertyName("estadoOt")]
    public string? EstadoOt { get; set; }

    [JsonPropertyName("definicionProblema")]
    public string? DefinicionProblema { get; set; }

    [JsonPropertyName("referenciaUbicacion")]
    public string? ReferenciaUbicacion { get; set; }

    [JsonPropertyName("zona")]
    public string? Zona { get; set; }

    [JsonPropertyName("fechaInicio")]
    public DateTime? FechaInicio { get; set; }

    [JsonPropertyName("fechaFin")]
    public DateTime? FechaFin { get; set; }

    [JsonPropertyName("operadorMovil")]
    public string? OperadorMovil { get; set; }

    // ── Helpers de presentación ──────────────────────────────────────────
    [JsonIgnore]
    public string IconoTipo => TipoMantenimiento.StartsWith("P", StringComparison.OrdinalIgnoreCase)
        ? "🔧"   // Preventivo / Predictivo
        : "⚠️";  // Correctivo

    [JsonIgnore]
    public Color ColorTipo => TipoMantenimiento.StartsWith("C", StringComparison.OrdinalIgnoreCase)
        ? Color.FromArgb("#DC2626")   // rojo  — Correctivo
        : Color.FromArgb("#7C3AED"); // violeta — Preventivo/Predictivo

    [JsonIgnore]
    public string FechaInicioTexto => FechaInicio.HasValue
        ? FechaInicio.Value.ToString("dd/MM/yyyy")
        : "—";

    [JsonIgnore]
    public string TipoRedTexto => TipoRed ?? "—";
}
