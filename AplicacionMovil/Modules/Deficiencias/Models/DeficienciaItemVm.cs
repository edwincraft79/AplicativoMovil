namespace AplicacionMovil.Modules.Deficiencias.Models
{
    public class DeficienciaItemVm
    {
        public long Id { get; set; }

        // Identificación
        public string? CodigoOT { get; set; }
        public string? CodigoEmpresa { get; set; }
        public string? CodigoDeficiencia { get; set; }
        public DateTime? FechaDenuncia { get; set; }
        public DateTime? FechaInspeccion { get; set; }
        public DateTime? FechaSubsanacion { get; set; }

        // Responsable Atención
        public string? Sucursal { get; set; }
        public string? OperadorMovil { get; set; }
        public string? Responsable { get; set; }
        public string? Prioridad { get; set; }

        // Detalles deficiencia
        public string? PuntoUbicacionDeficiencia { get; set; }
        public long? CodigoTipoInstalacion { get; set; }
        public string? CodigoInstalacion { get; set; }
        public string? CodigoTipificacion { get; set; }
        public string? TipificacionTexto { get; set; }  // ← NUEVO CAMPO
        public string? NumeroSuministro { get; set; }
        public string? Alimentador { get; set; }

        // Filtros / campos varios
        public string? UnidadZonal { get; set; }
        public string? EstadoSubsanacion { get; set; }

        // Detalles solución
        public string? PuntoUbicacionSolucion { get; set; }
        public DateTime? FechaSolucion { get; set; }
        public string? ActividadesSubsanacion { get; set; }

        // GPS - Coordenadas convertidas a Lat/Lng
        public double? Latitud { get; set; }
        public double? Longitud { get; set; }

        // Propiedad computada para mostrar Código + Texto
        public string TipificacionCompleta
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(CodigoTipificacion) &&
                    !string.IsNullOrWhiteSpace(TipificacionTexto))
                {
                    return $"{CodigoTipificacion} - {TipificacionTexto}";
                }

                return CodigoTipificacion ?? "-";
            }
        }
    }
}
