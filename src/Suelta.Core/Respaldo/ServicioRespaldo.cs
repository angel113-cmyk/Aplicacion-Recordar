using Suelta.Core.Servicios;

namespace Suelta.Core.Respaldo;

/// <summary>Lo único que el teléfono recuerda del respaldo. En la app va en el almacenamiento seguro.</summary>
public interface IAlmacenRespaldo
{
    /// <summary>El código de recuperación (16 caracteres, sin guiones). Null si el respaldo no está activo.</summary>
    string? Codigo { get; set; }

    /// <summary>Hasta cuándo se enviaron los cambios de este teléfono (reloj del teléfono, UTC).</summary>
    DateTime? CursorLocalUtc { get; set; }

    /// <summary>Desde dónde pedir novedades al servidor (el cursor que devolvió).</summary>
    DateTime? CursorServidorUtc { get; set; }
}

public sealed record ResultadoSincronizacion(ResultadoRed Resultado, int Enviados, int Recibidos)
{
    public bool Ok => Resultado == ResultadoRed.Ok;
}

/// <summary>
/// El teléfono guarda todo primero; el servidor solo respalda (HU-26, HT-11). Funciona sin red:
/// los cambios se acumulan y se envían cuando vuelve la conexión. Gana la última modificación.
/// </summary>
public sealed class ServicioRespaldo(
    IRecordatorioRepositorio repositorio,
    ServicioRecordatorios recordatorios,
    ClienteRespaldo cliente,
    IAlmacenRespaldo almacen)
{
    private readonly SemaphoreSlim _candado = new(1, 1);

    public bool Activo => almacen.Codigo is not null;

    /// <summary>El código como se muestra a la persona: XXXX-XXXX-XXXX-XXXX.</summary>
    public string? CodigoFormateado => almacen.Codigo is null ? null : CodigoRecuperacion.Formatear(almacen.Codigo);

    /// <summary>Activar respaldo: se genera un código y los recordatorios se guardan en el servidor.</summary>
    public async Task<ResultadoRed> ActivarAsync(CancellationToken ct = default)
    {
        if (Activo) return ResultadoRed.Ok;

        var (resultado, codigo) = await cliente.CrearCuentaAsync(ct);
        if (resultado != ResultadoRed.Ok) return resultado;

        var normalizado = CodigoRecuperacion.Normalizar(codigo);
        if (normalizado is null) return ResultadoRed.ErrorServidor;

        Guardar(normalizado);

        // Si ahora no hay red, el respaldo ya quedó activo y se completa en la próxima sincronización.
        await SincronizarAsync(ct);
        return ResultadoRed.Ok;
    }

    /// <summary>Recuperar en un teléfono nuevo con el código de recuperación.</summary>
    public async Task<ResultadoRed> RecuperarAsync(string? codigoEscrito, CancellationToken ct = default)
    {
        var normalizado = CodigoRecuperacion.Normalizar(codigoEscrito);
        if (normalizado is null) return ResultadoRed.FormatoInvalido;

        var anterior = almacen.Codigo;
        Guardar(normalizado);

        var sincronizacion = await SincronizarAsync(ct);
        if (!sincronizacion.Ok)
        {
            // Un código que el servidor no conoce, o sin conexión: no se queda a medias.
            Guardar(anterior);
            return sincronizacion.Resultado;
        }

        return ResultadoRed.Ok;
    }

    /// <summary>Envía lo que cambió aquí y trae lo que cambió en otros teléfonos.</summary>
    public async Task<ResultadoSincronizacion> SincronizarAsync(CancellationToken ct = default)
    {
        var codigo = almacen.Codigo;
        if (codigo is null) return new ResultadoSincronizacion(ResultadoRed.Ok, 0, 0);

        await _candado.WaitAsync(ct);
        try
        {
            // Todo lo modificado antes de este instante va en este envío; lo que se modifique después, en el próximo.
            var inicio = DateTime.UtcNow;

            var cambios = await repositorio.ListarCambiosDesdeAsync(
                almacen.CursorLocalUtc is { } cursor ? ZonaLima.DeUtc(cursor) : null);
            var dtos = cambios.Select(DtoMapper.ADto).ToList();

            var (resultado, respuesta) = await cliente.SincronizarAsync(
                codigo, new SincronizarSolicitud(almacen.CursorServidorUtc, dtos), ct);
            if (resultado != ResultadoRed.Ok) return new ResultadoSincronizacion(resultado, 0, 0);

            var recibidos = await AplicarAsync(respuesta!.Cambios);

            almacen.CursorLocalUtc = inicio;
            almacen.CursorServidorUtc = respuesta.CursorUtc;
            return new ResultadoSincronizacion(ResultadoRed.Ok, dtos.Count, recibidos);
        }
        finally
        {
            _candado.Release();
        }
    }

    /// <summary>Borrar mis datos: se eliminan del servidor y el respaldo queda desactivado (HU-26, HT-13).</summary>
    public async Task<ResultadoRed> BorrarMisDatosAsync(CancellationToken ct = default)
    {
        var codigo = almacen.Codigo;
        if (codigo is null) return ResultadoRed.Ok;

        var resultado = await cliente.BorrarAsync(codigo, ct);

        // Si el servidor ya no conoce el código, los datos ya no están: queda igual de borrado.
        if (resultado is ResultadoRed.Ok or ResultadoRed.CodigoInvalido)
        {
            Guardar(null);
            return ResultadoRed.Ok;
        }

        return resultado;
    }

    private async Task<int> AplicarAsync(IEnumerable<RecordatorioDto> remotos)
    {
        var aplicados = 0;
        var ahora = ZonaLima.Ahora();

        foreach (var dto in remotos)
        {
            var local = await repositorio.ObtenerConEliminadosAsync(dto.Id);

            // Gana la última modificación.
            if (local is not null && dto.ActualizadoEnUtc <= ZonaLima.AUtc(local.ActualizadoEn)) continue;

            await recordatorios.AplicarRemotoAsync(DtoMapper.ARecordatorio(dto), ahora);
            aplicados++;
        }

        return aplicados;
    }

    private void Guardar(string? codigo)
    {
        almacen.Codigo = codigo;
        almacen.CursorLocalUtc = null;
        almacen.CursorServidorUtc = null;
    }
}
