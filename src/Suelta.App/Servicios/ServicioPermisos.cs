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

    private static Task<bool> Concedido<T>() where T : Permissions.BasePermission, new() =>
        MainThread.InvokeOnMainThreadAsync(async () =>
            await Permissions.CheckStatusAsync<T>() == PermissionStatus.Granted);
}
