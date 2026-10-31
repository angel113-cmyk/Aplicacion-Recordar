using Suelta.Core.Dominio;

namespace Suelta.Core.Formato;

/// <summary>Textos de fecha y hora como se ven en las tarjetas ("jue 5 nov · 3 pm").</summary>
public static class FormatoFechas
{
    private static readonly string[] DiasCortos = ["dom", "lun", "mar", "mié", "jue", "vie", "sáb"];
    private static readonly string[] DiasLargos = ["Domingo", "Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado"];
    private static readonly string[] MesesCortos = ["ene", "feb", "mar", "abr", "may", "jun", "jul", "ago", "sep", "oct", "nov", "dic"];
    private static readonly string[] MesesLargos =
        ["enero", "febrero", "marzo", "abril", "mayo", "junio", "julio", "agosto", "septiembre", "octubre", "noviembre", "diciembre"];

    public static string Hora(DateTime fh)
    {
        var h12 = fh.Hour % 12 == 0 ? 12 : fh.Hour % 12;
        var sufijo = fh.Hour < 12 ? "am" : "pm";
        return fh.Minute == 0 ? $"{h12} {sufijo}" : $"{h12}:{fh.Minute:00} {sufijo}";
    }

    /// <summary>"Hoy · 6 pm", "Mañana", "jue 5 nov · 3 pm".</summary>
    public static string Cuando(Recordatorio r, DateTime ahora)
    {
        if (r.FechaHora is null) return "Sin fecha";

        var fh = r.FechaHora.Value;
        var dias = DateOnly.FromDateTime(fh).DayNumber - DateOnly.FromDateTime(ahora).DayNumber;

        var dia = dias switch
        {
            0 => "Hoy",
            1 => "Mañana",
            _ => $"{DiasCortos[(int)fh.DayOfWeek]} {fh.Day} {MesesCortos[fh.Month - 1]}",
        };

        return r.SinHora ? dia : $"{dia} · {Hora(fh)}";
    }

    /// <summary>"Cada día", "Cada semana", "Cada mes, día 30", "Cada año"; vacío si no se repite.</summary>
    public static string EtiquetaRepeticion(Recordatorio r) => r.Repeticion switch
    {
        Repeticion.Diaria => "Cada día",
        Repeticion.Semanal => "Cada semana",
        Repeticion.Mensual when r.FechaHora is not null => $"Cada mes, día {r.FechaHora.Value.Day}",
        Repeticion.Mensual => "Cada mes",
        Repeticion.Anual => "Cada año",
        _ => "",
    };

    /// <summary>"Jueves 29 de octubre".</summary>
    public static string Cabecera(DateTime ahora) =>
        $"{DiasLargos[(int)ahora.DayOfWeek]} {ahora.Day} de {MesesLargos[ahora.Month - 1]}";

    /// <summary>"jueves 5 de noviembre".</summary>
    public static string FechaLarga(DateTime fecha) =>
        $"{DiasLargos[(int)fecha.DayOfWeek].ToLowerInvariant()} {fecha.Day} de {MesesLargos[fecha.Month - 1]}";

    /// <summary>"Te aviso 3 días antes"; vacío si el recordatorio no avisa antes.</summary>
    public static string AvisoAnticipado(Recordatorio r) =>
        r.DiasAvisoAnticipado > 0 ? $"Te aviso {r.DiasAvisoAnticipado} días antes" : "";
}
