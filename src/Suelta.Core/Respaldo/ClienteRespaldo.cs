using System.Net;
using System.Net.Http.Json;

namespace Suelta.Core.Respaldo;

public enum ResultadoRed
{
    Ok,

    /// <summary>El servidor no conoce ese código.</summary>
    CodigoInvalido,

    /// <summary>El texto escrito no tiene forma de código de recuperación.</summary>
    FormatoInvalido,

    SinConexion,

    /// <summary>Todavía no se indicó la dirección del servidor.</summary>
    SinServidor,

    /// <summary>Demasiadas peticiones seguidas: hay que esperar un momento.</summary>
    LimiteExcedido,

    ErrorServidor,
}

/// <summary>Habla con la API de respaldo (HT-09). El respaldo es opcional: sin él nada sale del teléfono.</summary>
public sealed class ClienteRespaldo(HttpClient http)
{
    public const string EncabezadoCodigo = "X-Codigo";

    public async Task<(ResultadoRed Resultado, string? Codigo)> CrearCuentaAsync(CancellationToken ct = default)
    {
        var (resultado, respuesta) = await EnviarAsync(HttpMethod.Post, "v1/respaldo", null, null, ct);
        if (resultado != ResultadoRed.Ok) return (resultado, null);

        var cuenta = await LeerAsync(respuesta!, JsonContexto.Default.CuentaCreada, ct);
        return cuenta is null ? (ResultadoRed.ErrorServidor, null) : (ResultadoRed.Ok, cuenta.Codigo);
    }

    public async Task<(ResultadoRed Resultado, SincronizarRespuesta? Respuesta)> SincronizarAsync(
        string codigo, SincronizarSolicitud solicitud, CancellationToken ct = default)
    {
        var contenido = JsonContent.Create(solicitud, JsonContexto.Default.SincronizarSolicitud);
        var (resultado, respuesta) = await EnviarAsync(HttpMethod.Post, "v1/respaldo/sincronizar", codigo, contenido, ct);
        if (resultado != ResultadoRed.Ok) return (resultado, null);

        var datos = await LeerAsync(respuesta!, JsonContexto.Default.SincronizarRespuesta, ct);
        return datos is null ? (ResultadoRed.ErrorServidor, null) : (ResultadoRed.Ok, datos);
    }

    /// <summary>Borra del servidor todo lo que se guardó con ese código (HU-26).</summary>
    public async Task<ResultadoRed> BorrarAsync(string codigo, CancellationToken ct = default) =>
        (await EnviarAsync(HttpMethod.Delete, "v1/respaldo", codigo, null, ct)).Resultado;

    private async Task<(ResultadoRed Resultado, HttpResponseMessage? Respuesta)> EnviarAsync(
        HttpMethod metodo, string ruta, string? codigo, HttpContent? contenido, CancellationToken ct)
    {
        if (http.BaseAddress is null) return (ResultadoRed.SinServidor, null);

        try
        {
            using var peticion = new HttpRequestMessage(metodo, ruta) { Content = contenido };
            if (codigo is not null) peticion.Headers.Add(EncabezadoCodigo, codigo);

            var respuesta = await http.SendAsync(peticion, ct);
            var resultado = respuesta.StatusCode switch
            {
                HttpStatusCode.OK or HttpStatusCode.NoContent => ResultadoRed.Ok,
                HttpStatusCode.Unauthorized => ResultadoRed.CodigoInvalido,
                HttpStatusCode.TooManyRequests => ResultadoRed.LimiteExcedido,
                _ => ResultadoRed.ErrorServidor,
            };

            return (resultado, resultado == ResultadoRed.Ok ? respuesta : null);
        }
        catch (HttpRequestException)
        {
            return (ResultadoRed.SinConexion, null);
        }
        catch (TaskCanceledException)
        {
            // Se acabó el tiempo de espera: para la persona es lo mismo que no tener conexión.
            return (ResultadoRed.SinConexion, null);
        }
    }

    private static async Task<T?> LeerAsync<T>(
        HttpResponseMessage respuesta, System.Text.Json.Serialization.Metadata.JsonTypeInfo<T> tipo, CancellationToken ct)
    {
        try
        {
            return await respuesta.Content.ReadFromJsonAsync(tipo, ct);
        }
        catch (System.Text.Json.JsonException)
        {
            return default;
        }
    }
}
