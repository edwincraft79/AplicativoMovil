using SQLite;
using System;

namespace AplicacionMovil.Modules.Deficiencias.Data
{
    [Table("InspeccionesDeficiencias")]
    public class InspeccionDeficiencia
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }   // 👈 ESTE ES TU ID LOCAL

        public string? UnidadZonal { get; set; }
        public int? CodigoDenunciante { get; set; }
        public int? CodigoTipoInstalacion { get; set; }
        public string? CodigoTipificacion { get; set; }

        public string? Observaciones { get; set; }
        public DateTime? FechaInspeccion { get; set; }

        public decimal? UtmEste { get; set; }
        public decimal? UtmNorte { get; set; }

        public decimal? Latitud { get; set; }
        public decimal? Longitud { get; set; }
        public decimal? PrecisionGpsM { get; set; }
        public string? RutaFoto { get; set; }

        public bool Sincronizado { get; set; }

        public long? IdServidor { get; set; }
        public string? NivelTension { get; set; }
        public DateTime FechaRegistroLocal { get; set; } = DateTime.Now; // 👈 AGREGAR
    }
}
