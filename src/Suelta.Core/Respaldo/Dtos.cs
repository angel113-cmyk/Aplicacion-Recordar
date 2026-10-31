using System.Text.Json;
using System.Text.Json.Serialization;
using Suelta.Core.Dominio;
using Suelta.Core.Servicios;

namespace Suelta.Core.Respaldo;

/// <summary>
/// Un recordatorio tal como viaja al servidor. Las fechas de actualización van en UTC; la fecha y la
/// hora del recordatorio siguen siendo la hora de pared de Lima, porque así suena la alarma.
/// </summary>
public sealed record RecordatorioDto(
    Guid Id,
    string Titulo,
    int Categoria,
    DateTime? FechaHora,
    bool SinHora,
    int Repeticion,
    int? AnclaDia,
    decimal? Monto,
    string Dijiste,
    bool Hecho,
    DateTime? PospuestoHasta,
    DateTime CreadoEnUtc,
    DateTime ActualizadoEnUtc,
    DateTime? EliminadoEnUtc);

/// <param name="DesdeUtc">El cursor que devolvió el servidor la vez anterior (null la primera vez).</param>
/// <param name="Cambios">Lo que cambió en este teléfono desde la última sincronización.</param>
public sealed record SincronizarSolicitud(DateTime? DesdeUtc, List<RecordatorioDto> Cambios);

/// <param name="CursorUtc">Desde donde pedir novedades la próxima vez.</param>
/// <param name="Cambios">Lo que cambió en el servidor (por otros teléfonos) y este teléfono no tiene.</param>
public sealed record SincronizarRespuesta(DateTime CursorUtc, List<RecordatorioDto> Cambios);

public sealed record CuentaCreada(string Codigo);

public static class DtoMapper
{
    public static RecordatorioDto ADto(Recordatorio r) => new(
        r.Id,
        r.Titulo,
        (int)r.Categoria,
        r.FechaHora,
        r.SinHora,
        (int)r.Repeticion,
        r.AnclaDia,
        r.Monto,
        r.Dijiste,
        r.Hecho,
        r.PospuestoHasta,
        ZonaLima.AUtc(r.CreadoEn),
        ZonaLima.AUtc(r.ActualizadoEn),
        r.EliminadoEn is null ? null : ZonaLima.AUtc(r.EliminadoEn.Value));

    public static Recordatorio ARecordatorio(RecordatorioDto d) => new()
    {
        Id = d.Id,
        Titulo = d.Titulo,
        Categoria = Enum.IsDefined((Categoria)d.Categoria) ? (Categoria)d.Categoria : Categoria.Otro,
        FechaHora = d.FechaHora,
        SinHora = d.SinHora,
        Repeticion = Enum.IsDefined((Repeticion)d.Repeticion) ? (Repeticion)d.Repeticion : Repeticion.Ninguna,
        AnclaDia = d.AnclaDia,
        Monto = d.Monto,
        Dijiste = d.Dijiste,
        Hecho = d.Hecho,
        PospuestoHasta = d.PospuestoHasta,
        CreadoEn = ZonaLima.DeUtc(d.CreadoEnUtc),
        ActualizadoEn = ZonaLima.DeUtc(d.ActualizadoEnUtc),
        EliminadoEn = d.EliminadoEnUtc is null ? null : ZonaLima.DeUtc(d.EliminadoEnUtc.Value),
    };
}

/// <summary>Serialización generada en compilación: no depende de reflexión, así que el recorte de la app no la rompe.</summary>
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(RecordatorioDto))]
[JsonSerializable(typeof(SincronizarSolicitud))]
[JsonSerializable(typeof(SincronizarRespuesta))]
[JsonSerializable(typeof(CuentaCreada))]
public partial class JsonContexto : JsonSerializerContext;
