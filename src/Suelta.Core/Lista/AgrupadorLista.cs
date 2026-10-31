using Suelta.Core.Dominio;

namespace Suelta.Core.Lista;

public enum GrupoFecha
{
    SeTePaso,
    Hoy,
    Manana,
    EstaSemana,
    MasAdelante,
    SinFecha,
    Hechos,
}

public sealed record GrupoRecordatorios(GrupoFecha Grupo, string Titulo, IReadOnlyList<Recordatorio> Items);

/// <summary>Ordena los recordatorios por tiempo en los grupos de la vista Próximos (HU-15).</summary>
public static class AgrupadorLista
{
    public static string Titulo(GrupoFecha grupo) => grupo switch
    {
        GrupoFecha.SeTePaso => "Se te pasó",
        GrupoFecha.Hoy => "Hoy",
        GrupoFecha.Manana => "Mañana",
        GrupoFecha.EstaSemana => "Esta semana",
        GrupoFecha.MasAdelante => "Más adelante",
        GrupoFecha.SinFecha => "Sin fecha",
        _ => "Hechos",
    };

    public static GrupoFecha Clasificar(Recordatorio r, DateTime ahora)
    {
        if (r.Hecho) return GrupoFecha.Hechos;
        if (r.FechaHora is null) return GrupoFecha.SinFecha;

        var fh = r.FechaHora.Value;
        var diaR = DateOnly.FromDateTime(fh);
        var hoy = DateOnly.FromDateTime(ahora);

        // Sin hora, el recordatorio vale todo el día: no se pasa hasta que termina.
        if (r.SinHora ? diaR < hoy : fh < ahora) return GrupoFecha.SeTePaso;

        var dias = diaR.DayNumber - hoy.DayNumber;
        return dias switch
        {
            0 => GrupoFecha.Hoy,
            1 => GrupoFecha.Manana,
            <= 7 => GrupoFecha.EstaSemana,
            _ => GrupoFecha.MasAdelante,
        };
    }

    /// <summary>Devuelve solo los grupos que tienen recordatorios, cada uno ordenado por fecha y hora.</summary>
    public static IReadOnlyList<GrupoRecordatorios> Agrupar(IEnumerable<Recordatorio> recordatorios, DateTime ahora)
    {
        return recordatorios
            .Where(r => r.EliminadoEn is null)
            .GroupBy(r => Clasificar(r, ahora))
            .OrderBy(g => g.Key)
            .Select(g => new GrupoRecordatorios(
                g.Key,
                Titulo(g.Key),
                g.OrderBy(r => r.FechaHora ?? DateTime.MaxValue).ThenBy(r => r.CreadoEn).ToList()))
            .ToList();
    }
}
