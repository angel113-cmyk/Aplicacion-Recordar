using Suelta.Core.Datos;
using Suelta.Core.Dominio;
using Suelta.Core.Parser;
using Suelta.Core.Servicios;

namespace Suelta.Tests;

public sealed class RepositorioTests : IAsyncDisposable
{
    private static readonly DateTime Ahora = new(2026, 10, 29, 10, 0, 0);
    private readonly string _ruta = Path.Combine(Path.GetTempPath(), $"suelta-test-{Guid.NewGuid():N}.db3");
    private readonly RecordatorioRepositorio _repo;

    public RepositorioTests() => _repo = new RecordatorioRepositorio(_ruta);

    [Fact]
    public async Task Guarda_y_lee_un_recordatorio_con_todos_sus_campos()
    {
        var servicio = new ServicioRecordatorios(_repo);
        var resultado = ParserLocal.Interpretar("comprar regalo de 20 soles el 3 de noviembre a las 4 pm", Ahora);

        var guardado = await servicio.GuardarAsync(resultado, Ahora);
        var leidos = await servicio.ListarAsync();

        var leido = Assert.Single(leidos);
        Assert.Equal(guardado.Id, leido.Id);
        Assert.Equal("Comprar regalo (S/ 20)", leido.Titulo);
        Assert.Equal(Categoria.Compras, leido.Categoria);
        Assert.Equal(new DateTime(2026, 11, 3, 16, 0, 0), leido.FechaHora);
        Assert.False(leido.SinHora);
        Assert.Equal(20m, leido.Monto);
        Assert.Equal("comprar regalo de 20 soles el 3 de noviembre a las 4 pm", leido.Dijiste);
        Assert.False(leido.Hecho);
        Assert.Null(leido.EliminadoEn);
    }

    [Fact]
    public async Task Guarda_la_repeticion_y_el_cumpleanos_con_aviso_anticipado()
    {
        var servicio = new ServicioRecordatorios(_repo);
        await servicio.GuardarAsync(ParserLocal.Interpretar("5 de noviembre cumpleaños de Martín", Ahora), Ahora);

        var leido = Assert.Single(await servicio.ListarAsync());

        Assert.Equal(Repeticion.Anual, leido.Repeticion);
        Assert.Equal(Categoria.Cumpleanos, leido.Categoria);
        Assert.Equal(3, leido.DiasAvisoAnticipado);
    }

    [Fact]
    public async Task No_guarda_si_todavia_falta_un_dato()
    {
        var servicio = new ServicioRecordatorios(_repo);
        var incompleto = ParserLocal.Interpretar("cumpleaños de Martín", Ahora);

        await Assert.ThrowsAsync<InvalidOperationException>(() => servicio.GuardarAsync(incompleto, Ahora));
        Assert.Empty(await servicio.ListarAsync());
    }

    [Fact]
    public async Task Los_eliminados_no_se_listan()
    {
        await _repo.InsertarAsync(new Recordatorio { Id = Guid.NewGuid(), Titulo = "visible", CreadoEn = Ahora });
        await _repo.InsertarAsync(new Recordatorio { Id = Guid.NewGuid(), Titulo = "borrado", CreadoEn = Ahora, EliminadoEn = Ahora });

        var leidos = await _repo.ListarAsync();

        Assert.Equal("visible", Assert.Single(leidos).Titulo);
    }

    public async ValueTask DisposeAsync()
    {
        await _repo.CerrarAsync();
        SQLitePCL.raw.sqlite3_shutdown();
        try { File.Delete(_ruta); } catch (IOException) { }
    }
}
