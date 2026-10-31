using Android.App;
using Android.Content;
using AndroidX.Core.App;
using Suelta.Core.Avisos;

namespace Suelta.App.Avisos;

/// <summary>Arma y muestra la notificación de un aviso, con los botones Hecho, En 1 hora y Mañana (HU-21).</summary>
internal static class NotificacionesAndroid
{
    private const PendingIntentFlags Banderas = PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable;

    public static void Mostrar(Context contexto, Guid recordatorioId, TipoAviso tipo, string titulo, string texto)
    {
        CrearCanal(contexto);
        var notificacionId = IdsAviso.Para(recordatorioId, tipo);

        // Al tocar la notificación se abre Suelta en ese recordatorio (HU-20, HU-22).
        var abrir = new Intent(contexto, typeof(MainActivity));
        abrir.SetFlags(ActivityFlags.NewTask | ActivityFlags.ClearTop | ActivityFlags.SingleTop);
        abrir.PutExtra(ConstantesAvisos.ExtraAbrirId, recordatorioId.ToString());
        var abrirPendiente = PendingIntent.GetActivity(contexto, notificacionId, abrir, Banderas);

        var builder = new NotificationCompat.Builder(contexto, ConstantesAvisos.CanalId);
        builder.SetSmallIcon(Resource.Drawable.ic_notificacion);
        builder.SetContentTitle(titulo);
        builder.SetContentText(texto);
        builder.SetStyle(new NotificationCompat.BigTextStyle().BigText(texto));
        builder.SetPriority(NotificationCompat.PriorityHigh);
        builder.SetCategory(NotificationCompat.CategoryReminder);
        builder.SetAutoCancel(true);
        builder.SetContentIntent(abrirPendiente);

        // Los tres botones aparecen en toda notificación de Suelta.
        builder.AddAction(0, "Hecho", Accion(contexto, recordatorioId, notificacionId, ConstantesAvisos.AccionHecho));
        builder.AddAction(0, "En 1 hora", Accion(contexto, recordatorioId, notificacionId, ConstantesAvisos.AccionEnUnaHora));
        builder.AddAction(0, "Mañana", Accion(contexto, recordatorioId, notificacionId, ConstantesAvisos.AccionManana));

        // Sin el permiso de avisos Android descarta la notificación sin error.
        NotificationManagerCompat.From(contexto)!.Notify(notificacionId, builder.Build()!);
    }

    public static void Quitar(Context contexto, int notificacionId) =>
        NotificationManagerCompat.From(contexto)!.Cancel(notificacionId);

    private static PendingIntent? Accion(Context contexto, Guid recordatorioId, int notificacionId, string accion)
    {
        var intent = new Intent(contexto, typeof(AccionAvisoReceiver));
        intent.SetAction(accion);
        intent.PutExtra(ConstantesAvisos.ExtraId, recordatorioId.ToString());
        intent.PutExtra(ConstantesAvisos.ExtraNotificacionId, notificacionId);

        return PendingIntent.GetBroadcast(contexto, HashCode.Combine(notificacionId, accion), intent, Banderas);
    }

    private static void CrearCanal(Context contexto)
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(26)) return;

        var canal = new NotificationChannel(ConstantesAvisos.CanalId, "Avisos", NotificationImportance.High)
        {
            Description = "Los avisos de tus recordatorios",
        };

        var manager = (NotificationManager)contexto.GetSystemService(Context.NotificationService)!;
        manager.CreateNotificationChannel(canal);
    }
}
