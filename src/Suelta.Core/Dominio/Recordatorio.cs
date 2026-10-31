namespace Suelta.Core.Dominio;

/// <summary>
/// Un recordatorio guardado en el teléfono. Las fechas son la hora de pared de Lima
/// (sin zona); la conversión a UTC se hace solo al sincronizar con el servidor.
/// </summary>
public class Recordatorio
{
    /// <summary>Días de anticipación del aviso de un cumpleaños (HU-07, HU-22).</summary>
    public const int DiasAvisoCumpleanos = 3;

    public Guid Id { get; set; }
    public string Titulo { get; set; } = "";
    public Categoria Categoria { get; set; } = Categoria.Otro;
    public DateTime? FechaHora { get; set; }
    public bool SinHora { get; set; }
    public Repeticion Repeticion { get; set; } = Repeticion.Ninguna;
    public decimal? Monto { get; set; }
    public string Dijiste { get; set; } = "";
    public bool Hecho { get; set; }
    public DateTime CreadoEn { get; set; }
    public DateTime ActualizadoEn { get; set; }
    public DateTime? EliminadoEn { get; set; }

    public int DiasAvisoAnticipado => Categoria == Categoria.Cumpleanos ? DiasAvisoCumpleanos : 0;
}
