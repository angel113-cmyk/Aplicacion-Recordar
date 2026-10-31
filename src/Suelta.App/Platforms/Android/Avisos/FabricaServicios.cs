using Android.Content;
using Suelta.Core.Datos;
using Suelta.Core.Servicios;

namespace Suelta.App.Avisos;

/// <summary>
/// Arma los servicios para los receptores del sistema (alarmas, botones de la notificación y
/// reinicio). Pueden ejecutarse con la app cerrada, así que no dependen de la interfaz.
/// </summary>
internal static class FabricaServicios
{
    // La misma carpeta que FileSystem.AppDataDirectory, para usar la misma base de datos que la app.
    public static string Carpeta(Context contexto) => contexto.FilesDir!.AbsolutePath;

    public static string RutaBaseDeDatos(Context contexto) => Path.Combine(Carpeta(contexto), "suelta.db3");

    public static RegistroErrores Registro(Context contexto) => new(Path.Combine(Carpeta(contexto), "errores.log"));

    public static ServicioRecordatorios Crear(Context contexto) =>
        new(new RecordatorioRepositorio(RutaBaseDeDatos(contexto)), new ProgramadorAvisosAndroid(contexto));
}
