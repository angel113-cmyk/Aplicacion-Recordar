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

public sealed record CategoriaInfo(Categoria Categoria, string Nombre, string Color, string Icono)
{
    private static readonly CategoriaInfo[] Todas =
    [
        new(Categoria.Cumpleanos, "Cumpleaños", "#E8457A", "🎂"),
        new(Categoria.Salud, "Salud", "#1AA39A", "🩺"),
        new(Categoria.Compras, "Compras", "#2EA44F", "🛒"),
        new(Categoria.Auto, "Auto", "#2F6FDE", "🚗"),
        new(Categoria.Pagos, "Pagos", "#E09A1B", "💳"),
        new(Categoria.Citas, "Citas", "#7B5CE0", "📅"),
        new(Categoria.Otro, "Otro", "#727C95", "🕐"),
    ];

    public static CategoriaInfo De(Categoria categoria) => Todas[(int)categoria];

    public static IReadOnlyList<CategoriaInfo> Lista => Todas;
}
