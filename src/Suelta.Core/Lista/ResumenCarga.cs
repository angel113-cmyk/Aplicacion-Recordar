using Suelta.Core.Dominio;

namespace Suelta.Core.Lista;

/// <summary>Las tres cantidades que se ven arriba de la lista (HU-16).</summary>
public sealed record ResumenCarga(int Hoy, int ProximosSieteDias, int SeRepitenSolos)
{
    public static ResumenCarga De(IEnumerable<Recordatorio> recordatorios, DateTime ahora)
    {
        var hoy = DateOnly.FromDateTime(ahora);
        var pendientes = recordatorios.Where(r => r.EliminadoEn is null && !r.Hecho).ToList();

        var conFecha = pendientes
            .Where(r => r.FechaHora is not null)
            .Select(r => DateOnly.FromDateTime(r.FechaHora!.Value))
            .ToList();

        return new ResumenCarga(
            conFecha.Count(f => f == hoy),
            conFecha.Count(f => f >= hoy && f <= hoy.AddDays(7)),
            pendientes.Count(r => r.Repeticion != Repeticion.Ninguna));
    }

    public bool EstaVacio => Hoy == 0 && ProximosSieteDias == 0 && SeRepitenSolos == 0;
}
