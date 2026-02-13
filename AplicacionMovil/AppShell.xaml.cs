using AplicacionMovil.Pages;

namespace AplicacionMovil;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // ✅ Ruta “oficial” del módulo Reclamos
        Routing.RegisterRoute("Reclamos/Historial", typeof(HistorialPage));

        // ✅ Alias por compatibilidad
        Routing.RegisterRoute("HistorialPage", typeof(HistorialPage));

        Routing.RegisterRoute(nameof(RegistroInterrupcionesPage), typeof(RegistroInterrupcionesPage));

        // ✅ MAPA OT
        Routing.RegisterRoute(nameof(OtMapPage), typeof(OtMapPage));

        //Deficiencias
        Routing.RegisterRoute(nameof(DeficienciasMapPage), typeof(DeficienciasMapPage));
        Routing.RegisterRoute(nameof(MisDeficienciasPage), typeof(MisDeficienciasPage));
    }
}
