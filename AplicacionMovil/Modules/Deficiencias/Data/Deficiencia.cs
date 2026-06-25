using SQLite;
using System;

namespace AplicacionMovil.Modules.Deficiencias.Data
{
    /// <summary>
    /// Modelo de Deficiencia para representar las deficiencias asignadas
    /// Si ya tienes este modelo, ignora este archivo
    /// </summary>
    [Table("Deficiencias")]
    public class Deficiencia
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string CodigoDeficiencia { get; set; }
        public int IdDeficiencia { get; set; }
        public int IdUsuario { get; set; }

        public string UnidadZonal { get; set; }
        public string Alimentador { get; set; }
        public string TipificacionCompleta { get; set; }

        public DateTime FechaDenuncia { get; set; }
        public DateTime FechaAsignacion { get; set; }

        public string EstadoSubsanacion { get; set; }
        public string Prioridad { get; set; }

        public double? Latitud { get; set; }
        public double? Longitud { get; set; }

        public string Observaciones { get; set; }
    }
}
