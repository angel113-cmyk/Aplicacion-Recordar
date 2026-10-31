using Suelta.Core.Dominio;
using Suelta.Core.Formato;

namespace Suelta.Core.Avisos;

public enum TipoAviso
{
    /// <summary>A la hora del recordatorio.</summary>
    Principal = 0,

    /// <summary>Unos días antes, para los cumpleaños (HU-22).</summary>
    Anticipado = 1,

    /// <summary>Un nuevo aviso tras tocar "En 1 hora" o "Mañana" (HU-21).</summary>
    Pospuesto = 2,
}

/// <summary>Un aviso por programar. Cuando es la hora de pared de Lima.</summary>
public sealed record AvisoPlan(Guid RecordatorioId, TipoAviso Tipo, DateTime Cuando, string Titulo, string Texto)
{
    public int Id => IdsAviso.Para(RecordatorioId, Tipo);
}

/// <summary>Quien programa de verdad los avisos en el sistema (en Android, las alarmas del teléfono).</summary>
public interface IProgramadorAvisos
{
    void Programar(AvisoPlan aviso);

    /// <summary>Cancela todos los avisos de un recordatorio y quita sus notificaciones visibles.</summary>
    void Cancelar(Guid recordatorioId);
}

/// <summary>Los números de notificación de cada aviso: estables y distintos para cada tipo.</summary>
public static class IdsAviso
{
    public static int Para(Guid recordatorioId, TipoAviso tipo)
    {
        var raiz = BitConverter.ToInt32(recordatorioId.ToByteArray(), 0) & 0x1FFFFFFF;
        return raiz * 4 + (int)tipo;
    }

    public static IEnumerable<int> De(Guid recordatorioId) =>
        Enum.GetValues<TipoAviso>().Select(t => Para(recordatorioId, t));
}

/// <summary>Decide qué avisos le tocan a un recordatorio (HU-20, HU-21, HU-22).</summary>
public static class PlanDeAvisos
{
    /// <summary>Hora a la que avisa un recordatorio que no tiene hora (por confirmar con el Product Owner).</summary>
    public static readonly TimeSpan HoraPorDefecto = TimeSpan.FromHours(9);

    public static IReadOnlyList<AvisoPlan> Para(Recordatorio r, DateTime ahora)
    {
        var avisos = new List<AvisoPlan>();
        if (r.EliminadoEn is not null || r.Hecho) return avisos;

        if (r.FechaHora is { } fechaHora)
        {
            var cuando = r.SinHora ? fechaHora.Date + HoraPorDefecto : fechaHora;

            if (cuando > ahora)
                avisos.Add(new AvisoPlan(r.Id, TipoAviso.Principal, cuando, r.Titulo, "Es ahora."));

            var dias = r.DiasAvisoAnticipado;
            if (dias > 0)
            {
                var anticipado = fechaHora.Date.AddDays(-dias) + HoraPorDefecto;
                if (anticipado > ahora)
                {
                    avisos.Add(new AvisoPlan(
                        r.Id,
                        TipoAviso.Anticipado,
                        anticipado,
                        $"En {dias} días: {EnMinuscula(r.Titulo)}",
                        $"Es el {FormatoFechas.FechaLarga(fechaHora)}."));
                }
            }
        }

        if (r.PospuestoHasta is { } pospuesto && pospuesto > ahora)
            avisos.Add(new AvisoPlan(r.Id, TipoAviso.Pospuesto, pospuesto, r.Titulo, "Te lo vuelvo a recordar."));

        return avisos;
    }

    private static string EnMinuscula(string s) => s.Length == 0 ? s : char.ToLowerInvariant(s[0]) + s[1..];
}
