using Suelta.Core.Dominio;
using Suelta.Core.Parser;

namespace Suelta.Tests;

public class ParserLocalTests
{
    // Jueves 29 de octubre de 2026, 10:00 (el día del prototipo).
    private static readonly DateTime Ahora = new(2026, 10, 29, 10, 0, 0);

    private static ResultadoParser P(string frase) => ParserLocal.Interpretar(frase, Ahora);

    [Theory]
    [InlineData("5 de noviembre cumpleaños de Martín")]
    [InlineData("El 5 de noviembre es el cumpleaños de Martín")]
    [InlineData("cumpleaños de Martín el 5 de noviembre")]
    public void Cumpleanos_se_guarda_anual_el_5_de_noviembre(string frase)
    {
        var r = P(frase);

        Assert.True(r.Completo);
        Assert.Equal("Cumpleaños de Martín", r.Titulo);
        Assert.Equal(new DateTime(2026, 11, 5), r.FechaHora);
        Assert.True(r.SinHora);
        Assert.Equal(Repeticion.Anual, r.Repeticion);
        Assert.Equal(Categoria.Cumpleanos, r.Categoria);
    }

    [Fact]
    public void Cumpleanos_sin_dia_pregunta_el_dia_y_no_inventa_fecha()
    {
        var r = P("cumpleaños de Martín");

        Assert.False(r.Completo);
        Assert.Equal([DatoFaltante.Dia], r.Faltan);
        Assert.Null(r.FechaHora);
        Assert.Equal("¿Qué día cumple Martín?", r.Pregunta);
        Assert.Equal("Cumpleaños de Martín", r.Titulo);
        Assert.Equal(Repeticion.Anual, r.Repeticion);
    }

    [Fact]
    public void Completar_con_la_respuesta_guarda_el_cumpleanos_y_conserva_lo_dicho()
    {
        var r = ParserLocal.Completar(P("cumpleaños de Martín"), "el 5 de noviembre", Ahora);

        Assert.True(r.Completo);
        Assert.Equal("Cumpleaños de Martín", r.Titulo);
        Assert.Equal(new DateTime(2026, 11, 5), r.FechaHora);
        Assert.Equal("cumpleaños de Martín", r.Dijiste);
    }

    [Fact]
    public void Cumpleanos_que_ya_paso_este_ano_va_al_ano_siguiente()
    {
        var r = P("1 de octubre cumpleaños de Ana");

        Assert.Equal(new DateTime(2027, 10, 1), r.FechaHora);
    }

    [Fact]
    public void Una_sola_palabra_no_se_guarda_y_pregunta_que_recordar()
    {
        var r = P("perro");

        Assert.False(r.Completo);
        Assert.Equal([DatoFaltante.Accion, DatoFaltante.Dia], r.Faltan);
        Assert.Equal("¿Qué quieres recordar del perro?", r.Pregunta);
    }

    [Fact]
    public void Frase_vacia_pide_que_recordar()
    {
        var r = P("   ");

        Assert.Contains(DatoFaltante.Accion, r.Faltan);
        Assert.Equal("¿Qué quieres recordar?", r.Pregunta);
    }

    [Fact]
    public void El_dia_15_es_el_proximo_15()
    {
        var r = P("el 15 comprar pan");

        Assert.True(r.Completo);
        Assert.Equal("Comprar pan", r.Titulo);
        Assert.Equal(new DateTime(2026, 11, 15), r.FechaHora);
        Assert.Equal(Categoria.Compras, r.Categoria);
    }

    [Fact]
    public void El_dia_de_hoy_cuenta_si_es_el_mismo_numero()
    {
        var r = P("el día 29 pagar el agua");

        Assert.Equal(new DateTime(2026, 10, 29), r.FechaHora);
    }

    [Fact]
    public void Manana_es_el_dia_siguiente()
    {
        var r = P("mañana pagar el recibo");

        Assert.True(r.Completo);
        Assert.Equal("Pagar el recibo", r.Titulo);
        Assert.Equal(new DateTime(2026, 10, 30), r.FechaHora);
        Assert.True(r.SinHora);
        Assert.Equal(Categoria.Pagos, r.Categoria);
    }

    [Fact]
    public void Manana_dia_se_distingue_de_en_la_manana_momento()
    {
        var r = P("tomar pastilla mañana en la mañana");

        Assert.Equal("Tomar pastilla", r.Titulo);
        Assert.Equal(new DateTime(2026, 10, 30, 8, 0, 0), r.FechaHora);
        Assert.False(r.SinHora);
        Assert.Equal(Categoria.Salud, r.Categoria);
    }

    [Fact]
    public void En_la_manana_sin_dia_no_inventa_el_dia()
    {
        var r = P("tomar pastilla en la mañana");

        Assert.Equal([DatoFaltante.Dia], r.Faltan);
        Assert.Null(r.FechaHora);
    }

    [Fact]
    public void Pasado_manana_son_dos_dias_despues()
    {
        var r = P("pasado mañana llamar a mamá");

        Assert.Equal(new DateTime(2026, 10, 31), r.FechaHora);
        Assert.Equal("Llamar a mamá", r.Titulo);
    }

    [Fact]
    public void Hoy_con_hora_pm()
    {
        var r = P("llevar el auto al taller hoy 6 pm");

        Assert.Equal("Llevar el auto al taller", r.Titulo);
        Assert.Equal(new DateTime(2026, 10, 29, 18, 0, 0), r.FechaHora);
        Assert.Equal(Categoria.Auto, r.Categoria);
    }

