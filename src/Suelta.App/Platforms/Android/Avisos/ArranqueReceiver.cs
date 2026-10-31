using Android.App;
using Android.Content;
using Suelta.Core.Servicios;

namespace Suelta.App.Avisos;

/// <summary>
/// Al encender el teléfono (o actualizar la app) Android borra las alarmas: aquí se vuelven a
/// programar todas, sin abrir la app (HU-23).
/// </summary>
[BroadcastReceiver(Enabled = true, Exported = true)]
[IntentFilter([Intent.ActionBootCompleted, Intent.ActionMyPackageReplaced])]
public sealed class ArranqueReceiver : BroadcastReceiver
{
    public override void OnReceive(Context? context, Intent? intent)
    {
        if (context is null || intent is null) return;
        if (intent.Action is not (Intent.ActionBootCompleted or Intent.ActionMyPackageReplaced)) return;

        var pendiente = GoAsync();

        _ = Task.Run(async () =>
        {
            try
            {
                await FabricaServicios.Crear(context).ReprogramarTodosAsync(ZonaLima.Ahora());
            }
            catch (Exception e)
            {
                FabricaServicios.Registro(context).Error("ArranqueReceiver", e);
            }
            finally
            {
                pendiente?.Finish();
            }
        });
    }
}
