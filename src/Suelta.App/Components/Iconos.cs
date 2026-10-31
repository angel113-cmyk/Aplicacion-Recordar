namespace Suelta.App.Components;

/// <summary>Íconos en SVG (trazo, sin relleno) que heredan el color del texto.</summary>
public static class Iconos
{
    private const string Cabecera = "<svg viewBox=\"0 0 24 24\" width=\"1em\" height=\"1em\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\">";

    public const string Microfono = Cabecera + "<rect x=\"9\" y=\"3\" width=\"6\" height=\"11\" rx=\"3\"/><path d=\"M5 11a7 7 0 0 0 14 0M12 18v3\"/></svg>";

    public const string Campana = Cabecera + "<path d=\"M6 17V11a6 6 0 0 1 12 0v6l1.5 2h-15zM10 21h4\"/></svg>";
}
