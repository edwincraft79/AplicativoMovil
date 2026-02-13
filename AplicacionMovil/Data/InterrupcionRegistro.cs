// Data/InterrupcionRegistro.cs
using System;
using System.Text.Json.Serialization;

namespace AplicacionMovil.Data
{
    public class InterrupcionRegistro
    {
        public string CodigoReclamo { get; set; } = string.Empty;
        public string Usuario { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }

        public string? ClasificacionReclamo { get; set; }
        public string? NaturalezaInterrupcion { get; set; }
        public string? EquipoReposicion { get; set; }
        public string? FasesReposicion { get; set; }
        public string? DetalleAlumbrado { get; set; }
        public string? DetalleRiesgo { get; set; }

        public bool Desestimado { get; set; }
        public string? DescripcionSolucion { get; set; }

        // ✅ NUEVO: se guarda en el JSON offline
        public string? FotoBase64 { get; set; }

        // ✅ opcional: solo memoria (no JSON)
        [JsonIgnore]
        public byte[]? FotoBytes { get; set; }

        public double? Latitud { get; set; }
        public double? Longitud { get; set; }
        public double? PrecisionGps { get; set; }

        public bool Enviado { get; set; }
    }
}
