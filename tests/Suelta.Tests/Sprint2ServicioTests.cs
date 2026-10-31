using Suelta.Core.Datos;
using Suelta.Core.Dominio;
using Suelta.Core.Lista;
using Suelta.Core.Parser;
using Suelta.Core.Servicios;

namespace Suelta.Tests;

public class RepeticionesTests
{
    private static readonly DateTime Ahora = new(2026, 10, 29, 10, 0, 0);

    private static Recordatorio R(Repeticion rep, DateTime fh, bool sinHora = false, int? ancla = null) =>
        new() { Repeticion = rep, FechaHora = fh, SinHora = sinHora, AnclaDia = ancla };

    [Fact]
    public void Diaria_pasa_al_dia_siguiente()
    {
        var r = R(Repeticion.Diaria, new DateTime(2026, 10, 29, 18, 0, 0));

        Assert.Equal(new DateTime(2026, 10, 30, 18, 0, 0), Repeticiones.Siguiente(r, Ahora));
    }

    [Fact]
    public void Semanal_pasa_a_la_semana_siguiente()
    {
        var r = R(Repeticion.Semanal, new DateTime(2026, 10, 31, 9, 0, 0));

        Assert.Equal(new DateTime(2026, 11, 7, 9, 0, 0), Repeticiones.Siguiente(r, Ahora));
    }

    [Fact]
    public void Mensual_dia_30_no_se_corre_despues_de_febrero()
    {
        var r = R(Repeticion.Mensual, new DateTime(2027, 1, 30), sinHora: true, ancla: 30);
        var ahora = new DateTime(2027, 1, 30, 12, 0, 0);

        var febrero = Repeticiones.Siguiente(r, ahora)!.Value;
        Assert.Equal(new DateTime(2027, 2, 28), febrero);

        r.FechaHora = febrero;
        Assert.Equal(new DateTime(2027, 3, 30), Repeticiones.Siguiente(r, febrero.AddHours(12)));
    }

    [Fact]
    public void Anual_pasa_al_ano_siguiente()
    {
        var r = R(Repeticion.Anual, new DateTime(2026, 11, 5), sinHora: true);

        Assert.Equal(new DateTime(2027, 11, 5), Repeticiones.Siguiente(r, Ahora));
    }

    [Fact]
    public void Una_diaria_atrasada_salta_a_la_proxima_vez_futura()
    {
        var r = R(Repeticion.Diaria, new DateTime(2026, 10, 20, 8, 0, 0));

        Assert.Equal(new DateTime(2026, 10, 30, 8, 0, 0), Repeticiones.Siguiente(r, Ahora));
    }

    [Fact]
    public void Sin_hora_la_siguiente_es_un_dia_posterior_a_hoy()
    {
        var r = R(Repeticion.Diaria, new DateTime(2026, 10, 29), sinHora: true);

        Assert.Equal(new DateTime(2026, 10, 30), Repeticiones.Siguiente(r, Ahora));
    }

    [Fact]
    public void Lo_que_no_se_repite_no_tiene_siguiente()
    {
        Assert.Null(Repeticiones.Siguiente(R(Repeticion.Ninguna, Ahora), Ahora));
    }
}

public class ResumenYCategoriasTests
{
    private static readonly DateTime Ahora = new(2026, 10, 29, 10, 0, 0);

    private static Recordatorio R(string titulo, Categoria cat, DateTime? fh, Repeticion rep = Repeticion.Ninguna, bool hecho = false) =>
        new() { Id = Guid.NewGuid(), Titulo = titulo, Categoria = cat, FechaHora = fh, Repeticion = rep, Hecho = hecho, CreadoEn = Ahora };

    // La misma lista del prototipo: 1 hoy, 5 en 7 días, 2 se repiten solos.
    private static List<Recordatorio> Prototipo() =>
    [
        R("auto", Categoria.Auto, new DateTime(2026, 10, 29, 18, 0, 0)),
        R("luz", Categoria.Pagos, new DateTime(2026, 10, 30), Repeticion.Mensual),
        R("perro", Categoria.Salud, new DateTime(2026, 11, 5, 15, 0, 0)),
        R("cafe", Categoria.Otro, new DateTime(2026, 11, 5, 23, 0, 0)),
        R("cumple", Categoria.Cumpleanos, new DateTime(2026, 11, 5), Repeticion.Anual),
    ];

    [Fact]
    public void El_resumen_coincide_con_el_prototipo()
    {
        var resumen = ResumenCarga.De(Prototipo(), Ahora);

        Assert.Equal(new ResumenCarga(1, 5, 2), resumen);
    }

    [Fact]
    public void El_resumen_ignora_hechos_y_eliminados()
    {
        var lista = Prototipo();
        lista[0].Hecho = true;
        lista[1].EliminadoEn = Ahora;

        var resumen = ResumenCarga.De(lista, Ahora);

        Assert.Equal(0, resumen.Hoy);
        Assert.Equal(3, resumen.ProximosSieteDias);
        Assert.Equal(1, resumen.SeRepitenSolos);
    }

    [Fact]
    public void Una_lista_vacia_da_un_resumen_vacio()
    {
        Assert.True(ResumenCarga.De([], Ahora).EstaVacio);
    }

