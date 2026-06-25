using SQLite;

namespace AplicacionMovil.Modules.Reclamos.Data
{
    [Table("FotosReclamosOt")]
    public class FotoReclamoOt
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public int IdEjecucion { get; set; }
        public string RutaLocal { get; set; } = "";
        public bool Sincronizada { get; set; }
        public DateTime FechaCaptura { get; set; }
    }
}
