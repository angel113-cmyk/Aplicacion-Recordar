namespace Suelta.Core.Servicios;

/// <summary>
/// Registro local de errores y fallos (HT-17). Guarda qué falló y dónde, pero nunca el mensaje de la
/// excepción ni el texto que dijo la persona: esas frases son privadas.
/// </summary>
public sealed class RegistroErrores(string rutaArchivo)
{
    private const long TamanoMaximo = 256 * 1024;
    private static readonly object Candado = new();

    public void Error(string donde, Exception excepcion)
    {
        try
        {
            lock (Candado)
            {
                Recortar();
                File.AppendAllText(rutaArchivo, Formatear(donde, excepcion));
            }
        }
        catch (Exception)
        {
            // El registro nunca debe causar otro fallo.
        }
    }

    public string Leer() => File.Exists(rutaArchivo) ? File.ReadAllText(rutaArchivo) : "";

    // Tipo de excepción y pila de llamadas (nombres de métodos del código). Sin Message ni Data.
    private static string Formatear(string donde, Exception excepcion)
    {
        var pila = string.Join(" <- ", excepcion.StackTrace?
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(linea => linea.Trim())
            .Select(QuitarRuta)
            .Take(8) ?? []);

        return $"{DateTime.UtcNow:O} [{donde}] {excepcion.GetType().FullName} | {pila}{Environment.NewLine}";
    }

    // "at Clase.Metodo(String x) in C:\Users\...\Archivo.cs:line 5" -> "Clase.Metodo"
    private static string QuitarRuta(string linea)
    {
        var sinAt = linea.StartsWith("at ", StringComparison.Ordinal) ? linea[3..] : linea;
        var parentesis = sinAt.IndexOf('(');
        return parentesis > 0 ? sinAt[..parentesis] : sinAt;
    }

    private void Recortar()
    {
        var info = new FileInfo(rutaArchivo);
        if (info.Exists && info.Length > TamanoMaximo)
            File.Delete(rutaArchivo);
    }
}
