using System.Text;
using System.Text.RegularExpressions;

namespace Suelta.Core.Formato;

/// <summary>Un pedazo de la frase; Resaltado es lo que la app ya entendió (una fecha, una hora, un monto).</summary>
public sealed record TrozoFrase(string Texto, bool Resaltado);

/// <summary>
/// Marca en la frase lo que Suelta entendió, para que la persona lo vea mientras habla:
/// "<b>mañana</b> a las 8 pastilla". Solo es presentación: el que interpreta de verdad es el parser.
/// </summary>
public static class ResaltadoFrase
{
    private const string Meses = "enero|febrero|marzo|abril|mayo|junio|julio|agosto|septiembre|setiembre|octubre|noviembre|diciembre";
    private const string Dias = "domingo|lunes|martes|miercoles|jueves|viernes|sabado";
    private const string Momento = "ma[ñn]ana|tarde|noche";

    private static readonly Regex Entendido = new(
        @"\b(?:pasado\s+ma[ñn]ana|ma[ñn]ana|hoy|(?:el\s+|este\s+|proximo\s+)?(?:" + Dias + @")s?" +
        @"|todos\s+los\s+dias|cada\s+(?:dia|semana|mes|a[ñn]o)|fines?\s+de\s+semana)\b" +
        @"|\b(?:el\s+)?\d{1,2}\s+de\s+(?:" + Meses + @")\b" +
        @"|\b(?:en|por|de)\s+la\s+(?:" + Momento + @")\b" +
        @"|\ba\s+las?\s+\d{1,2}(?::\d{2})?(?:\s*(?:am|pm|de\s+la\s+(?:" + Momento + @")))?(?![\p{L}\d])" +
        @"|\b\d{1,2}(?::\d{2})?\s*(?:am|pm)(?![\p{L}\d])" +
        @"|\b\d+(?:[.,]\d+)?\s*soles?\b",
        RegexOptions.CultureInvariant);

    public static IReadOnlyList<TrozoFrase> Dividir(string? frase)
    {
        var texto = frase ?? "";
        var trozos = new List<TrozoFrase>();
        var ultimo = 0;

        // Se busca en una copia sin tildes ni mayúsculas del mismo largo, y se corta el texto original.
        foreach (Match m in Entendido.Matches(Normalizar(texto)))
        {
            if (m.Length == 0) continue;

            if (m.Index > ultimo) trozos.Add(new TrozoFrase(texto[ultimo..m.Index], false));
            trozos.Add(new TrozoFrase(texto.Substring(m.Index, m.Length), true));
            ultimo = m.Index + m.Length;
        }

        if (ultimo < texto.Length) trozos.Add(new TrozoFrase(texto[ultimo..], false));
        return trozos;
    }

    private static string Normalizar(string s)
    {
        var sb = new StringBuilder(s.Length);
        foreach (var c in s)
        {
            sb.Append(char.ToLowerInvariant(c) switch
            {
                'á' => 'a',
                'é' => 'e',
                'í' => 'i',
                'ó' => 'o',
                'ú' or 'ü' => 'u',
                var x => x,
            });
        }

        return sb.ToString();
    }
}
