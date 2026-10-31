using System.Security.Cryptography;
using System.Text;

namespace Suelta.Core.Respaldo;

/// <summary>
/// El código de recuperación es la única identidad del respaldo: sin correo ni contraseña (HU-26).
/// Son 16 caracteres (80 bits) que no se pueden adivinar, escritos como XXXX-XXXX-XXXX-XXXX.
/// El servidor solo guarda su huella, nunca el código.
/// </summary>
public static class CodigoRecuperacion
{
    // Alfabeto de Crockford: sin I, L, O ni U para no confundir al leer o dictar el código.
    private const string Alfabeto = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";
    private const int Largo = 16;

    public static string Generar()
    {
        Span<byte> bytes = stackalloc byte[10];
        RandomNumberGenerator.Fill(bytes);

        var caracteres = new char[Largo];
        var indice = 0;
        var acumulado = 0;
        var bits = 0;

        foreach (var b in bytes)
        {
            acumulado = (acumulado << 8) | b;
            bits += 8;

            while (bits >= 5)
            {
                bits -= 5;
                caracteres[indice++] = Alfabeto[(acumulado >> bits) & 31];
            }

            acumulado &= (1 << bits) - 1;
        }

        return new string(caracteres);
    }

    /// <summary>
    /// El código en su forma canónica (16 caracteres en mayúscula), o null si no es un código válido.
    /// Acepta guiones, espacios, minúsculas y las confusiones típicas (O por 0, I o L por 1).
    /// </summary>
    public static string? Normalizar(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto)) return null;

        var sb = new StringBuilder(Largo);
        foreach (var c in texto)
        {
            if (c is '-' or ' ' or '\t') continue;

            var mayuscula = char.ToUpperInvariant(c) switch
            {
                'O' => '0',
                'I' or 'L' => '1',
                var x => x,
            };

            if (Alfabeto.IndexOf(mayuscula) < 0) return null;
            sb.Append(mayuscula);
        }

        return sb.Length == Largo ? sb.ToString() : null;
    }

    /// <summary>"ABCD-EFGH-JKMN-PQRS".</summary>
    public static string Formatear(string normalizado) =>
        string.Join('-', Enumerable.Range(0, Largo / 4).Select(i => normalizado.Substring(i * 4, 4)));

    /// <summary>Lo único que el servidor guarda del código.</summary>
    public static string Huella(string normalizado) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalizado)));
}
