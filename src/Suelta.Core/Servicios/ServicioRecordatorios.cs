using Suelta.Core.Dominio;
using Suelta.Core.Parser;

namespace Suelta.Core.Servicios;

/// <summary>Donde se guardan los recordatorios (en el teléfono: SQLite, HT-05).</summary>
public interface IRecordatorioRepositorio
{
    Task InsertarAsync(Recordatorio recordatorio);

    Task ActualizarAsync(Recordatorio recordatorio);

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
            AnclaDia = resultado.FechaHora?.Day,
            Monto = resultado.Monto,
            Dijiste = resultado.Dijiste,
            CreadoEn = ahora,
            ActualizadoEn = ahora,
        };

        await repositorio.InsertarAsync(recordatorio);
        return recordatorio;
    }

    /// <summary>Guarda todos los recordatorios de una frase larga (HU-08). Solo si ya no falta nada.</summary>
    public async Task<IReadOnlyList<Recordatorio>> GuardarVariosAsync(IEnumerable<ResultadoParser> resultados, DateTime ahora)
    {
        var lista = resultados.ToList();
        if (lista.Any(r => !r.Completo))
            throw new InvalidOperationException("Todavía falta un dato; primero hay que responder la pregunta.");

        var guardados = new List<Recordatorio>();
        foreach (var resultado in lista)
            guardados.Add(await GuardarAsync(resultado, ahora));
        return guardados;
    }

    public Task<List<Recordatorio>> ListarAsync() => repositorio.ListarAsync();

    /// <summary>
    /// Marca como hecho (HU-19). Si se repite, no se marca: pasa a su siguiente fecha y sigue en la
    /// lista; solo cuenta como hecha la fecha actual.
    /// </summary>
    public async Task MarcarHechoAsync(Recordatorio r, DateTime ahora)
    {
        var siguiente = r.Hecho ? null : Repeticiones.Siguiente(r, ahora);
        if (siguiente is not null)
            r.FechaHora = siguiente;
        else
            r.Hecho = true;

        r.ActualizadoEn = ahora;
        await repositorio.ActualizarAsync(r);
    }

    /// <summary>Un recordatorio que estaba en "Hechos" vuelve a la lista de próximos.</summary>
    public async Task DesmarcarHechoAsync(Recordatorio r, DateTime ahora)
    {
        r.Hecho = false;
        r.ActualizadoEn = ahora;
        await repositorio.ActualizarAsync(r);
    }

    /// <summary>Guarda los cambios de una edición (HU-25). El título no puede quedar vacío.</summary>
    public async Task ActualizarAsync(Recordatorio r, DateTime ahora)
    {
        if (string.IsNullOrWhiteSpace(r.Titulo))
            throw new ArgumentException("El título no puede quedar vacío.", nameof(r));

        r.Titulo = r.Titulo.Trim();
        r.AnclaDia = r.FechaHora?.Day;
        r.ActualizadoEn = ahora;
        await repositorio.ActualizarAsync(r);
    }

    /// <summary>Borrado lógico: sirve para eliminar y también para deshacer justo después de guardar (HU-24).</summary>
    public async Task EliminarAsync(IEnumerable<Recordatorio> recordatorios, DateTime ahora)
    {
        foreach (var r in recordatorios)
        {
            r.EliminadoEn = ahora;
            r.ActualizadoEn = ahora;
            await repositorio.ActualizarAsync(r);
        }
    }
}
