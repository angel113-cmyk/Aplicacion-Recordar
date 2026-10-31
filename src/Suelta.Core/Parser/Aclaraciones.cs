namespace Suelta.Core.Parser;

/// <summary>Las opciones que se ofrecen cuando la persona dice una sola palabra (HU-13).</summary>
public static class Aclaraciones
{
    private static readonly OpcionAclaracion Otro = new("Otro", "");

    private static readonly Dictionary<string, OpcionAclaracion[]> PorPalabra = new(StringComparer.Ordinal)
    {
        ["perro"] =
        [
            new("Vacuna", "Llevar al perro a su vacuna"),
            new("Baño", "Bañar al perro"),
            new("Veterinario", "Llevar al perro al veterinario"),
            Otro,
        ],
        ["gato"] =
        [
            new("Vacuna", "Llevar al gato a su vacuna"),
            new("Baño", "Bañar al gato"),
            new("Veterinario", "Llevar al gato al veterinario"),
            Otro,
        ],
        ["auto"] = Auto("auto"),
        ["carro"] = Auto("carro"),
        ["mama"] = Familiar("mamá"),
        ["papa"] = Familiar("papá"),
    };

    /// <summary>Las opciones de esa palabra, o null si no se conocen (entonces se pregunta con texto libre).</summary>
    public static IReadOnlyList<OpcionAclaracion>? Para(string palabra) =>
        PorPalabra.TryGetValue(Normalizar(palabra), out var opciones) ? opciones : null;

    private static OpcionAclaracion[] Auto(string palabra) =>
    [
        new("Taller", $"Llevar el {palabra} al taller"),
        new("Gasolina", $"Echar gasolina al {palabra}"),
        new("Revisión técnica", $"Revisión técnica del {palabra}"),
        Otro,
    ];

    private static OpcionAclaracion[] Familiar(string nombre) =>
    [
        new("Llamar", $"Llamar a {nombre}"),
        new("Visitar", $"Visitar a {nombre}"),
        new("Regalo", $"Comprar regalo para {nombre}"),
        Otro,
    ];

    private static string Normalizar(string s) =>
        s.Trim().ToLowerInvariant().Replace('á', 'a').Replace('é', 'e').Replace('í', 'i').Replace('ó', 'o').Replace('ú', 'u');
}
