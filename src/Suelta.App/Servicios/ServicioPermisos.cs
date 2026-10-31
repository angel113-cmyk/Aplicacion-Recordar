namespace Suelta.App.Servicios;

/// <summary>Los dos únicos permisos de Suelta: micrófono y avisos (HU-02).</summary>
public sealed class ServicioPermisos
{
    public Task<bool> MicrofonoConcedidoAsync() => Concedido<Permissions.Microphone>();

    public Task<bool> AvisosConcedidosAsync() => Concedido<Permissions.PostNotifications>();

    /// <summary>Android muestra primero la solicitud del micrófono y luego la de notificaciones.</summary>
    public Task PedirAsync() => MainThread.InvokeOnMainThreadAsync(async () =>
    {
        await Permissions.RequestAsync<Permissions.Microphone>();
        await Permissions.RequestAsync<Permissions.PostNotifications>();
    });

    public void AbrirAjustes() => AppInfo.Current.ShowSettingsUI();

    /// <summary>
    /// Desde Android 12 avisar justo a la hora necesita el permiso de alarmas exactas, y desde
    /// Android 14 viene apagado: hay que activarlo en los ajustes (HU-20).
    /// </summary>
    public bool AlarmasExactasConcedidas()
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(31)) return true;

        var alarmas = (global::Android.App.AlarmManager)global::Android.App.Application.Context
            .GetSystemService(global::Android.Content.Context.AlarmService)!;
        return alarmas.CanScheduleExactAlarms();
    }

    public void PedirAlarmasExactas()
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(31)) return;

        var contexto = global::Android.App.Application.Context;
        var intent = new global::Android.Content.Intent(
            global::Android.Provider.Settings.ActionRequestScheduleExactAlarm,
            global::Android.Net.Uri.Parse($"package:{contexto.PackageName}"));
        intent.AddFlags(global::Android.Content.ActivityFlags.NewTask);
        contexto.StartActivity(intent);
    }

    private static Task<bool> Concedido<T>() where T : Permissions.BasePermission, new() =>
        MainThread.InvokeOnMainThreadAsync(async () =>
            await Permissions.CheckStatusAsync<T>() == PermissionStatus.Granted);
}
