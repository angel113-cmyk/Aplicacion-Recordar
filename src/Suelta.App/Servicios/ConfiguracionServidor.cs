namespace Suelta.App.Servicios;

/// <summary>
/// La dirección del servidor de respaldo. El respaldo es opcional, así que no hay una dirección fija:
/// la persona (o quien instala la app) la escribe en Ajustes.
/// </summary>
public sealed class ConfiguracionServidor
{
    private const string Clave = "servidor_url";

    private readonly HttpClient _http;

    public ConfiguracionServidor(HttpClient http)
    {
        _http = http;
        Aplicar(Preferences.Get(Clave, ""));
    }

    /// <summary>La dirección guardada, o "" si todavía no hay.</summary>
    public string Direccion => Preferences.Get(Clave, "");

    /// <summary>
    /// Guarda la dirección. Devuelve false si no es válida. Fuera de las pruebas de desarrollo
    /// exige https: el código de recuperación viaja en cada petición.
    /// </summary>
    public bool Establecer(string? direccion)
    {
        var texto = (direccion ?? "").Trim();

        if (texto.Length == 0)
        {
            Preferences.Remove(Clave);
            _http.BaseAddress = null;
            return true;
        }

        if (!EsValida(texto, out var uri)) return false;

        Preferences.Set(Clave, uri.ToString());
        _http.BaseAddress = uri;
        return true;
    }

    private void Aplicar(string direccion)
    {
        _http.BaseAddress = EsValida(direccion, out var uri) ? uri : null;
    }

    private static bool EsValida(string texto, out Uri uri)
    {
        uri = null!;
        if (!Uri.TryCreate(texto.EndsWith('/') ? texto : texto + "/", UriKind.Absolute, out var u)) return false;

        var seguro = u.Scheme == Uri.UriSchemeHttps;
#if DEBUG
        var permitido = seguro || u.Scheme == Uri.UriSchemeHttp;
#else
        var permitido = seguro;
#endif
        if (!permitido) return false;

        uri = u;
        return true;
    }
}
