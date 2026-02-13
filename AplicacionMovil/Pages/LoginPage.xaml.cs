using AplicacionMovil.Data;
using AplicacionMovil.Models;
using AplicacionMovil.Services;
using System.Net.Http.Json;
using System.Collections.ObjectModel;
using System.Globalization;

namespace AplicacionMovil.Pages;

public partial class LoginPage : ContentPage
{
    private List<OperadorDto> _operadores = new();
    private ObservableCollection<OperadorDto> _sugerenciasOperadores = new();
    private OperadorDto? _operadorSel;
    private bool _suppressTextChanged;

    private bool _passwordVisible = false;

    public LoginPage()
    {
        InitializeComponent();
        Shell.SetNavBarIsVisible(this, false);

        // ✅ enlaza la lista de sugerencias
        cvOperadores.ItemsSource = _sugerenciasOperadores;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CargarOperadoresAsync();
    }

    private async Task CargarOperadoresAsync()
    {
        try
        {
            // OJO: para catálogo normalmente NO debe llevar Bearer
            // pero si tu backend lo exige, descomenta:
            // await ApiClient.ApplyBearerAsync();

            var resp = await ApiClient.Http.GetAsync("api/movil/catalogo/operadores");
            var contentType = resp.Content.Headers.ContentType?.ToString() ?? "(sin content-type)";
            var body = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
            {
                await DisplayAlert("Operadores (ERROR)", await DumpResponseAsync(resp), "OK");
                _operadores = new();
                return;
            }
            // Si viene HTML (login) lo verás aquí
            if (!contentType.Contains("application/json"))
            {
                await DisplayAlert("Operadores (NO JSON)", await DumpResponseAsync(resp), "OK");
                _operadores = new();
                return;
            }

            _operadores = System.Text.Json.JsonSerializer.Deserialize<List<OperadorDto>>(body,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? new();

            
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error operadores", ex.Message, "OK");
            _operadores = new();
        }
    }


    // ===========================
    // AUTOCOMPLETAR OPERADOR
    // ===========================
    private void OnOperadorTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_suppressTextChanged) return;

        var q = (e.NewTextValue ?? "").Trim();

        _operadorSel = null;

        // Limpia móvil SOLO cuando el usuario está escribiendo de verdad
        pkMovil.ItemsSource = null;
        pkMovil.SelectedItem = null;

        _sugerenciasOperadores.Clear();

        if (q.Length < 1)
        {
            panelOperadores.IsVisible = false;
            return;
        }

        var encontrados = _operadores
            .Where(o => (o.Usuario ?? "").StartsWith(q, true, CultureInfo.InvariantCulture))
            .Take(20)
            .ToList();

        foreach (var it in encontrados)
            _sugerenciasOperadores.Add(it);

        panelOperadores.IsVisible = _sugerenciasOperadores.Count > 0;
    }


    private void OnOperadorSelected(object sender, SelectionChangedEventArgs e)
    {
        var item = e.CurrentSelection?.FirstOrDefault() as OperadorDto;
        if (item == null) return;

        SeleccionarOperador(item);
        cvOperadores.SelectedItem = null;
    }

    private void OnOperadorCompleted(object sender, EventArgs e)
    {
        var q = (txtOperador.Text ?? "").Trim();

        var exacto = _operadores.FirstOrDefault(o =>
            string.Equals(o.Usuario, q, StringComparison.OrdinalIgnoreCase));

        if (exacto != null)
            SeleccionarOperador(exacto);
    }

    private void SeleccionarOperador(OperadorDto op)
    {
        _operadorSel = op;

        _suppressTextChanged = true;
        txtOperador.Text = op.Usuario ?? "";
        _suppressTextChanged = false;

        panelOperadores.IsVisible = false;

        pkMovil.ItemsSource = op.Moviles ?? new List<string>();

        // ✅ Si solo hay 1 móvil, lo selecciona automático
        if (op.Moviles != null && op.Moviles.Count == 1)
            pkMovil.SelectedItem = op.Moviles[0];
        else
            pkMovil.SelectedItem = null;

        txtPassword.Focus();
    }


    // ===========================
    // LOGIN
    // ===========================
    private async void OnLoginClicked(object sender, EventArgs e)
    {
        // ✅ Autoselección si escribieron el usuario exacto
        if (_operadorSel == null)
        {
            var u = (txtOperador.Text ?? "").Trim();
            if (!string.IsNullOrWhiteSpace(u))
            {
                var exacto = _operadores.FirstOrDefault(o =>
                    string.Equals(o.Usuario, u, StringComparison.OrdinalIgnoreCase));

                if (exacto != null)
                    SeleccionarOperador(exacto);
            }
        }

        if (_operadorSel == null)
        {
            await DisplayAlert("Error", "Seleccione un operador.", "OK");
            return;
        }

        if (pkMovil.SelectedItem is not string movil || string.IsNullOrWhiteSpace(movil))
        {
            await DisplayAlert("Error", "Seleccione un móvil.", "OK");
            return;
        }

        var pass = txtPassword?.Text?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(pass))
        {
            await DisplayAlert("Error", "Ingrese contraseña.", "OK");
            return;
        }

        var req = new
        {
            userName = _operadorSel.Usuario,
            password = pass,
            codigoMovil = movil
        };

        try
        {
            var resp = await ApiClient.Http.PostAsJsonAsync("api/movil/auth/login", req);

            if (!resp.IsSuccessStatusCode)
            {
                await DisplayAlert("Login (ERROR)", await DumpResponseAsync(resp), "OK");
                return;
            }

            var login = await resp.Content.ReadFromJsonAsync<LoginResponseDto>();
            if (login == null || string.IsNullOrWhiteSpace(login.Token))
            {
                await DisplayAlert("Error", "No se pudo obtener token.", "OK");
                return;
            }

            await SesionMovil.IniciarSesionAsync(
                usuario: login.UserName,
                cliente: login.UserName,
                zona: login.Zona ?? "",
                movil: login.CodigoMovil ?? movil,
                codigoMovil: login.CodigoMovil ?? movil,
                token: login.Token
            );

            await ApiClient.ApplyBearerAsync();
            await Shell.Current.GoToAsync("//HomePage");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "No se pudo iniciar sesión:\n" + ex.Message, "OK");
        }
    }

    private static async Task<string> DumpResponseAsync(HttpResponseMessage resp)
    {
        var ct = resp.Content.Headers.ContentType?.ToString() ?? "(sin content-type)";
        var loc = resp.Headers.Location?.ToString() ?? "(sin Location)";
        var body = await resp.Content.ReadAsStringAsync();

        if (body.Length > 500) body = body.Substring(0, 500);

        return $"HTTP {(int)resp.StatusCode} {resp.StatusCode}\nCT={ct}\nLocation={loc}\n\n{body}";
    }


    private sealed class LoginResponseDto
    {
        public string Token { get; set; } = "";
        public string UserName { get; set; } = "";
        public string? Zona { get; set; }
        public string? CodigoMovil { get; set; }
    }

    // ===========================
    // OJO CONTRASEÑA
    // ===========================
    private void OnTogglePasswordClicked(object sender, EventArgs e)
    {
        _passwordVisible = !_passwordVisible;
        txtPassword.IsPassword = !_passwordVisible;
        btnTogglePassword.Text = _passwordVisible ? "🙈" : "👁️";
    }
}