    [Fact]
    public void Por_categoria_solo_aparecen_las_que_tienen_recordatorios_con_su_cantidad()
    {
        var lista = Prototipo();
        lista.Add(R("pastilla", Categoria.Salud, new DateTime(2026, 11, 6, 8, 0, 0)));
        lista.Add(R("dentista", Categoria.Salud, new DateTime(2026, 11, 9, 16, 0, 0)));

        var conteo = AgrupadorCategorias.Contar(lista);

        Assert.Equal(Categoria.Salud, conteo[0].Info.Categoria);
        Assert.Equal(3, conteo[0].Cantidad);
        Assert.DoesNotContain(conteo, c => c.Info.Categoria == Categoria.Compras);
        Assert.Equal(5, conteo.Count);
    }

    [Fact]
    public void Filtrar_por_salud_ordena_por_fecha()
    {
        var lista = Prototipo();
        lista.Add(R("dentista", Categoria.Salud, new DateTime(2026, 11, 9, 16, 0, 0)));
        lista.Add(R("pastilla", Categoria.Salud, new DateTime(2026, 11, 6, 8, 0, 0)));

        var salud = AgrupadorCategorias.Filtrar(lista, Categoria.Salud);

        Assert.Equal(["perro", "pastilla", "dentista"], salud.Select(r => r.Titulo));
        Assert.Equal(lista.Count - 0, AgrupadorCategorias.Filtrar(lista, null).Count);
    }
}

public sealed class ServicioSprint2Tests : IAsyncDisposable
{
    private static readonly DateTime Ahora = new(2026, 10, 29, 10, 0, 0);
    private readonly string _ruta = Path.Combine(Path.GetTempPath(), $"suelta-s2-{Guid.NewGuid():N}.db3");
    private readonly RecordatorioRepositorio _repo;
    private readonly ServicioRecordatorios _servicio;

    public ServicioSprint2Tests()
    {
        _repo = new RecordatorioRepositorio(_ruta);
        _servicio = new ServicioRecordatorios(_repo);
    }

    private async Task<Recordatorio> Guardar(string frase) =>
        await _servicio.GuardarAsync(ParserLocal.Interpretar(frase, Ahora), Ahora);

    [Fact]
    public async Task Guardar_varios_guarda_los_dos_de_una_frase_larga()
    {
        var resultados = ParserLocal.InterpretarVarios(
            "El 5 de noviembre es el cumpleaños de Martín, cómprale un regalo de 20 soles el 3 en la tarde", Ahora);

        var guardados = await _servicio.GuardarVariosAsync(resultados, Ahora);

        Assert.Equal(2, guardados.Count);
        Assert.Equal(2, (await _servicio.ListarAsync()).Count);
    }

    [Fact]
    public async Task Guardar_varios_no_guarda_nada_si_a_uno_le_falta_un_dato()
    {
        var resultados = ParserLocal.InterpretarVarios("cumpleaños de Martín, comprar pan mañana", Ahora);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _servicio.GuardarVariosAsync(resultados, Ahora));
        Assert.Empty(await _servicio.ListarAsync());
    }

    [Fact]
    public async Task Marcar_hecho_pasa_a_hechos_y_se_puede_devolver()
    {
        var r = await Guardar("mañana pagar el recibo");

        await _servicio.MarcarHechoAsync(r, Ahora);
        var guardado = Assert.Single(await _servicio.ListarAsync());
        Assert.True(guardado.Hecho);
        Assert.Equal(GrupoFecha.Hechos, AgrupadorLista.Clasificar(guardado, Ahora));

        await _servicio.DesmarcarHechoAsync(guardado, Ahora);
        Assert.False(Assert.Single(await _servicio.ListarAsync()).Hecho);
    }

    [Fact]
    public async Task Marcar_hecho_un_recordatorio_que_se_repite_pasa_a_su_siguiente_fecha()
    {
        var r = await Guardar("pagar la luz cada mes el día 30");

        await _servicio.MarcarHechoAsync(r, Ahora);

        var guardado = Assert.Single(await _servicio.ListarAsync());
        Assert.False(guardado.Hecho);
        Assert.Equal(new DateTime(2026, 11, 30), guardado.FechaHora);
        Assert.Equal(30, guardado.AnclaDia);
    }

    [Fact]
    public async Task Eliminar_lo_quita_de_la_lista_y_sirve_para_deshacer_varios()
    {
        var a = await Guardar("mañana pagar el recibo");
        var b = await Guardar("mañana comprar pan");

        await _servicio.EliminarAsync([a, b], Ahora);

        Assert.Empty(await _servicio.ListarAsync());
    }

    [Fact]
    public async Task Editar_cambia_los_datos_y_no_deja_el_titulo_vacio()
    {
        var r = await Guardar("mañana pagar el recibo");

        r.Titulo = "  Pagar el agua ";
        r.FechaHora = new DateTime(2026, 11, 2, 9, 0, 0);
        r.Repeticion = Repeticion.Mensual;
        r.Categoria = Categoria.Otro;
        await _servicio.ActualizarAsync(r, Ahora);

        var guardado = Assert.Single(await _servicio.ListarAsync());
        Assert.Equal("Pagar el agua", guardado.Titulo);
        Assert.Equal(new DateTime(2026, 11, 2, 9, 0, 0), guardado.FechaHora);
        Assert.Equal(Repeticion.Mensual, guardado.Repeticion);
        Assert.Equal(2, guardado.AnclaDia);

        guardado.Titulo = "   ";
        await Assert.ThrowsAsync<ArgumentException>(() => _servicio.ActualizarAsync(guardado, Ahora));
    }

    public async ValueTask DisposeAsync()
    {
        await _repo.CerrarAsync();
        SQLitePCL.raw.sqlite3_shutdown();
        try { File.Delete(_ruta); } catch (IOException) { }
    }
}
