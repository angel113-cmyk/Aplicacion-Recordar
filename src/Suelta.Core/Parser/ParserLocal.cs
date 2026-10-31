using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Suelta.Core.Dominio;

namespace Suelta.Core.Parser;

/// <summary>
/// Interpreta una frase en español de Perú y saca qué, cuándo, hora, repetición,
/// monto y categoría (HT-04). Nunca inventa una fecha: si falta, lo avisa en Faltan.
/// </summary>
public static class ParserLocal
{
    private const RegexOptions Opc = RegexOptions.CultureInvariant;

    private const string Meses = "enero|febrero|marzo|abril|mayo|junio|julio|agosto|septiembre|setiembre|octubre|noviembre|diciembre";
    private const string Dias = "lunes|martes|miercoles|jueves|viernes|sabado|domingo";
    private const string Momento = "ma[ñn]ana|tarde|noche";

    private static readonly Regex RxMontoSoles = new(@"(?:\bde\s+)?\b(?<n>\d+(?:[.,]\d+)?)\s*soles?\b", Opc);
    private static readonly Regex RxMontoSimbolo = new(@"\bs/\.?\s*(?<n>\d+(?:[.,]\d+)?)", Opc);

    private static readonly Regex RxHoraALas = new(
        @"\ba\s+(?:las?|la)\s+(?<h>\d{1,2}|una)(?::(?<m>\d{2}))?(?:\s*(?<ap>am|pm|a\.m\.|p\.m\.)(?![\p{L}\d])|\s+de\s+la\s+(?<per>" + Momento + @"|madrugada)\b)?", Opc);
    private static readonly Regex RxHoraAmPm = new(@"\b(?<h>\d{1,2})(?::(?<m>\d{2}))?\s*(?<ap>am|pm|a\.m\.|p\.m\.)(?![\p{L}\d])", Opc);
    private static readonly Regex RxHora24 = new(@"\b(?<h>[01]?\d|2[0-3]):(?<m>[0-5]\d)\b", Opc);
    private static readonly Regex RxMediodia = new(@"\b(?:al\s+)?mediodia\b", Opc);
    private static readonly Regex RxEstaMomento = new(@"\besta\s+(?<per>" + Momento + @")\b", Opc);
    private static readonly Regex RxEnMomento = new(@"\b(?:en|por|de)\s+la\s+(?<per>" + Momento + @")\b", Opc);

    private static readonly Regex RxDiaria = new(@"\b(?:cada\s+dia|todos\s+los\s+dias|diariamente|diario)\b", Opc);
    private static readonly Regex RxSemanalDia = new(@"\b(?:cada|todos\s+los|todas\s+las)\s+(?<dow>" + Dias + @")s?\b", Opc);
    private static readonly Regex RxSemanal = new(@"\b(?:cada\s+semana|todas\s+las\s+semanas|semanalmente|semanal)\b", Opc);
    private static readonly Regex RxMensual = new(@"\b(?:cada\s+mes|todos\s+los\s+meses|mensualmente|mensual)\b", Opc);
    private static readonly Regex RxAnual = new(@"\b(?:cada\s+a[ñn]o|todos\s+los\s+a[ñn]os|anualmente|anual)\b", Opc);

    private static readonly Regex RxPasadoManana = new(@"\bpasado\s+ma[ñn]ana\b", Opc);
    private static readonly Regex RxManana = new(@"\bma[ñn]ana\b", Opc);
    private static readonly Regex RxHoy = new(@"\bhoy\b", Opc);
    private static readonly Regex RxDiaMes = new(
        @"\b(?:el\s+)?(?<d>\d{1,2})\s+de\s+(?<mes>" + Meses + @")(?:\s+(?:de|del)\s+(?<a>\d{4}))?\b", Opc);
    private static readonly Regex RxDiaSemana = new(
        @"\b(?:(?:el\s+)?proximo\s+|este\s+|el\s+)?(?<dow>" + Dias + @")s?\b", Opc);
    private static readonly Regex RxDiaNumero = new(@"\b(?:el\s+dia\s+|el\s+|dia\s+)(?<d>\d{1,2})\b", Opc);

