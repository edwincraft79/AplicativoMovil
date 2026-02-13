namespace AplicacionMovil.Models
{
    public class InterrupcionMovilRequest
    {
        public string CodigoReclamo { get; set; } = string.Empty;
        public string Usuario { get; set; } = string.Empty;
        public DateTime FechaHoraAtencion { get; set; }
        public string DescripcionSolucion { get; set; } = string.Empty;

        public string ClasificacionReclamo { get; set; } = string.Empty;
        public string NaturalezaInterrupcion { get; set; } = string.Empty;
        public string EquipoReposicion { get; set; } = string.Empty;
        public string FasesReposicion { get; set; } = string.Empty;
        public string DetalleAlumbrado { get; set; } = string.Empty;
        public string DetalleRiesgo { get; set; } = string.Empty;
        // ✅ nuevo
        public bool Desestimado { get; set; }

        public string? FotoBase64 { get; set; }
        public double? Latitud { get; set; }
        public double? Longitud { get; set; }
        public double? PrecisionGps { get; set; }
  
    }
}
