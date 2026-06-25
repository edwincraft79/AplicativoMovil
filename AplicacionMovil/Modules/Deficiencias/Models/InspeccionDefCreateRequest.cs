using System;
using System.Text.Json.Serialization;

namespace AplicacionMovil.Modules.Deficiencias.Models
{
    public class InspeccionDefCreateRequest
    {
        public string? UnidadZonal { get; set; }
        public int? CodigoDenunciante { get; set; }
        public int? CodigoTipoInstalacion { get; set; }  // conservado por compatibilidad
        public string? NivelTension { get; set; }        // ✅ Baja Tensión / Media Tensión / Alta Tensión
        public string? CodigoTipificacion { get; set; }
        public string? Observaciones { get; set; }
        public DateTime? FechaInspeccion { get; set; }

        public decimal? UtmEste { get; set; }
        public decimal? UtmNorte { get; set; }

        public decimal? Latitud { get; set; }
        public decimal? Longitud { get; set; }
        public decimal? PrecisionGpsM { get; set; }

        // ✅ SOLO LOCAL (no enviar al API)
        [JsonIgnore]
        public string? RutaFoto { get; set; }
    }
}
