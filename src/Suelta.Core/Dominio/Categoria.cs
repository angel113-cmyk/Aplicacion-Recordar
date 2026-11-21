namespace Suelta.Core.Dominio;

public enum Categoria
{
    Cumpleanos,
    Salud,
    Compras,
    Auto,
    Pagos,
    Citas,
    Otro,
}

/// <param name="Icono">El nombre del ícono (cake, heart, bag, car, card, brief, dots); la app lo dibuja en SVG.</param>
public sealed record CategoriaInfo(Categoria Categoria, string Nombre, string Color, string Icono)
{
    private static readonly CategoriaInfo[] Todas =
    [
        new(Categoria.Cumpleanos, "Cumpleaños", "#E8457A", "cake"),
        new(Categoria.Salud, "Salud", "#0FA3A3", "heart"),
        new(Categoria.Compras, "Compras", "#1E9E6A", "bag"),
        new(Categoria.Auto, "Auto", "#2B7BE4", "car"),
        new(Categoria.Pagos, "Pagos", "#D98A0B", "card"),
        new(Categoria.Citas, "Citas", "#8A4FE0", "brief"),
        new(Categoria.Otro, "Otro", "#7A7F9A", "dots"),
    ];

    public static CategoriaInfo De(Categoria categoria) => Todas[(int)categoria];

    public static IReadOnlyList<CategoriaInfo> Lista => Todas;
}
