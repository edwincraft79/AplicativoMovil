using SQLite;
using System;

namespace AplicacionMovil.Modules.Deficiencias.Data
{
    [Table("FotosDeficiencias")]
    public class FotoDeficiencia
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public int IdEjecucion { get; set; }
        public string RutaLocal { get; set; }
        public string RutaServidor { get; set; }
        public DateTime FechaCaptura { get; set; }
        public bool Sincronizada { get; set; }
    }
}
