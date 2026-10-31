using Suelta.Core.Dominio;
using Suelta.Core.Parser;

namespace Suelta.Tests;

public class Sprint2ParserTests
{
    // Jueves 29 de octubre de 2026, 10:00.
    private static readonly DateTime Ahora = new(2026, 10, 29, 10, 0, 0);

    private const string FraseLarga =
        "El 5 de noviembre es el cumpleaños de Martín, cómprale un regalo de 20 soles el 3 en la tarde";

    // ---- HU-08: separar una frase larga ----

    [Fact]
    public void Una_frase_larga_se_separa_en_cumpleanos_y_regalo()
    {
        var r = ParserLocal.InterpretarVarios(FraseLarga, Ahora);

        Assert.Equal(2, r.Count);

        Assert.Equal("Cumpleaños de Martín", r[0].Titulo);
        Assert.Equal(new DateTime(2026, 11, 5), r[0].FechaHora);

        Assert.Equal("Comprar regalo para Martín (S/ 20)", r[1].Titulo);
        Assert.Equal(new DateTime(2026, 11, 3, 16, 0, 0), r[1].FechaHora);
        Assert.Equal(20m, r[1].Monto);
        Assert.Equal(Categoria.Compras, r[1].Categoria);

        Assert.All(r, x => Assert.True(x.Completo));
        Assert.All(r, x => Assert.Equal(FraseLarga, x.Dijiste));
    }

    [Fact]
    public void Una_hora_o_fecha_suelta_entre_comas_no_crea_otro_recordatorio()
    {
        var r = ParserLocal.InterpretarVarios("mañana, pagar el recibo, a las 5", Ahora);

        var unico = Assert.Single(r);
        Assert.Equal("Pagar el recibo", unico.Titulo);
        Assert.Equal(new DateTime(2026, 10, 30, 17, 0, 0), unico.FechaHora);
    }

    [Fact]
    public void Los_decimales_con_coma_no_separan_la_frase()
    {
        var r = ParserLocal.InterpretarVarios("mañana pagar 15,50 soles de internet", Ahora);

        Assert.Single(r);
        Assert.Equal(15.5m, r[0].Monto);
    }

    [Fact]
    public void Una_frase_simple_da_un_solo_recordatorio()
    {
        var r = ParserLocal.InterpretarVarios("mañana pagar el recibo", Ahora);

        Assert.Single(r);
    }

    // ---- HU-09: repeticiones ----

    [Fact]
    public void Los_fines_de_semana_son_sabado_y_domingo()
    {
        var r = ParserLocal.InterpretarVarios("salir a caminar los fines de semana a las 8", Ahora);

        Assert.Equal(2, r.Count);
        Assert.All(r, x => Assert.Equal("Salir a caminar", x.Titulo));
        Assert.All(r, x => Assert.Equal(Repeticion.Semanal, x.Repeticion));
        Assert.Equal(new DateTime(2026, 10, 31, 8, 0, 0), r[0].FechaHora);
        Assert.Equal(DayOfWeek.Saturday, r[0].FechaHora!.Value.DayOfWeek);
        Assert.Equal(new DateTime(2026, 11, 1, 8, 0, 0), r[1].FechaHora);
        Assert.Equal(DayOfWeek.Sunday, r[1].FechaHora!.Value.DayOfWeek);
    }

    // ---- HU-13: una sola palabra ----

    [Fact]
    public void Perro_ofrece_opciones_al_toque()
    {
        var r = ParserLocal.InterpretarVarios("perro", Ahora).Single();

        Assert.NotNull(r.Opciones);
        Assert.Equal(["Vacuna", "Baño", "Veterinario", "Otro"], r.Opciones!.Select(o => o.Etiqueta));
    }

    [Fact]
    public void Elegir_vacuna_pregunta_el_dia_y_al_responder_se_guarda()
    {
        var perro = ParserLocal.Interpretar("perro", Ahora);
        var vacuna = perro.Opciones!.Single(o => o.Etiqueta == "Vacuna");

        var conOpcion = ParserLocal.ConOpcion(perro, vacuna, Ahora);
        Assert.Equal("Llevar al perro a su vacuna", conOpcion.Titulo);
        Assert.Equal([DatoFaltante.Dia], conOpcion.Faltan);
        Assert.Equal("¿Qué día?", conOpcion.Pregunta);
        Assert.Equal("perro", conOpcion.Dijiste);

        var completo = ParserLocal.Completar(conOpcion, "el 5 de noviembre a las 3 pm", Ahora);
        Assert.True(completo.Completo);
        Assert.Equal("Llevar al perro a su vacuna", completo.Titulo);
        Assert.Equal(new DateTime(2026, 11, 5, 15, 0, 0), completo.FechaHora);
        Assert.Equal(Categoria.Salud, completo.Categoria);
    }

    [Fact]
    public void Elegir_otro_deja_la_pregunta_para_decirlo_o_escribirlo()
    {
        var perro = ParserLocal.Interpretar("perro", Ahora);
        var otro = perro.Opciones!.Single(o => o.EsOtro);

        Assert.Same(perro, ParserLocal.ConOpcion(perro, otro, Ahora));

        var respuesta = ParserLocal.Completar(perro, "bañar al perro mañana", Ahora);
        Assert.True(respuesta.Completo);
        Assert.Equal("Bañar al perro", respuesta.Titulo);
        Assert.Equal(new DateTime(2026, 10, 30), respuesta.FechaHora);
    }

    [Fact]
    public void Una_palabra_desconocida_no_tiene_opciones_y_se_pregunta_con_texto_libre()
    {
        var r = ParserLocal.Interpretar("cumpleaños", Ahora);
        Assert.Null(r.Opciones);

        var jarron = ParserLocal.Interpretar("jarrón", Ahora);
        Assert.Contains(DatoFaltante.Accion, jarron.Faltan);
        Assert.Null(jarron.Opciones);
    }

    [Fact]
    public void Si_solo_se_dijo_la_fecha_la_respuesta_dice_que_recordar()
    {
        var soloFecha = ParserLocal.Interpretar("mañana", Ahora);

        var r = ParserLocal.Completar(soloFecha, "pagar la luz", Ahora);

        Assert.True(r.Completo);
        Assert.Equal("Pagar la luz", r.Titulo);
        Assert.Equal(new DateTime(2026, 10, 30), r.FechaHora);
    }

    // ---- HU-14: calendario ----

    [Fact]
    public void Elegir_un_dia_en_el_calendario_completa_el_recordatorio()
    {
        var sinDia = ParserLocal.Interpretar("cumpleaños de Martín", Ahora);

        var r = ParserLocal.ConFecha(sinDia, new DateOnly(2026, 11, 5));

        Assert.True(r.Completo);
        Assert.Null(r.Pregunta);
        Assert.Equal(new DateTime(2026, 11, 5), r.FechaHora);
        Assert.Equal("Cumpleaños de Martín", r.Titulo);
    }

    [Fact]
    public void El_calendario_conserva_la_hora_que_ya_se_dijo()
    {
        var sinDia = ParserLocal.Interpretar("tomar pastilla en la mañana", Ahora);

        var r = ParserLocal.ConFecha(sinDia, new DateOnly(2026, 11, 2));

        Assert.Equal(new DateTime(2026, 11, 2, 8, 0, 0), r.FechaHora);
        Assert.False(r.SinHora);
    }
}
