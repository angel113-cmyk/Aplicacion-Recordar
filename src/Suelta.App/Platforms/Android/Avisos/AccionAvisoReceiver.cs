using Android.App;
using Android.Content;
using Suelta.App.Servicios;
using Suelta.Core.Servicios;

namespace Suelta.App.Avisos;

/// <summary>
/// Los botones de la notificación: Hecho, En 1 hora y Mañana (HU-21). Funcionan sin abrir la app,
/// incluso si está cerrada.
/// </summary>
[BroadcastReceiver(Enabled = true, Exported = false)]
public sealed class AccionAvisoReceiver : BroadcastReceiver
{
    public override void OnReceive(Context? context, Intent? intent)
    {
        if (context is null || intent is null) return;

        var accion = intent.Action;
        var notificacionId = intent.GetIntExtra(ConstantesAvisos.ExtraNotificacionId, -1);
        var hayId = Guid.TryParse(intent.GetStringExtra(ConstantesAvisos.ExtraId), out var id);

        // Se sigue ejecutando un momento más aunque este método ya haya terminado.
        var pendiente = GoAsync();

        _ = Task.Run(async () =>
        {
            try
            {
                if (hayId)
                    await ResolverAsync(context, id, accion);

                if (notificacionId >= 0)
                    NotificacionesAndroid.Quitar(context, notificacionId);

                // Si la app está abierta, que refleje el cambio.
                NotificadorCambios.Avisar();
            }
            catch (Exception e)
            {
                FabricaServicios.Registro(context).Error("AccionAvisoReceiver", e);
            }
            finally
            {
                pendiente?.Finish();
            }
        });
    }

    private static async Task ResolverAsync(Context context, Guid id, string? accion)
    {
        var servicio = FabricaServicios.Crear(context);
        var recordatorio = await servicio.ObtenerAsync(id);
        if (recordatorio is null) return;

        var ahora = ZonaLima.Ahora();
        switch (accion)
        {
            case ConstantesAvisos.AccionHecho:
                await servicio.MarcarHechoAsync(recordatorio, ahora);
                break;
            case ConstantesAvisos.AccionEnUnaHora:
                await servicio.PosponerUnaHoraAsync(recordatorio, ahora);
                break;
            case ConstantesAvisos.AccionManana:
                await servicio.PosponerHastaMananaAsync(recordatorio, ahora);
                break;
        }
    }
}
