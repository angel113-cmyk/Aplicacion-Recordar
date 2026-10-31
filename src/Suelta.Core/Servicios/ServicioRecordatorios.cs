using Suelta.Core.Dominio;
using Suelta.Core.Parser;

namespace Suelta.Core.Servicios;

/// <summary>Donde se guardan los recordatorios (en el teléfono: SQLite, HT-05).</summary>
public interface IRecordatorioRepositorio
{
    Task InsertarAsync(Recordatorio recordatorio);

    /// <summary>Todos los recordatorios que no están eliminados.</summary>
    Task<List<Recordatorio>> ListarAsync();
}

public sealed class ServicioRecordatorios(IRecordatorioRepositorio repositorio)
{
    /// <summary>Guarda lo que el parser entendió. Si todavía falta un dato, no guarda nada.</summary>
    public async Task<Recordatorio> GuardarAsync(ResultadoParser resultado, DateTime ahora)
    {
        if (!resultado.Completo)
            throw new InvalidOperationException("Todavía falta un dato; primero hay que responder la pregunta.");

        var recordatorio = new Recordatorio
        {
            Id = Guid.NewGuid(),
            Titulo = resultado.Titulo,
            Categoria = resultado.Categoria,
            FechaHora = resultado.FechaHora,
            SinHora = resultado.SinHora,
            Repeticion = resultado.Repeticion,
            Monto = resultado.Monto,
            Dijiste = resultado.Dijiste,
            CreadoEn = ahora,
            ActualizadoEn = ahora,
        };

        await repositorio.InsertarAsync(recordatorio);
        return recordatorio;
    }

    public Task<List<Recordatorio>> ListarAsync() => repositorio.ListarAsync();
}
