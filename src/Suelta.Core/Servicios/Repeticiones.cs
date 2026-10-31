using Suelta.Core.Dominio;

namespace Suelta.Core.Servicios;

/// <summary>Motor de repeticiones: calcula la siguiente fecha de un recordatorio (HT-07).</summary>
public static class Repeticiones
{
    private const int MaximoDePasos = 2000;

    /// <summary>
    /// La próxima vez que toca, que siempre queda después de ahora. Null si el recordatorio
    /// no se repite o no tiene fecha.
    /// </summary>
    public static DateTime? Siguiente(Recordatorio r, DateTime ahora)
    {
        if (r.Repeticion == Repeticion.Ninguna || r.FechaHora is null) return null;

        var ancla = r.AnclaDia ?? r.FechaHora.Value.Day;
        var actual = r.FechaHora.Value;

        for (var i = 0; i < MaximoDePasos; i++)
        {
            actual = Avanzar(actual, r.Repeticion, ancla);
            if (YaEsFutura(actual, r.SinHora, ahora)) return actual;
        }

        return null;
    }

    private static bool YaEsFutura(DateTime fecha, bool sinHora, DateTime ahora) =>
        sinHora ? DateOnly.FromDateTime(fecha) > DateOnly.FromDateTime(ahora) : fecha > ahora;

    private static DateTime Avanzar(DateTime fecha, Repeticion repeticion, int ancla) => repeticion switch
    {
        Repeticion.Diaria => fecha.AddDays(1),
        Repeticion.Semanal => fecha.AddDays(7),
        Repeticion.Mensual => ConDia(fecha.AddDays(-fecha.Day + 1).AddMonths(1), fecha, ancla),
        _ => ConDia(new DateTime(fecha.Year + 1, fecha.Month, 1), fecha, ancla),
    };

    // El primer día del mes destino con el día ancla (o el último del mes si ese mes es más corto).
    private static DateTime ConDia(DateTime primeroDelMes, DateTime original, int ancla)
    {
        var dia = Math.Min(ancla, DateTime.DaysInMonth(primeroDelMes.Year, primeroDelMes.Month));
        return new DateTime(primeroDelMes.Year, primeroDelMes.Month, dia).Add(original.TimeOfDay);
    }
}