    private static readonly Regex RxCumple = new(
        @"\bcumple(?:a[ñn]os)?\s+de\s+(?<pos>(?:mi|tu|su)\s+)?(?<n1>\p{L}+)(?:\s+(?<n2>\p{L}+))?", Opc);
    private static readonly Regex RxEsCumple = new(@"\bcumplea[ñn]os\b|\bcumple\b", Opc);

    private static readonly Regex RxInicioRelleno = new(
        @"^(?:(?:recuerdame|recordarme|avisame|acuerdame)(?:\s+(?:que|de))?|tengo\s+que|debo|hay\s+que|no\s+olvidar|no\s+olvides|para|y|que|es|son|de|del|en|a)(?![\p{L}\d])[\s,;.]*", Opc);
    private static readonly Regex RxFinRelleno = new(@"[\s,;.]+(?:el|la|los|las|de|del|a|al|en|para|y|que)$", Opc);

    private static readonly (Categoria Cat, Regex Rx)[] ReglasCategoria =
    [
        (Categoria.Salud, new Regex(@"\b(?:pastillas?|medicinas?|medicamentos?|doctor|doctora|dentista|vacunas?|veterinari[oa]|medic[oa]|salud|hospital|clinica|terapia|pomada|jarabe|analisis)\b", Opc)),
        (Categoria.Auto, new Regex(@"\b(?:auto|carro|coche|taller|gasolina|mecanico|soat|revision\s+tecnica|llantas?|aceite|moto)\b", Opc)),
        (Categoria.Pagos, new Regex(@"\b(?:pagar|pago|recibo|luz|agua|alquiler|renta|internet|cuota|deuda|factura|mensualidad|tarjeta|telefono)\b", Opc)),
        (Categoria.Compras, new Regex(@"\b(?:comprar|compra|compras|mercado|supermercado|regalo|pan|super|bodega|tienda)\b", Opc)),
        (Categoria.Citas, new Regex(@"\b(?:cita|reunion|entrevista|junta|clase|examen)\b", Opc)),
    ];

