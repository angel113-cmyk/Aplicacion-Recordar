using Suelta.Core.Dominio;

namespace Suelta.Core.Parser;

public enum DatoFaltante
{
    /// <summary>Solo se dijo una palabra suelta: no se sabe qué quiere recordar.</summary>
    Accion,

    /// <summary>No se dijo el día. La app nunca inventa una fecha.</summary>
    Dia,
}

/// <summary>Una opción para elegir al toque cuando se dice una sola palabra (HU-13).</summary>
/// <param name="Etiqueta">Lo que se ve en el botón ("Vacuna").</param>
/// <param name="Titulo">El recordatorio que se arma ("Llevar al perro a su vacuna"); vacío en "Otro".</param>
public sealed record OpcionAclaracion(string Etiqueta, string Titulo)
{
    public bool EsOtro => Titulo.Length == 0;
}

/// <summary>Lo que el parser entendió de una frase (HT-04).</summary>
/// <param name="Dijiste">La frase original completa, para mostrarla después en "Dijiste".</param>
/// <param name="Fuente">El texto que se interpretó (un trozo de la frase si se separó en varios recordatorios).</param>
public sealed record ResultadoParser(
    string Dijiste,
    string Fuente,
    string Titulo,
    DateTime? FechaHora,
    TimeOnly? Hora,
    bool SinHora,
    Repeticion Repeticion,
    decimal? Monto,
    Categoria Categoria,
    IReadOnlyList<DatoFaltante> Faltan,
    string? Pregunta,
    string? Persona = null,
    IReadOnlyList<OpcionAclaracion>? Opciones = null)
{
    public bool Completo => Faltan.Count == 0;
}

/// <summary>Lo que ya se entendió de un trozo anterior de la misma frase (HU-08).</summary>
/// <param name="Persona">Para quién es: "cómprale un regalo" después de "cumpleaños de Martín".</param>
/// <param name="Fecha">Fecha de referencia: "el 3" se entiende como el 3 del mismo mes.</param>
public sealed record ContextoFrase(string? Persona, DateOnly? Fecha);
