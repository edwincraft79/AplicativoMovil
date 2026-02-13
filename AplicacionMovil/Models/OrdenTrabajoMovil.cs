namespace AplicacionMovil.Models
{
    public class OrdenTrabajoMovil
    {
        public int Id { get; set; }
        public string CodigoOt { get; set; } = string.Empty;
        public string CodigoReclamo { get; set; } = string.Empty;
        public string Sucursal { get; set; } = string.Empty;
        public DateTime FechaHoraReclamo { get; set; }
        public string TipoReclamo { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public string NombreReclamante { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public string Gps { get; set; } = string.Empty;

        public string FechaHoraTexto => FechaHoraReclamo.ToString("dd/MM/yyyy HH:mm");

        public double? Latitud { get; set; }
        public double? Longitud { get; set; }
    }
}
