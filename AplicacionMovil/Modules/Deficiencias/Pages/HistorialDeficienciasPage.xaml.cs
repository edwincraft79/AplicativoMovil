using AplicacionMovil.Modules.Deficiencias.Data;
using AplicacionMovil.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AplicacionMovil.Modules.Deficiencias.Pages;

public partial class HistorialDeficienciasPage : ContentPage
{
    private readonly ApiService _apiService;
    private readonly DatabaseService _databaseService;
    private List<EjecucionDeficiencia> _lista;

    public HistorialDeficienciasPage()
    {
        InitializeComponent();

        _apiService = new ApiService();
        _databaseService = new DatabaseService();
        _lista = new List<EjecucionDeficiencia>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CargarHistorialAsync();
    }

    // ================== CARGAR HISTORIAL ==================
    private async Task CargarHistorialAsync()
    {
        try
        {
            var userName = Preferences.Get("UserName", "");


            if (string.IsNullOrWhiteSpace(userName))
            {
                await DisplayAlert("Error", "No se pudo obtener el usuario actual", "OK");
                return;
            }

            _lista = await _databaseService.ObtenerTodasLasEjecucionesPorUsuarioAsync(userName);

            foreach (var ejecucion in _lista)
            {
                var fotos = await _databaseService.ObtenerFotosPorEjecucionAsync(ejecucion.Id);
                ejecucion.CantidadFotos = fotos?.Count ?? 0;
            }

            _lista = _lista.OrderByDescending(x => x.FechaEjecucion).ToList();
            cvHistorial.ItemsSource = _lista;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"No se pudo leer el historial.\n{ex.Message}", "OK");
        }
    }


    // ================== SWIPE ENVIAR ==================
    private async void OnEnviarPendienteInvoked(object sender, EventArgs e)
    {
        if (sender is not SwipeItem swipeItem)
            return;

        if (swipeItem.BindingContext is not EjecucionDeficiencia ejecucion)
            return;

        // Ya está enviado
        if (ejecucion.Sincronizado)
        {
            await DisplayAlert("Info", "Este registro ya fue enviado.", "OK");
            return;
        }

        var confirmar = await DisplayAlert(
            "Enviar registro",
            $"¿Desea enviar la ejecución de {ejecucion.CodigoDeficiencia} al servidor?",
            "Sí", "No");

        if (!confirmar)
            return;

        try
        {
            var ok = await EnviarEjecucionAlServidorAsync(ejecucion);

            if (ok)
            {
                ejecucion.Sincronizado = true;
                await _databaseService.MarcarEjecucionComoSincronizadaAsync(ejecucion.Id);

                // refrescar lista
                cvHistorial.ItemsSource = null;
                cvHistorial.ItemsSource = _lista;

                await DisplayAlert("Listo",
                    "El registro se envió correctamente.",
                    "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error",
                $"No se pudo enviar el registro.\n{ex.Message}",
                "OK");
        }
    }

    // ================== ENVIAR AL SERVIDOR ==================
    private async Task<bool> EnviarEjecucionAlServidorAsync(EjecucionDeficiencia ejecucion)
    {
        try
        {
            // Cargar fotos antes de enviar
            var fotos = await _databaseService.ObtenerFotosPorEjecucionAsync(ejecucion.Id);
            ejecucion.Fotos = fotos?.Select(f => f.RutaLocal).ToList() ?? new List<string>();

            // Intentar enviar usando el ApiService
            var (ok, msg) = await _apiService.EnviarSubsanacionAsync(ejecucion);

            if (!ok)
            {
                await DisplayAlert("Servidor",
                    "No se pudo conectar con el servidor o hubo un error en el envío.\n\n" + msg,
                    "OK");
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error",
                $"Error al enviar al servidor: {ex.Message}",
                "OK");
            return false;
        }
    }

    // ================== ELIMINAR ==================
    private async void OnEliminarInvoked(object sender, EventArgs e)
    {
        if (sender is not SwipeItem swipeItem)
            return;

        if (swipeItem.BindingContext is not EjecucionDeficiencia ejecucion)
            return;

        var confirmar = await DisplayAlert(
            "Eliminar registro",
            $"¿Desea eliminar la ejecución de {ejecucion.CodigoDeficiencia} del historial offline?",
            "Sí", "No");

        if (!confirmar)
            return;

        // Eliminar fotos del dispositivo
        var fotos = await _databaseService.ObtenerFotosPorEjecucionAsync(ejecucion.Id);
        foreach (var foto in fotos)
        {
            try
            {
                if (File.Exists(foto.RutaLocal))
                {
                    File.Delete(foto.RutaLocal);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al eliminar foto: {ex.Message}");
            }
        }

        // Eliminar de la base de datos
        await _databaseService.EliminarEjecucionAsync(ejecucion.Id);

        // Eliminar de la lista en memoria
        _lista.Remove(ejecucion);

        // refrescar CollectionView
        cvHistorial.ItemsSource = null;
        cvHistorial.ItemsSource = _lista;
    }

    private async void OnVolverClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}
