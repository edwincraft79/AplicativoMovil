using AplicacionMovil.Modules.Reclamos.Pages;
using AplicacionMovil.Modules.Deficiencias.Pages;
using AplicacionMovil.Modules.Calidad.Pages;
using AplicacionMovil.Modules.Mantenimiento.Pages;
using AplicacionMovil.Modules.Suministros.Pages;

namespace AplicacionMovil;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // ✅ Reclamos
        Routing.RegisterRoute("Reclamos/Historial", typeof(HistorialPage));
        Routing.RegisterRoute("HistorialPage", typeof(HistorialPage));
        Routing.RegisterRoute(nameof(RegistroInterrupcionesPage), typeof(RegistroInterrupcionesPage));
        Routing.RegisterRoute(nameof(OtMapPage), typeof(OtMapPage));

        // ✅ Deficiencias
        Routing.RegisterRoute(nameof(DeficienciasMapPage), typeof(DeficienciasMapPage));
        Routing.RegisterRoute("RegistroDeficienciasPage", typeof(RegistroDeficienciasPage));
        Routing.RegisterRoute(nameof(MisDeficienciasPage), typeof(MisDeficienciasPage));
        Routing.RegisterRoute(nameof(HistorialDeficienciasPage), typeof(HistorialDeficienciasPage));
        Routing.RegisterRoute(nameof(RegistroInspeccionDefPage), typeof(RegistroInspeccionDefPage));

        // ✅ Calidad de Producto
        Routing.RegisterRoute(nameof(MisOtCalidadPage), typeof(MisOtCalidadPage));
        Routing.RegisterRoute(nameof(EjecutarCalidadPage), typeof(EjecutarCalidadPage));

        // ✅ Mantenimiento
        Routing.RegisterRoute(nameof(MisOtMantenimientoPage), typeof(MisOtMantenimientoPage));
        Routing.RegisterRoute(nameof(EjecutarMantenimientoPage), typeof(EjecutarMantenimientoPage));
        Routing.RegisterRoute(nameof(MapaCampoPage), typeof(MapaCampoPage));
        Routing.RegisterRoute(nameof(ConfirmarPuntoCampoPage), typeof(ConfirmarPuntoCampoPage));
        Routing.RegisterRoute(nameof(RegistroItemCampoPage), typeof(RegistroItemCampoPage));

        // ✅ Suministros
        Routing.RegisterRoute(nameof(MisSuministrosPage), typeof(MisSuministrosPage));
        Routing.RegisterRoute(nameof(MapaSuministroPage), typeof(MapaSuministroPage));
        Routing.RegisterRoute(nameof(ConfirmarSuministroPage), typeof(ConfirmarSuministroPage));
    }
}
