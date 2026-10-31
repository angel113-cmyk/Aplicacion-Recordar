using Suelta.Core.Dominio;

namespace Suelta.Core.Parser;

public enum DatoFaltante
{
    /// <summary>Solo se dijo una palabra suelta: no se sabe qué quiere recordar.</summary>
    Accion,

    /// <summary>No se dijo el día. La app nunca inventa una fecha.</summary>
    Dia,
}

/// <summary>Lo que el parser entendió de una frase (HT-04).</summary>
public sealed record ResultadoParser(
    string Dijiste,
    string Titulo,
    DateTime? FechaHora,
    bool SinHora,
    Repeticion Repeticion,
    decimal? Monto,
    Categoria Categoria,
    IReadOnlyList<DatoFaltante> Faltan,
    string? Pregunta)
{
    public bool Completo => Faltan.Count == 0;
}