    public static ResultadoParser Interpretar(string frase, DateTime ahora)
    {
        var original = (frase ?? "").Trim();
        var t = new Texto(original);
        var hoy = DateOnly.FromDateTime(ahora);

        // --- monto ---
        decimal? monto = null;
        var m = t.Consumir(RxMontoSoles) ?? t.Consumir(RxMontoSimbolo);
        if (m is not null)
            monto = decimal.Parse(m.Groups["n"].Value.Replace(',', '.'), CultureInfo.InvariantCulture);

        // --- hora ---
        TimeOnly? hora = null;
        DateOnly? fecha = null;

        m = t.Consumir(RxHoraALas) ?? t.Consumir(RxHoraAmPm) ?? t.Consumir(RxHora24);
        if (m is not null)
        {
            var hTxt = m.Groups["h"].Value;
            var h = hTxt == "una" ? 1 : int.Parse(hTxt, CultureInfo.InvariantCulture);
            var min = m.Groups["m"].Success ? int.Parse(m.Groups["m"].Value, CultureInfo.InvariantCulture) : 0;
            var ap = m.Groups["ap"].Success ? m.Groups["ap"].Value.Replace(".", "") : null;
            var per = m.Groups["per"].Success ? m.Groups["per"].Value : null;
            hora = ResolverHora(h, min, ap, per);
        }
        else if (t.Consumir(RxMediodia) is not null)
        {
            hora = new TimeOnly(12, 0);
        }

        if (hora is null)
        {
            m = t.Consumir(RxEstaMomento);
            if (m is not null)
            {
                fecha = hoy;
                hora = HoraDeMomento(m.Groups["per"].Value);
            }
            else if ((m = t.Consumir(RxEnMomento)) is not null)
            {
                hora = HoraDeMomento(m.Groups["per"].Value);
            }
        }

        // --- repetición ---
        var repeticion = Repeticion.Ninguna;
        DayOfWeek? diaSemanaRep = null;

        if ((m = t.Consumir(RxSemanalDia)) is not null)
        {
            repeticion = Repeticion.Semanal;
            diaSemanaRep = ADayOfWeek(m.Groups["dow"].Value);
        }
        else if (t.Consumir(RxDiaria) is not null) repeticion = Repeticion.Diaria;
        else if (t.Consumir(RxSemanal) is not null) repeticion = Repeticion.Semanal;
        else if (t.Consumir(RxMensual) is not null) repeticion = Repeticion.Mensual;
        else if (t.Consumir(RxAnual) is not null) repeticion = Repeticion.Anual;

        // --- fecha ---
        if (fecha is null)
        {
            if (t.Consumir(RxPasadoManana) is not null) fecha = hoy.AddDays(2);
            else if (t.Consumir(RxManana) is not null) fecha = hoy.AddDays(1);
            else if (t.Consumir(RxHoy) is not null) fecha = hoy;
            else if ((m = t.Consumir(RxDiaMes)) is not null)
                fecha = ResolverDiaMes(m, hoy);
            else if ((m = t.Consumir(RxDiaSemana)) is not null)
                fecha = ProximoDiaSemana(ADayOfWeek(m.Groups["dow"].Value), hoy, hora, TimeOnly.FromDateTime(ahora));
            else if ((m = t.Consumir(RxDiaNumero)) is not null)
                fecha = ProximoDiaDelMes(int.Parse(m.Groups["d"].Value, CultureInfo.InvariantCulture), hoy);
        }

        // Lo que la repetición permite deducir cuando no se dijo el día.
        if (fecha is null)
        {
            if (repeticion == Repeticion.Diaria)
                fecha = hora is null || hora.Value > TimeOnly.FromDateTime(ahora) ? hoy : hoy.AddDays(1);
            else if (repeticion == Repeticion.Semanal && diaSemanaRep is not null)
                fecha = ProximoDiaSemana(diaSemanaRep.Value, hoy, hora, TimeOnly.FromDateTime(ahora));
        }

        // --- cumpleaños ---
        var esCumple = RxEsCumple.IsMatch(t.Normalizado);
        string? nombreCumple = null;
        string titulo;
        var cumple = RxCumple.Match(t.Normalizado);
        if (cumple.Success)
        {
            nombreCumple = NombreCumple(original, cumple);
            titulo = "Cumpleaños de " + nombreCumple;
        }
        else
        {
            titulo = LimpiarTitulo(t.Resto());
            if (esCumple)
                titulo = "Cumpleaños";
        }

        if (esCumple && repeticion == Repeticion.Ninguna)
            repeticion = Repeticion.Anual;

        if (monto is not null && titulo.Length > 0 && !esCumple)
            titulo += $" (S/ {monto.Value.ToString("0.##", CultureInfo.InvariantCulture)})";

        var categoria = esCumple ? Categoria.Cumpleanos : DetectarCategoria(t.NormalizadoOriginal);

        // --- qué falta ---
        var faltan = new List<DatoFaltante>();
        var palabras = titulo.Length == 0 ? 0 : titulo.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        var sinNingunDato = fecha is null && hora is null && repeticion == Repeticion.Ninguna;
        if (titulo.Length == 0 || (palabras == 1 && sinNingunDato && !esCumple))
            faltan.Add(DatoFaltante.Accion);
        if (fecha is null)
            faltan.Add(DatoFaltante.Dia);

        DateTime? fechaHora = fecha is null ? null : fecha.Value.ToDateTime(hora ?? TimeOnly.MinValue);

        return new ResultadoParser(
            original,
            titulo,
            fechaHora,
            hora is null,
            repeticion,
            monto,
            categoria,
            faltan,
            ConstruirPregunta(faltan, titulo, nombreCumple));
    }

