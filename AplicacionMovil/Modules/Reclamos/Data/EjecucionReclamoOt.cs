using SQLite;
using System;

namespace AplicacionMovil.Modules.Reclamos.Data
{
    [Table("EjecucionesReclamosOt")]
    public class EjecucionReclamoOt
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string CodigoReclamo { get; set; } = "";
        public string Usuario { get; set; } = "";

        public DateTime FechaHoraAtencion { get; set; }
        public DateTime FechaCreacion { get; set; }

        public string ClasificacionReclamo { get; set; } = "";
        public string NaturalezaInterrupcion { get; set; } = "";
        public string EquipoReposicion { get; set; } = "";
        public string FasesReposicion { get; set; } = "";
        public string DetalleAlumbrado { get; set; } = "";
        public string DetalleRiesgo { get; set; } = "";
        public string DescripcionSolucion { get; set; } = "";

        public double? Latitud { get; set; }
        public double? Longitud { get; set; }
        public double? PrecisionGps { get; set; }

        public bool Desestimado { get; set; }

        public string UsuarioEjecucion { get; set; } = "";
        public bool Sincronizado { get; set; }
    }
}

