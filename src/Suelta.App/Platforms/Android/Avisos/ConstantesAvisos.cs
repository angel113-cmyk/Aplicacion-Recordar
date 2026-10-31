namespace Suelta.App.Avisos;

internal static class ConstantesAvisos
{
    public const string CanalId = "avisos";

    public const string ExtraId = "recordatorio_id";
    public const string ExtraTipo = "aviso_tipo";
    public const string ExtraTitulo = "aviso_titulo";
    public const string ExtraTexto = "aviso_texto";
    public const string ExtraNotificacionId = "notificacion_id";

    /// <summary>Al tocar la notificación, la app se abre en ese recordatorio.</summary>
    public const string ExtraAbrirId = "abrir_recordatorio_id";

    public const string AccionHecho = "com.suelta.app.accion.HECHO";
    public const string AccionEnUnaHora = "com.suelta.app.accion.EN_1_HORA";
    public const string AccionManana = "com.suelta.app.accion.MANANA";
}
