using System.Globalization;
using Suelta.Core.Respaldo;

namespace Suelta.App.Servicios;

/// <summary>
/// Lo que el teléfono recuerda del respaldo (HU-26). El código de recuperación va en el almacenamiento
/// seguro de Android (Keystore); los cursores, que no son secretos, en las preferencias.
/// </summary>
public sealed class AlmacenRespaldoApp : IAlmacenRespaldo
{
    private const string ClaveCodigo = "respaldo_codigo";
    private const string ClaveCursorLocal = "respaldo_cursor_local";
    private const string ClaveCursorServidor = "respaldo_cursor_servidor";

    private string? _codigo;

    public AlmacenRespaldoApp()
    {
        _codigo = LeerCodigo();
    }

    public string? Codigo
    {
        get => _codigo;
        set
        {
            _codigo = value;
            GuardarCodigo(value);
        }
    }

    public DateTime? CursorLocalUtc
    {
        get => LeerFecha(ClaveCursorLocal);
        set => GuardarFecha(ClaveCursorLocal, value);
    }

    public DateTime? CursorServidorUtc
    {
        get => LeerFecha(ClaveCursorServidor);
        set => GuardarFecha(ClaveCursorServidor, value);
    }

    // SecureStorage solo tiene métodos asíncronos; se lee una vez al abrir la app y se guarda en memoria.
    private static string? LeerCodigo()
    {
        try
        {
            return Task.Run(async () => await SecureStorage.Default.GetAsync(ClaveCodigo)).GetAwaiter().GetResult();
        }
        catch (Exception)
        {
            // Si el almacenamiento seguro no responde, no se pierde el código: queda en las preferencias.
            var respaldo = Preferences.Get(ClaveCodigo, "");
            return respaldo.Length == 0 ? null : respaldo;
        }
    }

    private static void GuardarCodigo(string? codigo)
    {
        try
        {
            Task.Run(async () =>
            {
                if (codigo is null) SecureStorage.Default.Remove(ClaveCodigo);
                else await SecureStorage.Default.SetAsync(ClaveCodigo, codigo);
            }).GetAwaiter().GetResult();

            Preferences.Remove(ClaveCodigo);
        }
        catch (Exception)
        {
            if (codigo is null) Preferences.Remove(ClaveCodigo);
            else Preferences.Set(ClaveCodigo, codigo);
        }
    }

    private static DateTime? LeerFecha(string clave)
    {
        var texto = Preferences.Get(clave, "");
        return DateTime.TryParse(texto, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var fecha) ? fecha : null;
    }

    private static void GuardarFecha(string clave, DateTime? fecha)
    {
        if (fecha is null) Preferences.Remove(clave);
        else Preferences.Set(clave, fecha.Value.ToString("O", CultureInfo.InvariantCulture));
    }
}