    /// <summary>
    /// Completa una frase a la que le faltaba un dato: junta la frase base con la respuesta
    /// y la interpreta de nuevo, pero conserva lo que la persona dijo al principio.
    /// </summary>
    public static ResultadoParser Completar(string fraseBase, string respuesta, DateTime ahora)
    {
        var r = Interpretar($"{fraseBase} {respuesta}", ahora);
        return r with { Dijiste = fraseBase.Trim() };
    }

    private static string? ConstruirPregunta(List<DatoFaltante> faltan, string titulo, string? nombreCumple)
    {
        if (faltan.Count == 0) return null;

        if (faltan[0] == DatoFaltante.Accion)
        {
            if (titulo.Length == 0) return "¿Qué quieres recordar?";
            var palabra = titulo.ToLowerInvariant();
            var articulo = palabra.EndsWith('a') ? "de la" : "del";
            return $"¿Qué quieres recordar {articulo} {palabra}?";
        }

        return nombreCumple is not null ? $"¿Qué día cumple {nombreCumple}?" : "¿Qué día?";
    }

    private static string NombreCumple(string original, Match cumple)
    {
        var pos = cumple.Groups["pos"];
        var n1 = cumple.Groups["n1"];
        var n2 = cumple.Groups["n2"];

        var sb = new StringBuilder();
        if (pos.Success) sb.Append(original.AsSpan(pos.Index, pos.Length).Trim()).Append(' ');
        sb.Append(original.AsSpan(n1.Index, n1.Length));

        // Un segundo nombre solo si va con mayúscula ("Martín Pérez"); si no, es otra palabra de la frase.
        if (n2.Success && char.IsUpper(original[n2.Index]))
            sb.Append(' ').Append(original.AsSpan(n2.Index, n2.Length));

        return sb.ToString();
    }

    private static TimeOnly? ResolverHora(int h, int min, string? ap, string? per)
    {
        if (h > 23 || min > 59) return null;

        if (ap == "pm")
        {
            if (h < 12) h += 12;
        }
        else if (ap == "am")
        {
            if (h == 12) h = 0;
        }
        else if (per is not null)
        {
            if (per.StartsWith("ma", StringComparison.Ordinal) && per != "madrugada")
            {
                if (h == 12) h = 0;
            }
            else if (per is "tarde" or "noche")
            {
                if (h < 12) h += 12;
                else if (h == 12 && per == "noche") h = 0;
            }
        }
        else if (h is >= 1 and <= 6)
        {
            // Sin aclaración, "a las 3" se entiende como de la tarde; "a las 9" queda de la mañana.
            h += 12;
        }

        return new TimeOnly(h, min);
    }

    private static TimeOnly HoraDeMomento(string momento) => momento switch
    {
        "tarde" => new TimeOnly(16, 0),
        "noche" => new TimeOnly(20, 0),
        _ => new TimeOnly(8, 0),
    };

    private static DateOnly? ResolverDiaMes(Match m, DateOnly hoy)
    {
        var dia = int.Parse(m.Groups["d"].Value, CultureInfo.InvariantCulture);
        var mes = NumeroMes(m.Groups["mes"].Value);

        if (m.Groups["a"].Success)
        {
            var anio = int.Parse(m.Groups["a"].Value, CultureInfo.InvariantCulture);
            return Valida(anio, mes, dia) ? new DateOnly(anio, mes, dia) : null;
        }

        for (var anio = hoy.Year; anio <= hoy.Year + 4; anio++)
        {
            if (!Valida(anio, mes, dia)) continue;
            var f = new DateOnly(anio, mes, dia);
            if (f >= hoy) return f;
        }

        return null;
    }

    private static int NumeroMes(string nombre) => nombre switch
    {
        "enero" => 1,
        "febrero" => 2,
        "marzo" => 3,
        "abril" => 4,
        "mayo" => 5,
        "junio" => 6,
        "julio" => 7,
        "agosto" => 8,
        "septiembre" or "setiembre" => 9,
        "octubre" => 10,
        "noviembre" => 11,
        _ => 12,
    };

