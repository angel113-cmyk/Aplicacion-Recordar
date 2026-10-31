using Suelta.Core.Dominio;
using Suelta.Core.Formato;
using Suelta.Core.Lista;

namespace Suelta.Tests;

public class ListaTests
{
    private static readonly DateTime Ahora = new(2026, 10, 29, 10, 0, 0);

    private static Recordatorio R(string titulo, DateTime? fh, bool sinHora = false, bool hecho = false) => new()
    {
        Id = Guid.NewGuid(),
        Titulo = titulo,
        FechaHora = fh,
        SinHora = sinHora,
        Hecho = hecho,
        CreadoEn = Ahora,
    };

    [Fact]
    public void Agrupa_en_el_orden_pedido_y_oculta_grupos_vacios()
    {
        var items = new[]
        {
            R("hecho", new DateTime(2026, 10, 29, 18, 0, 0), hecho: true),
            R("lejos", new DateTime(2026, 12, 1), sinHora: true),
            R("sin fecha", null),
            R("semana", new DateTime(2026, 11, 5, 15, 0, 0)),
            R("manana", new DateTime(2026, 10, 30), sinHora: true),
            R("hoy", new DateTime(2026, 10, 29, 18, 0, 0)),
            R("pasado", new DateTime(2026, 10, 28, 9, 0, 0)),
        };

        var grupos = AgrupadorLista.Agrupar(items, Ahora);

        Assert.Equal(
            [GrupoFecha.SeTePaso, GrupoFecha.Hoy, GrupoFecha.Manana, GrupoFecha.EstaSemana, GrupoFecha.MasAdelante, GrupoFecha.SinFecha, GrupoFecha.Hechos],
            grupos.Select(g => g.Grupo));
    }

    [Fact]
    public void Un_recordatorio_de_hoy_con_hora_ya_pasada_es_se_te_paso()
    {
        var r = R("temprano", new DateTime(2026, 10, 29, 8, 0, 0));

        Assert.Equal(GrupoFecha.SeTePaso, AgrupadorLista.Clasificar(r, Ahora));
    }

    [Fact]
    public void Un_recordatorio_de_hoy_sin_hora_sigue_en_hoy()
    {
        var r = R("todo el dia", new DateTime(2026, 10, 29), sinHora: true);

        Assert.Equal(GrupoFecha.Hoy, AgrupadorLista.Clasificar(r, Ahora));
    }

    [Fact]
    public void Dentro_de_cada_grupo_se_ordena_por_fecha_y_hora()
    {
        var items = new[]
        {
            R("tarde", new DateTime(2026, 10, 29, 20, 0, 0)),
            R("noche", new DateTime(2026, 10, 29, 22, 0, 0)),
            R("mediodia", new DateTime(2026, 10, 29, 12, 0, 0)),
        };

        var hoy = AgrupadorLista.Agrupar(items, Ahora).Single();

        Assert.Equal(["mediodia", "tarde", "noche"], hoy.Items.Select(i => i.Titulo));
    }

    [Fact]
    public void Los_eliminados_no_aparecen()
    {
        var borrado = R("borrado", new DateTime(2026, 10, 29, 18, 0, 0));
        borrado.EliminadoEn = Ahora;

        Assert.Empty(AgrupadorLista.Agrupar([borrado], Ahora));
    }

    [Fact]
    public void Formato_de_tarjeta_como_en_el_prototipo()
    {
        Assert.Equal("Hoy · 6 pm", FormatoFechas.Cuando(R("a", new DateTime(2026, 10, 29, 18, 0, 0)), Ahora));
        Assert.Equal("Mañana", FormatoFechas.Cuando(R("a", new DateTime(2026, 10, 30), sinHora: true), Ahora));
        Assert.Equal("jue 5 nov · 3 pm", FormatoFechas.Cuando(R("a", new DateTime(2026, 11, 5, 15, 0, 0)), Ahora));
        Assert.Equal("jue 5 nov · 9:30 am", FormatoFechas.Cuando(R("a", new DateTime(2026, 11, 5, 9, 30, 0)), Ahora));
        Assert.Equal("Jueves 29 de octubre", FormatoFechas.Cabecera(Ahora));
    }

    [Fact]
    public void Etiquetas_de_repeticion_y_aviso()
    {
        var mensual = new Recordatorio { Repeticion = Repeticion.Mensual, FechaHora = new DateTime(2026, 10, 30) };
        var cumple = new Recordatorio { Categoria = Categoria.Cumpleanos, Repeticion = Repeticion.Anual };

        Assert.Equal("Cada mes, día 30", FormatoFechas.EtiquetaRepeticion(mensual));
        Assert.Equal("Cada año", FormatoFechas.EtiquetaRepeticion(cumple));
        Assert.Equal("Te aviso 3 días antes", FormatoFechas.AvisoAnticipado(cumple));
        Assert.Equal("", FormatoFechas.AvisoAnticipado(mensual));
    }
}