    [Fact]
    public void Esta_tarde_son_las_4_pm_de_hoy()
    {
        var r = P("esta tarde sacar al perro");

        Assert.Equal(new DateTime(2026, 10, 29, 16, 0, 0), r.FechaHora);
    }

    [Theory]
    [InlineData("mañana a las 9 de la noche reunión", 21, 0)]
    [InlineData("mañana a las 9:30 reunión", 9, 30)]
    [InlineData("mañana a las 3 reunión", 15, 0)]
    [InlineData("mañana a las 12 de la noche reunión", 0, 0)]
    [InlineData("mañana 7:15 am reunión", 7, 15)]
    [InlineData("mañana al mediodía reunión", 12, 0)]
    public void Horas_en_distintas_formas(string frase, int hora, int minuto)
    {
        var r = P(frase);

        Assert.Equal(new DateTime(2026, 10, 30, hora, minuto, 0), r.FechaHora);
        Assert.Equal("Reunión", r.Titulo);
    }

    [Fact]
    public void Frases_con_comas_quedan_limpias()
    {
        var r = P("mañana, pagar el recibo, a las 5");

        Assert.Equal("Pagar el recibo", r.Titulo);
        Assert.Equal(new DateTime(2026, 10, 30, 17, 0, 0), r.FechaHora);
    }

    [Fact]
    public void Cada_sabado_a_las_9_es_semanal()
    {
        var r = P("mercado cada sábado a las 9");

        Assert.True(r.Completo);
        Assert.Equal("Mercado", r.Titulo);
        Assert.Equal(Repeticion.Semanal, r.Repeticion);
        Assert.Equal(new DateTime(2026, 10, 31, 9, 0, 0), r.FechaHora);
        Assert.Equal(Categoria.Compras, r.Categoria);
    }

    [Fact]
    public void Cada_mes_el_dia_30_es_mensual()
    {
        var r = P("pagar la luz cada mes el día 30");

        Assert.True(r.Completo);
        Assert.Equal("Pagar la luz", r.Titulo);
        Assert.Equal(Repeticion.Mensual, r.Repeticion);
        Assert.Equal(new DateTime(2026, 10, 30), r.FechaHora);
        Assert.Equal(Categoria.Pagos, r.Categoria);
    }

    [Theory]
    [InlineData("tomar agua cada día a las 8", Repeticion.Diaria)]
    [InlineData("tomar agua todos los días a las 8", Repeticion.Diaria)]
    [InlineData("regar las plantas cada semana el lunes", Repeticion.Semanal)]
    [InlineData("pagar el seguro cada año el 12 de marzo", Repeticion.Anual)]
    public void Repeticiones_con_palabras_propias(string frase, Repeticion esperada)
    {
        var r = P(frase);

        Assert.Equal(esperada, r.Repeticion);
        Assert.True(r.Completo);
    }

    [Fact]
    public void Repeticion_diaria_con_hora_pasada_empieza_manana()
    {
        var r = P("tomar agua cada día a las 8");

        Assert.Equal(new DateTime(2026, 10, 30, 8, 0, 0), r.FechaHora);
    }

    [Fact]
    public void Cada_semana_sin_dia_pregunta_el_dia()
    {
        var r = P("comprar pan cada semana");

        Assert.Equal([DatoFaltante.Dia], r.Faltan);
    }

    [Fact]
    public void Monto_en_soles_se_muestra_en_el_titulo()
    {
        var r = P("comprar regalo de 20 soles el 3 de noviembre");

        Assert.Equal(20m, r.Monto);
        Assert.Equal("Comprar regalo (S/ 20)", r.Titulo);
        Assert.Equal(new DateTime(2026, 11, 3), r.FechaHora);
        Assert.Equal(Categoria.Compras, r.Categoria);
    }

    [Fact]
    public void Monto_con_decimales()
    {
        var r = P("mañana pagar 15.50 soles de internet");

        Assert.Equal(15.5m, r.Monto);
        Assert.Contains("S/ 15.5", r.Titulo);
    }

    [Fact]
    public void Dia_de_la_semana_hoy_mismo_va_a_la_semana_siguiente()
    {
        // Hoy es jueves y no se dijo hora: "el jueves" es el próximo.
        var r = P("el jueves ir al banco");

        Assert.Equal(new DateTime(2026, 11, 5), r.FechaHora);
    }

    [Fact]
    public void Dia_de_la_semana_hoy_con_hora_futura_es_hoy()
    {
        var r = P("el jueves a las 5 ir al banco");

        Assert.Equal(new DateTime(2026, 10, 29, 17, 0, 0), r.FechaHora);
    }

    [Theory]
    [InlineData("mañana cita con el dentista", Categoria.Salud)]
    [InlineData("mañana llevar el carro al mecánico", Categoria.Auto)]
    [InlineData("mañana tomar café", Categoria.Otro)]
    public void Categoria_se_asigna_sola(string frase, Categoria esperada)
    {
        Assert.Equal(esperada, P(frase).Categoria);
    }

    [Fact]
    public void Recuerdame_al_inicio_no_entra_en_el_titulo()
    {
        var r = P("recuérdame que mañana pague la cuota");

        Assert.Equal("Pague la cuota", r.Titulo);
        Assert.Equal(new DateTime(2026, 10, 30), r.FechaHora);
    }

    [Fact]
    public void Si_se_dice_solo_una_fecha_se_pregunta_que_recordar()
    {
        var r = P("mañana");

        Assert.Contains(DatoFaltante.Accion, r.Faltan);
        Assert.Equal(new DateTime(2026, 10, 30), r.FechaHora);
    }
}
