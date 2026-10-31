namespace Suelta.App.Servicios;

/// <summary>Al tocar una notificación, la app se abre en ese recordatorio (HU-20, HU-22).</summary>
public static class AperturaDesdeAviso
{
    private static Guid? _pendiente;

    /// <summary>La app ya estaba abierta y llegó otra notificación tocada.</summary>
    public static event Action? Solicitada;

    public static void Registrar(string? recordatorioId)
    {
        if (!Guid.TryParse(recordatorioId, out var id)) return;

        _pendiente = id;
        Solicitada?.Invoke();
    }

    /// <summary>El recordatorio que hay que abrir, una sola vez.</summary>
    public static Guid? Consumir()
    {
        var id = _pendiente;
        _pendiente = null;
        return id;
    }
}

/// <summary>Avisa a la pantalla que algo cambió fuera de ella, por ejemplo un botón de la notificación.</summary>
public static class NotificadorCambios
{
    public static event Action? RecordatoriosCambiaron;

    public static void Avisar() => RecordatoriosCambiaron?.Invoke();
}
