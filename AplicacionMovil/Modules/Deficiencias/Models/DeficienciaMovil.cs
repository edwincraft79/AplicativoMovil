namespace AplicacionMovil.Modules.Deficiencias.Models
{
    public class DeficienciaMovil
    {
        public int IdDeficiencia { get; set; }
        public string CodigoDeficiencia { get; set; } = string.Empty;
        public string UnidadZonal { get; set; } = string.Empty;
        public string Alimentador { get; set; } = string.Empty;
        public string CodigoTipificacion { get; set; } = string.Empty;
        public string TipificacionTexto { get; set; } = string.Empty;
        public string EstadoSubsanacion { get; set; } = string.Empty;
        public DateTime FechaDenuncia { get; set; }

        public string FechaTexto => FechaDenuncia.ToString("dd/MM/yyyy HH:mm");

        public double? Latitud { get; set; }
        public double? Longitud { get; set; }
    }
}
