using System.Text.Json.Serialization;

public class DeficienciaItemVm
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("codigoDeficiencia")]
    public string CodigoDeficiencia { get; set; } = "";

    [JsonPropertyName("unidadZonal")]
    public string? UnidadZonal { get; set; }

    [JsonPropertyName("alimentador")]
    public string? Alimentador { get; set; }

    [JsonPropertyName("codigoTipificacion")]
    public string? CodigoTipificacion { get; set; }

    [JsonPropertyName("prioridad")]
    public string? Prioridad { get; set; }

    [JsonPropertyName("estadoSubsanacion")]
    public string EstadoSubsanacion { get; set; } = "";

    [JsonPropertyName("fechaDenuncia")]
    public DateTime? FechaDenuncia { get; set; }

    [JsonPropertyName("latitud")]
    public double? Latitud { get; set; }

    [JsonPropertyName("longitud")]
    public double? Longitud { get; set; }

    [JsonPropertyName("utmEste")]
    public decimal? UtmEste { get; set; }

    [JsonPropertyName("utmNorte")]
    public decimal? UtmNorte { get; set; }
}
