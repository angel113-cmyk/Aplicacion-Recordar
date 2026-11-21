using Suelta.Core.Dominio;
using Suelta.Core.Formato;

namespace Suelta.Tests;

public class ResaltadoFraseTests
{
    private static string[] Resaltados(string frase) =>
        ResaltadoFrase.Dividir(frase).Where(t => t.Resaltado).Select(t => t.Texto).ToArray();

    [Fact]
    public void Juntar_los_trozos_devuelve_la_frase_original_sin_perder_nada()
    {
        const string frase = "Mañana a las 8 pastilla y el sábado reparar el auto, 20 soles";

        var unida = string.Concat(ResaltadoFrase.Dividir(frase).Select(t => t.Texto));

        Assert.Equal(frase, unida);
    }

    [Fact]
    public void Marca_dias_horas_y_montos_conservando_tildes_y_mayusculas()
    {
        var marcados = Resaltados("Mañana a las 8 pastilla y el sábado reparar el auto");

        Assert.Equal(["Mañana", "a las 8", "el sábado"], marcados);
    }

    [Theory]
    [InlineData("pagar la luz 85 soles el viernes", new[] { "85 soles", "el viernes" })]
    [InlineData("el 5 de noviembre cumpleaños de Martín", new[] { "el 5 de noviembre" })]
    [InlineData("tomar agua todos los días a las 7:30 am", new[] { "todos los días", "a las 7:30 am" })]
    [InlineData("pastilla en la mañana", new[] { "en la mañana" })]
    [InlineData("salir los fines de semana", new[] { "fines de semana" })]
    public void Marca_lo_que_la_app_entiende(string frase, string[] esperados)
    {
        Assert.Equal(esperados, Resaltados(frase));
    }

    [Fact]
    public void Una_frase_sin_fechas_no_marca_nada()
    {
        Assert.Empty(Resaltados("comprar zapatillas de Lucciano"));
    }

    [Fact]
    public void El_texto_vacio_o_nulo_no_falla()
    {
        Assert.Empty(ResaltadoFrase.Dividir(""));
        Assert.Empty(ResaltadoFrase.Dividir(null));
    }
}

public class CategoriaInfoTests
{
    [Fact]
    public void Cada_tema_tiene_el_color_del_diseno_y_un_icono()
    {
        Assert.Equal("#E8457A", CategoriaInfo.De(Categoria.Cumpleanos).Color);
        Assert.Equal("#0FA3A3", CategoriaInfo.De(Categoria.Salud).Color);
        Assert.Equal("#1E9E6A", CategoriaInfo.De(Categoria.Compras).Color);
        Assert.Equal("#2B7BE4", CategoriaInfo.De(Categoria.Auto).Color);
        Assert.Equal("#D98A0B", CategoriaInfo.De(Categoria.Pagos).Color);
        Assert.Equal("#8A4FE0", CategoriaInfo.De(Categoria.Citas).Color);
        Assert.Equal("#7A7F9A", CategoriaInfo.De(Categoria.Otro).Color);

        Assert.All(CategoriaInfo.Lista, c => Assert.Matches("^[a-z]+$", c.Icono));
    }
}
