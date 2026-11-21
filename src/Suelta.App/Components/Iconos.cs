namespace Suelta.App.Components;

/// <summary>Íconos en SVG de trazo, sin relleno: heredan el color del texto, así combinan con cualquier fondo.</summary>
public static class Iconos
{
    private static readonly Dictionary<string, string> Trazos = new()
    {
        ["mic"] = "<rect x=\"9\" y=\"3\" width=\"6\" height=\"11\" rx=\"3\"/><path d=\"M5 11a7 7 0 0 0 14 0M12 18v3\"/>",
        ["bell"] = "<path d=\"M6 16v-5a6 6 0 0 1 12 0v5l2 2H4l2-2zM10 21h4\"/>",
        ["list"] = "<path d=\"M9 6h11M9 12h11M9 18h11M4.5 6h.01M4.5 12h.01M4.5 18h.01\"/>",
        ["grid"] = "<rect x=\"4\" y=\"4\" width=\"7\" height=\"7\" rx=\"2\"/><rect x=\"13\" y=\"4\" width=\"7\" height=\"7\" rx=\"2\"/><rect x=\"4\" y=\"13\" width=\"7\" height=\"7\" rx=\"2\"/><rect x=\"13\" y=\"13\" width=\"7\" height=\"7\" rx=\"2\"/>",
        ["pen"] = "<path d=\"M4 20l4-1 11-11a2.2 2.2 0 0 0-3-3L5 16l-1 4z\"/>",
        ["heart"] = "<path d=\"M12 20s-7-4.5-7-10a4 4 0 0 1 7-2.5A4 4 0 0 1 19 10c0 5.5-7 10-7 10z\"/>",
        ["car"] = "<path d=\"M5 16v-5l2-5h10l2 5v5M3 16h18M7 19v-3M17 19v-3M8 12h.01M16 12h.01\"/>",
        ["bag"] = "<path d=\"M6 8h12l1 12H5L6 8zM9 8a3 3 0 0 1 6 0\"/>",
        ["card"] = "<rect x=\"3\" y=\"6\" width=\"18\" height=\"12\" rx=\"2.5\"/><path d=\"M3 10h18M7 15h3\"/>",
        ["cake"] = "<path d=\"M5 20h14v-6H5zM5 14c2 1.5 4-1.5 7 0s5 1 7 0M12 10V7M12 4.5v.01\"/>",
        ["brief"] = "<rect x=\"3\" y=\"7\" width=\"18\" height=\"13\" rx=\"2.5\"/><path d=\"M9 7V5h6v2M3 13h18\"/>",
        ["dots"] = "<path d=\"M6 12h.01M12 12h.01M18 12h.01\" stroke-width=\"3.4\"/>",
        ["check"] = "<path d=\"M5 12.5l4.5 4.5L19 7.5\"/>",
        ["undo"] = "<path d=\"M9 7L4 12l5 5M4 12h10a6 6 0 0 1 0 8\"/>",
        ["back"] = "<path d=\"M15 5l-7 7 7 7\"/>",
        ["calendar"] = "<rect x=\"3\" y=\"5\" width=\"18\" height=\"16\" rx=\"3\"/><path d=\"M3 10h18M8 3v4M16 3v4\"/>",
        ["settings"] = "<circle cx=\"12\" cy=\"12\" r=\"3\"/><path d=\"M19.4 15a1.7 1.7 0 0 0 .3 1.8l.1.1a2 2 0 1 1-2.8 2.8l-.1-.1a1.7 1.7 0 0 0-1.8-.3 1.7 1.7 0 0 0-1 1.5V21a2 2 0 1 1-4 0v-.1a1.7 1.7 0 0 0-1.1-1.5 1.7 1.7 0 0 0-1.8.3l-.1.1a2 2 0 1 1-2.8-2.8l.1-.1a1.7 1.7 0 0 0 .3-1.8 1.7 1.7 0 0 0-1.5-1H3a2 2 0 1 1 0-4h.1a1.7 1.7 0 0 0 1.5-1.1 1.7 1.7 0 0 0-.3-1.8l-.1-.1a2 2 0 1 1 2.8-2.8l.1.1a1.7 1.7 0 0 0 1.8.3H9a1.7 1.7 0 0 0 1-1.5V3a2 2 0 1 1 4 0v.1a1.7 1.7 0 0 0 1 1.5 1.7 1.7 0 0 0 1.8-.3l.1-.1a2 2 0 1 1 2.8 2.8l-.1.1a1.7 1.7 0 0 0-.3 1.8V9a1.7 1.7 0 0 0 1.5 1H21a2 2 0 1 1 0 4h-.1a1.7 1.7 0 0 0-1.5 1z\"/>",
    };

    /// <summary>El SVG de un ícono por su nombre (un cuadrado del tamaño pedido, en píxeles).</summary>
    public static string Svg(string nombre, int tamano = 24)
    {
        var trazo = Trazos.GetValueOrDefault(nombre, Trazos["dots"]);
        return $"<svg class=\"i\" viewBox=\"0 0 24 24\" width=\"{tamano}\" height=\"{tamano}\" fill=\"none\" stroke=\"currentColor\" " +
               $"stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\">{trazo}</svg>";
    }

    // Atajos con el tamaño que más se usa.
    public static string Microfono => Svg("mic");
    public static string Campana => Svg("bell");
    public static string Calendario => Svg("calendar");
    public static string Visto => Svg("check", 18);
    public static string Ajustes => Svg("settings", 22);
}
