namespace AplicacionMovil.Services;

public interface IAuthEvents
{
    event Action? SesionExpirada;
    void RaiseSesionExpirada();
}

public sealed class AuthEvents : IAuthEvents
{
    public event Action? SesionExpirada;
    public void RaiseSesionExpirada() => SesionExpirada?.Invoke();
}