    private static bool Valida(int anio, int mes, int dia) =>
        mes is >= 1 and <= 12 && dia >= 1 && dia <= DateTime.DaysInMonth(anio, mes);

    private static DateOnly? ProximoDiaDelMes(int dia, DateOnly hoy)
    {
        if (dia is < 1 or > 31) return null;

        var y = hoy.Year;
        var mes = hoy.Month;
        for (var i = 0; i < 14; i++)
        {
            if (Valida(y, mes, dia))
            {
                var f = new DateOnly(y, mes, dia);
                if (f >= hoy) return f;
            }

            mes++;
            if (mes > 12) { mes = 1; y++; }
        }

        return null;
    }

    private static DateOnly ProximoDiaSemana(DayOfWeek dow, DateOnly hoy, TimeOnly? hora, TimeOnly horaActual)
    {
        var diff = ((int)dow - (int)hoy.DayOfWeek + 7) % 7;
        if (diff == 0 && !(hora is not null && hora.Value > horaActual))
            diff = 7;
        return hoy.AddDays(diff);
    }

    private static DayOfWeek ADayOfWeek(string nombre) => nombre switch
    {
        "lunes" => DayOfWeek.Monday,
        "martes" => DayOfWeek.Tuesday,
        "miercoles" => DayOfWeek.Wednesday,
        "jueves" => DayOfWeek.Thursday,
        "viernes" => DayOfWeek.Friday,
        "sabado" => DayOfWeek.Saturday,
        _ => DayOfWeek.Sunday,
    };

    private static Categoria DetectarCategoria(string normalizado)
    {
        foreach (var (cat, rx) in ReglasCategoria)
            if (rx.IsMatch(normalizado))
                return cat;
        return Categoria.Otro;
    }

    private static string LimpiarTitulo(string s)
    {
        s = Regex.Replace(s, @"\s+", " ").Trim();
        s = Regex.Replace(s, @"\s+([,.;:])", "$1");
        s = s.Trim(' ', ',', '.', ';', ':', '¡', '!', '¿', '?');

        var cambio = true;
        while (cambio && s.Length > 0)
        {
            cambio = false;

            var inicio = RxInicioRelleno.Match(Texto.Normalizar(s));
            if (inicio.Success && inicio.Length < s.Length)
            {
                s = s[inicio.Length..].TrimStart();
                cambio = true;
            }

            var fin = RxFinRelleno.Match(Texto.Normalizar(s));
            if (fin.Success && fin.Index > 0)
            {
                s = s[..fin.Index].TrimEnd(' ', ',', ';', '.');
                cambio = true;
            }
        }

        return s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s[1..];
    }

    /// <summary>
    /// Texto original con una copia sin tildes ni mayúsculas del mismo largo, para buscar
    /// con expresiones regulares y marcar lo que ya se entendió.
    /// </summary>
    private sealed class Texto
    {
        private readonly string _original;
        private readonly string _normalizadoOriginal;
        private readonly char[] _trabajo;
        private readonly bool[] _usado;

        public Texto(string original)
        {
            _original = original;
            _normalizadoOriginal = Normalizar(original);
            _trabajo = _normalizadoOriginal.ToCharArray();
            _usado = new bool[original.Length];
        }

        public string Normalizado => new(_trabajo);

        public string NormalizadoOriginal => _normalizadoOriginal;

        public Match? Consumir(Regex rx)
        {
            var m = rx.Match(Normalizado);
            if (!m.Success) return null;

            for (var i = m.Index; i < m.Index + m.Length; i++)
            {
                _trabajo[i] = ' ';
                _usado[i] = true;
            }

            return m;
        }

        public string Resto()
        {
            var sb = new StringBuilder(_original.Length);
            for (var i = 0; i < _original.Length; i++)
                sb.Append(_usado[i] ? ' ' : _original[i]);
            return sb.ToString();
        }

        public static string Normalizar(string s)
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
}
