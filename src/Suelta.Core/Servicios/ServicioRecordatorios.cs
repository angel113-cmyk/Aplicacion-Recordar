using Suelta.Core.Avisos;
using Suelta.Core.Dominio;
using Suelta.Core.Parser;

namespace Suelta.Core.Servicios;

/// <summary>Donde se guardan los recordatorios (en el teléfono: SQLite, HT-05).</summary>
public interface IRecordatorioRepositorio
{
    Task InsertarAsync(Recordatorio recordatorio);

    Task ActualizarAsync(Recordatorio recordatorio);

    /// <summary>Un recordatorio por su id, o null si no existe o está eliminado.</summary>
    Task<Recordatorio?> ObtenerAsync(Guid id);

    /// <summary>Todos los recordatorios que no están eliminados.</summary>
    Task<List<Recordatorio>> ListarAsync();
}

public sealed class ServicioRecordatorios(IRecordatorioRepositorio repositorio, IProgramadorAvisos? avisos = null)
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
        Reprogramar(recordatorio, ahora);
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

    public Task<Recordatorio?> ObtenerAsync(Guid id) => repositorio.ObtenerAsync(id);

    /// <summary>
    /// Marca como hecho (HU-19). Si se repite, no se marca: pasa a su siguiente fecha y sigue en la
    /// lista; solo cuenta como hecha la fecha actual. En ambos casos se cancela el aviso de hoy.
    /// </summary>
    public async Task MarcarHechoAsync(Recordatorio r, DateTime ahora)
    {
        var siguiente = r.Hecho ? null : Repeticiones.Siguiente(r, ahora);
        if (siguiente is not null)
            r.FechaHora = siguiente;
        else
            r.Hecho = true;

        r.PospuestoHasta = null;
        r.ActualizadoEn = ahora;
        await repositorio.ActualizarAsync(r);
        Reprogramar(r, ahora);
    }

    /// <summary>Un recordatorio que estaba en "Hechos" vuelve a la lista de próximos.</summary>
    public async Task DesmarcarHechoAsync(Recordatorio r, DateTime ahora)
    {
        r.Hecho = false;
        r.ActualizadoEn = ahora;
        await repositorio.ActualizarAsync(r);
        Reprogramar(r, ahora);
    }

    /// <summary>"En 1 hora": un nuevo aviso una hora después. La fecha del recordatorio no cambia (HU-21).</summary>
    public Task PosponerUnaHoraAsync(Recordatorio r, DateTime ahora) => PosponerAsync(r, ahora.AddHours(1), ahora);

    /// <summary>"Mañana": un nuevo aviso mañana a la misma hora (HU-21).</summary>
    public Task PosponerHastaMananaAsync(Recordatorio r, DateTime ahora) => PosponerAsync(r, ahora.AddDays(1), ahora);

    private async Task PosponerAsync(Recordatorio r, DateTime hasta, DateTime ahora)
    {
        r.PospuestoHasta = hasta;
        r.ActualizadoEn = ahora;
        await repositorio.ActualizarAsync(r);
        Reprogramar(r, ahora);
    }

    /// <summary>Guarda los cambios de una edición (HU-25). El título no puede quedar vacío.</summary>
    public async Task ActualizarAsync(Recordatorio r, DateTime ahora)
    {
        if (string.IsNullOrWhiteSpace(r.Titulo))
            throw new ArgumentException("El título no puede quedar vacío.", nameof(r));

        r.Titulo = r.Titulo.Trim();
        r.AnclaDia = r.FechaHora?.Day;
        r.PospuestoHasta = null;
        r.ActualizadoEn = ahora;
        await repositorio.ActualizarAsync(r);
        Reprogramar(r, ahora);
    }

    /// <summary>Borrado lógico: sirve para eliminar y también para deshacer justo después de guardar (HU-24).</summary>
    public async Task EliminarAsync(IEnumerable<Recordatorio> recordatorios, DateTime ahora)
    {
        foreach (var r in recordatorios)
        {
            r.EliminadoEn = ahora;
            r.ActualizadoEn = ahora;
            await repositorio.ActualizarAsync(r);
            Reprogramar(r, ahora);
        }
    }

    /// <summary>
    /// Vuelve a programar los avisos de todo lo pendiente. Se usa al encender el teléfono y al abrir
    /// la app (HU-23). Lo que ya pasó mientras tanto no avisa tarde: queda en "Se te pasó".
    /// </summary>
    public async Task ReprogramarTodosAsync(DateTime ahora)
    {
        foreach (var r in await repositorio.ListarAsync())
            Reprogramar(r, ahora);
    }

    private void Reprogramar(Recordatorio r, DateTime ahora)
    {
        if (avisos is null) return;

        avisos.Cancelar(r.Id);
        foreach (var aviso in PlanDeAvisos.Para(r, ahora))
            avisos.Programar(aviso);
    }
}
