using SQLite;
using System;
using System.Collections.Generic;

namespace AplicacionMovil.Modules.Deficiencias.Data
{
    [Table("EjecucionesDeficiencias")]
    public class EjecucionDeficiencia
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string CodigoDeficiencia { get; set; }
        public int IdDeficiencia { get; set; }
        public int IdUsuario { get; set; }
        public DateTime FechaEjecucion { get; set; }
        public DateTime FechaCreacion { get; set; }

        public double? Latitud { get; set; }
        public double? Longitud { get; set; }
        public double? Precision { get; set; }

        public string Observaciones { get; set; }
        public string EstadoSubsanacion { get; set; }
        public string UsuarioEjecucion { get; set; }
        public string UnidadZonal { get; set; }
        public string Alimentador { get; set; }

        public bool Sincronizado { get; set; }

        // Propiedades no mapeadas a la BD
        [Ignore]
        public List<string> Fotos { get; set; }

        [Ignore]
        public int CantidadFotos { get; set; }
    }
}
