namespace Suelta.Core.Servicios;

/// <summary>La hora de pared de Lima (America/Lima, UTC-5 todo el año).</summary>
public static class ZonaLima
{
    private static readonly TimeZoneInfo Zona = Buscar();

    public static DateTime Ahora() => TimeZoneInfo.ConvertTime(DateTime.UtcNow, Zona);

    /// <summary>De hora de pared de Lima a UTC.</summary>
    public static DateTime AUtc(DateTime horaLima) =>
        TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(horaLima, DateTimeKind.Unspecified), Zona);

    /// <summary>De UTC a hora de pared de Lima.</summary>
    public static DateTime DeUtc(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Zona);

    /// <summary>Milisegundos Unix (los que piden las alarmas) de una hora de pared de Lima.</summary>
    public static long AMilisegundosUnix(DateTime horaLima)
    {
        var utc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(horaLima, DateTimeKind.Unspecified), Zona);
        return new DateTimeOffset(utc, TimeSpan.Zero).ToUnixTimeMilliseconds();
    }

    private static TimeZoneInfo Buscar()
    {
        foreach (var id in new[] { "America/Lima", "SA Pacific Standard Time" })
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
            catch (TimeZoneNotFoundException) { }
            catch (InvalidTimeZoneException) { }
        }

        return TimeZoneInfo.CreateCustomTimeZone("Lima", TimeSpan.FromHours(-5), "Lima", "Lima");
    }
}
