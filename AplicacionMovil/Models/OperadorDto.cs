namespace AplicacionMovil.Models
{
    public class OperadorDto
    {
        public string Usuario { get; set; } = "";
        public string? Sucursal { get; set; }
        public string? Zona { get; set; }

        // 👇 ESTA ES LA CLAVE
        public List<string> Moviles { get; set; } = new();
    }
}