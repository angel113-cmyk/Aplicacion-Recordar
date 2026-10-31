using Suelta.Core.Dominio;

namespace Suelta.Core.Lista;

public sealed record ConteoCategoria(CategoriaInfo Info, int Cantidad);

/// <summary>La vista "Por categoría" (HU-17). Solo cuenta lo que está pendiente.</summary>
public static class AgrupadorCategorias
{
    /// <summary>Solo las categorías que tienen recordatorios, las que tienen más primero.</summary>
    public static IReadOnlyList<ConteoCategoria> Contar(IEnumerable<Recordatorio> recordatorios) =>
        Pendientes(recordatorios)
            .GroupBy(r => r.Categoria)
            .Select(g => new ConteoCategoria(CategoriaInfo.De(g.Key), g.Count()))
            .OrderByDescending(c => c.Cantidad)
            .ThenBy(c => c.Info.Categoria)
            .ToList();

    /// <summary>Los recordatorios de una categoría (o de todas si es null), ordenados por fecha y hora.</summary>
    public static IReadOnlyList<Recordatorio> Filtrar(IEnumerable<Recordatorio> recordatorios, Categoria? categoria) =>
        Pendientes(recordatorios)
            .Where(r => categoria is null || r.Categoria == categoria)
            .OrderBy(r => r.FechaHora ?? DateTime.MaxValue)
            .ThenBy(r => r.CreadoEn)
            .ToList();

    private static IEnumerable<Recordatorio> Pendientes(IEnumerable<Recordatorio> recordatorios) =>
        recordatorios.Where(r => r.EliminadoEn is null && !r.Hecho);
}
