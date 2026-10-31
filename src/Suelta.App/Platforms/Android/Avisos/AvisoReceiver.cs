using Android.App;
using Android.Content;
using Suelta.Core.Avisos;

namespace Suelta.App.Avisos;

/// <summary>Llega la hora de un aviso: se muestra la notificación.</summary>
[BroadcastReceiver(Enabled = true, Exported = false)]
public sealed class AvisoReceiver : BroadcastReceiver
{
    public override void OnReceive(Context? context, Intent? intent)
    {
        if (context is null || intent is null) return;

        try
        {
            if (!Guid.TryParse(intent.GetStringExtra(ConstantesAvisos.ExtraId), out var id)) return;

            var tipo = (TipoAviso)intent.GetIntExtra(ConstantesAvisos.ExtraTipo, (int)TipoAviso.Principal);
            var titulo = intent.GetStringExtra(ConstantesAvisos.ExtraTitulo) ?? "Suelta";
            var texto = intent.GetStringExtra(ConstantesAvisos.ExtraTexto) ?? "";

            NotificacionesAndroid.Mostrar(context, id, tipo, titulo, texto);
        }
        catch (Exception e)
        {
            FabricaServicios.Registro(context).Error("AvisoReceiver", e);
        }
    }
}
