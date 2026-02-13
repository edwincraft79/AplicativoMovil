using System.Net;
using System.Net.Http.Headers;
using AplicacionMovil.Data;

namespace AplicacionMovil.Services;

public sealed class AuthHandler : DelegatingHandler
{
    private readonly IAuthEvents _events;

    public AuthHandler(IAuthEvents events)
    {
        _events = events;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // 1) Inyectar token si existe
        var token = SesionMovil.Token;

        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }

        // 2) Ejecutar request
        var response = await base.SendAsync(request, cancellationToken);

        // 3) Si el token venció → 401
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            try
            {
                await SesionMovil.CerrarSesionAsync();
            }
            catch { }

            _events.RaiseSesionExpirada();
        }

        return response;
    }
}
