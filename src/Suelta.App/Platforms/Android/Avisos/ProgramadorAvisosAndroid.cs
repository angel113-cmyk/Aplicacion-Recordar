using Android.App;
using Android.Content;
using AndroidX.Core.App;
using Suelta.Core.Avisos;
using Suelta.Core.Servicios;

namespace Suelta.App.Avisos;

/// <summary>Programa los avisos con las alarmas del teléfono (HT-06).</summary>
public sealed class ProgramadorAvisosAndroid(Context contexto) : IProgramadorAvisos
{
    private const PendingIntentFlags Banderas = PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable;

    public void Programar(AvisoPlan aviso)
    {
        var alarmas = Alarmas();

        var intent = new Intent(contexto, typeof(AvisoReceiver));
        intent.PutExtra(ConstantesAvisos.ExtraId, aviso.RecordatorioId.ToString());
        intent.PutExtra(ConstantesAvisos.ExtraTipo, (int)aviso.Tipo);
        intent.PutExtra(ConstantesAvisos.ExtraTitulo, aviso.Titulo);
        intent.PutExtra(ConstantesAvisos.ExtraTexto, aviso.Texto);

        var pendiente = PendingIntent.GetBroadcast(contexto, aviso.Id, intent, Banderas);
        var cuando = ZonaLima.AMilisegundosUnix(aviso.Cuando);

        // Sin el permiso de alarmas exactas, Android avisa "alrededor" de la hora en lugar de justo a la hora.
        if (PuedeProgramarExactas(alarmas))
            alarmas.SetExactAndAllowWhileIdle(AlarmType.RtcWakeup, cuando, pendiente!);
        else
            alarmas.SetAndAllowWhileIdle(AlarmType.RtcWakeup, cuando, pendiente!);
    }

    public void Cancelar(Guid recordatorioId)
    {
        var alarmas = Alarmas();
        var notificaciones = NotificationManagerCompat.From(contexto)!;

        foreach (var tipo in Enum.GetValues<TipoAviso>())
        {
            var id = IdsAviso.Para(recordatorioId, tipo);

            var intent = new Intent(contexto, typeof(AvisoReceiver));
            var pendiente = PendingIntent.GetBroadcast(contexto, id, intent, PendingIntentFlags.NoCreate | PendingIntentFlags.Immutable);
            if (pendiente is not null)
            {
                alarmas.Cancel(pendiente);
                pendiente.Cancel();
            }

            notificaciones.Cancel(id);
        }
    }

    public static bool PuedeProgramarExactas(AlarmManager alarmas) =>
        !OperatingSystem.IsAndroidVersionAtLeast(31) || alarmas.CanScheduleExactAlarms();

    private AlarmManager Alarmas() => (AlarmManager)contexto.GetSystemService(Context.AlarmService)!;
}
