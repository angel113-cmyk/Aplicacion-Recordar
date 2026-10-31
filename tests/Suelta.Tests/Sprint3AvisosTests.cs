using Suelta.Core.Avisos;
using Suelta.Core.Datos;
using Suelta.Core.Dominio;
using Suelta.Core.Formato;
using Suelta.Core.Parser;
using Suelta.Core.Servicios;

namespace Suelta.Tests;

public class PlanDeAvisosTests
{
    // Jueves 29 de octubre de 2026, 10:00.
    private static readonly DateTime Ahora = new(2026, 10, 29, 10, 0, 0);

    private static Recordatorio R(DateTime? fh, bool sinHora = false, Categoria cat = Categoria.Otro) => new()
    {
        Id = Guid.NewGuid(),
        Titulo = "Pagar el recibo",
        FechaHora = fh,
        SinHora = sinHora,
        Categoria = cat,
    };

    [Fact]
    public void Con_hora_avisa_justo_a_esa_hora()
    {
        var plan = PlanDeAvisos.Para(R(new DateTime(2026, 10, 30, 17, 30, 0)), Ahora);

        var aviso = Assert.Single(plan);
        Assert.Equal(TipoAviso.Principal, aviso.Tipo);
        Assert.Equal(new DateTime(2026, 10, 30, 17, 30, 0), aviso.Cuando);
        Assert.Equal("Pagar el recibo", aviso.Titulo);
    }

    [Fact]
    public void Sin_hora_avisa_a_las_9_de_la_manana_del_dia_indicado()
    {
        var plan = PlanDeAvisos.Para(R(new DateTime(2026, 10, 30), sinHora: true), Ahora);

        Assert.Equal(new DateTime(2026, 10, 30, 9, 0, 0), Assert.Single(plan).Cuando);
    }

    [Fact]
    public void Lo_que_ya_paso_no_avisa()
    {
        Assert.Empty(PlanDeAvisos.Para(R(new DateTime(2026, 10, 29, 8, 0, 0)), Ahora));
        Assert.Empty(PlanDeAvisos.Para(R(new DateTime(2026, 10, 29), sinHora: true), Ahora));
    }

    [Fact]
    public void Un_cumpleanos_programa_dos_avisos_3_dias_antes_y_el_mismo_dia_a_las_9()
    {
        var plan = PlanDeAvisos.Para(R(new DateTime(2026, 11, 5), sinHora: true, cat: Categoria.Cumpleanos), Ahora);

        Assert.Equal(2, plan.Count);

        var antes = plan.Single(a => a.Tipo == TipoAviso.Anticipado);
        Assert.Equal(new DateTime(2026, 11, 2, 9, 0, 0), antes.Cuando);
        Assert.Equal("En 3 días: pagar el recibo", antes.Titulo);
        Assert.Equal("Es el jueves 5 de noviembre.", antes.Texto);

        var mismoDia = plan.Single(a => a.Tipo == TipoAviso.Principal);
        Assert.Equal(new DateTime(2026, 11, 5, 9, 0, 0), mismoDia.Cuando);
    }

    [Fact]
    public void El_aviso_anticipado_dice_la_fecha_como_el_prototipo()
    {
        var cumple = new Recordatorio
        {
            Id = Guid.NewGuid(),
            Titulo = "Cumpleaños de Martín",
            Categoria = Categoria.Cumpleanos,
            FechaHora = new DateTime(2026, 11, 5),
            SinHora = true,
        };

        var antes = PlanDeAvisos.Para(cumple, Ahora).Single(a => a.Tipo == TipoAviso.Anticipado);

        Assert.Equal("En 3 días: cumpleaños de Martín", antes.Titulo);
        Assert.Equal("Es el jueves 5 de noviembre.", antes.Texto);
    }

    [Fact]
    public void Si_el_aviso_anticipado_ya_paso_solo_queda_el_del_dia()
    {
        var plan = PlanDeAvisos.Para(R(new DateTime(2026, 10, 31), sinHora: true, cat: Categoria.Cumpleanos), Ahora);

        Assert.Equal(TipoAviso.Principal, Assert.Single(plan).Tipo);
    }

    [Fact]
    public void Hechos_y_eliminados_no_avisan()
    {
        var hecho = R(new DateTime(2026, 10, 30, 9, 0, 0));
        hecho.Hecho = true;
        var eliminado = R(new DateTime(2026, 10, 30, 9, 0, 0));
        eliminado.EliminadoEn = Ahora;

        Assert.Empty(PlanDeAvisos.Para(hecho, Ahora));
        Assert.Empty(PlanDeAvisos.Para(eliminado, Ahora));
    }

    [Fact]
    public void Un_aviso_pospuesto_se_suma_sin_cambiar_la_fecha_original()
    {
        var r = R(new DateTime(2026, 10, 29, 9, 0, 0));
        r.PospuestoHasta = new DateTime(2026, 10, 29, 11, 0, 0);

        var aviso = Assert.Single(PlanDeAvisos.Para(r, Ahora));

        Assert.Equal(TipoAviso.Pospuesto, aviso.Tipo);
        Assert.Equal(new DateTime(2026, 10, 29, 11, 0, 0), aviso.Cuando);
        Assert.Equal(new DateTime(2026, 10, 29, 9, 0, 0), r.FechaHora);
    }

    [Fact]
    public void Los_numeros_de_notificacion_son_distintos_por_tipo_y_positivos()
    {
        var id = Guid.NewGuid();
        var numeros = IdsAviso.De(id).ToList();

        Assert.Equal(3, numeros.Distinct().Count());
        Assert.All(numeros, n => Assert.True(n >= 0));
        Assert.Equal(numeros, IdsAviso.De(id));
    }

    [Fact]
    public void Una_hora_de_lima_se_convierte_a_milisegundos_unix_con_5_horas_de_diferencia()
    {
        var lima = new DateTime(2026, 10, 30, 9, 0, 0);

        var esperado = new DateTimeOffset(2026, 10, 30, 14, 0, 0, TimeSpan.Zero).ToUnixTimeMilliseconds();

        Assert.Equal(esperado, ZonaLima.AMilisegundosUnix(lima));
    }

    [Fact]
    public void Fecha_larga_en_minuscula()
    {
        Assert.Equal("jueves 5 de noviembre", FormatoFechas.FechaLarga(new DateTime(2026, 11, 5)));
    }
}

/// <summary>Anota lo que se programa y se cancela, en lugar de usar las alarmas de un teléfono.</summary>
internal sealed class ProgramadorFalso : IProgramadorAvisos
{
    public List<AvisoPlan> Programados { get; } = [];
    public List<Guid> Cancelados { get; } = [];

    public void Programar(AvisoPlan aviso) => Programados.Add(aviso);

    public void Cancelar(Guid recordatorioId)
    {
        Cancelados.Add(recordatorioId);
        Programados.RemoveAll(a => a.RecordatorioId == recordatorioId);
    }
}

public sealed class ServicioAvisosTests : IAsyncDisposable
{
    private static readonly DateTime Ahora = new(2026, 10, 29, 10, 0, 0);
    private readonly string _ruta = Path.Combine(Path.GetTempPath(), $"suelta-s3-{Guid.NewGuid():N}.db3");
    private readonly RecordatorioRepositorio _repo;
    private readonly ProgramadorFalso _programador = new();
    private readonly ServicioRecordatorios _servicio;

    public ServicioAvisosTests()
    {
        _repo = new RecordatorioRepositorio(_ruta);
        _servicio = new ServicioRecordatorios(_repo, _programador);
    }

    private async Task<Recordatorio> Guardar(string frase) =>
        await _servicio.GuardarAsync(ParserLocal.Interpretar(frase, Ahora), Ahora);

    [Fact]
    public async Task Al_guardar_con_hora_se_programa_un_aviso_a_esa_hora()
    {
        await Guardar("mañana pagar el recibo a las 5 pm");

        var aviso = Assert.Single(_programador.Programados);
        Assert.Equal(new DateTime(2026, 10, 30, 17, 0, 0), aviso.Cuando);
    }

    [Fact]
    public async Task Al_guardar_sin_hora_se_programa_a_las_9()
    {
        await Guardar("mañana pagar el recibo");

        Assert.Equal(new DateTime(2026, 10, 30, 9, 0, 0), Assert.Single(_programador.Programados).Cuando);
    }

    [Fact]
    public async Task Al_guardar_un_cumpleanos_se_programan_dos_avisos()
    {
        await Guardar("5 de noviembre cumpleaños de Martín");

        Assert.Equal(2, _programador.Programados.Count);
    }

    [Fact]
    public async Task Marcar_hecho_cancela_el_aviso()
    {
        var r = await Guardar("mañana pagar el recibo");

        await _servicio.MarcarHechoAsync(r, Ahora);

        Assert.Empty(_programador.Programados);
        Assert.Contains(r.Id, _programador.Cancelados);
    }

    [Fact]
    public async Task Marcar_hecho_uno_que_se_repite_programa_el_siguiente()
    {
        var r = await Guardar("pagar la luz cada mes el día 30");

        await _servicio.MarcarHechoAsync(r, Ahora);

        var aviso = Assert.Single(_programador.Programados);
        Assert.Equal(new DateTime(2026, 11, 30, 9, 0, 0), aviso.Cuando);
    }

    [Fact]
    public async Task Eliminar_cancela_los_avisos_y_deshacer_varios_cancela_todos()
    {
        var a = await Guardar("mañana pagar el recibo");
        var b = await Guardar("mañana comprar pan");

        await _servicio.EliminarAsync([a, b], Ahora);

        Assert.Empty(_programador.Programados);
        Assert.Contains(a.Id, _programador.Cancelados);
        Assert.Contains(b.Id, _programador.Cancelados);
    }

    [Fact]
    public async Task Editar_reprograma_el_aviso_a_la_nueva_fecha()
    {
        var r = await Guardar("mañana pagar el recibo");

        r.FechaHora = new DateTime(2026, 11, 2, 8, 0, 0);
        r.SinHora = false;
        await _servicio.ActualizarAsync(r, Ahora);

        Assert.Equal(new DateTime(2026, 11, 2, 8, 0, 0), Assert.Single(_programador.Programados).Cuando);
    }

    [Fact]
    public async Task En_1_hora_programa_un_nuevo_aviso_y_no_cambia_la_fecha_del_recordatorio()
    {
        var r = await Guardar("hoy pagar el recibo a las 9 am");
        var original = r.FechaHora;
        var ahoraAviso = new DateTime(2026, 10, 29, 9, 0, 0);

        await _servicio.PosponerUnaHoraAsync(r, ahoraAviso);

        var aviso = Assert.Single(_programador.Programados);
        Assert.Equal(TipoAviso.Pospuesto, aviso.Tipo);
        Assert.Equal(new DateTime(2026, 10, 29, 10, 0, 0), aviso.Cuando);
        Assert.Equal(original, (await _servicio.ObtenerAsync(r.Id))!.FechaHora);
    }

    [Fact]
    public async Task Manana_programa_el_aviso_para_el_dia_siguiente_a_la_misma_hora()
    {
        var r = await Guardar("mañana pagar el recibo a las 5 pm");
        var ahoraAviso = new DateTime(2026, 10, 30, 17, 0, 0);

        await _servicio.PosponerHastaMananaAsync(r, ahoraAviso);

        var aviso = Assert.Single(_programador.Programados);
        Assert.Equal(new DateTime(2026, 10, 31, 17, 0, 0), aviso.Cuando);
    }

    [Fact]
    public async Task Marcar_hecho_un_aviso_pospuesto_lo_cancela()
    {
        var r = await Guardar("mañana pagar el recibo a las 5 pm");
        await _servicio.PosponerUnaHoraAsync(r, new DateTime(2026, 10, 30, 17, 0, 0));

        await _servicio.MarcarHechoAsync(r, new DateTime(2026, 10, 30, 17, 5, 0));

        Assert.Empty(_programador.Programados);
        Assert.Null((await _servicio.ObtenerAsync(r.Id))!.PospuestoHasta);
    }

    [Fact]
    public async Task Despues_de_reiniciar_se_reprograman_solo_los_avisos_que_siguen_por_venir()
    {
        await Guardar("mañana pagar el recibo a las 5 pm");
        await Guardar("hoy comprar pan a las 11 am");
        await Guardar("5 de noviembre cumpleaños de Martín");
        _programador.Programados.Clear();

        // El teléfono estuvo apagado hasta las 12 del día siguiente: se perdió el aviso de "hoy 11 am".
        var alEncender = new DateTime(2026, 10, 30, 12, 0, 0);
        await _servicio.ReprogramarTodosAsync(alEncender);

        Assert.Equal(3, _programador.Programados.Count);
        Assert.DoesNotContain(_programador.Programados, a => a.Titulo == "Comprar pan");
    }

    [Fact]
    public async Task El_recordatorio_vencido_sigue_en_la_lista_como_se_te_paso()
    {
        await Guardar("hoy comprar pan a las 11 am");

        var alEncender = new DateTime(2026, 10, 30, 12, 0, 0);
        var r = Assert.Single(await _servicio.ListarAsync());

        Assert.Equal(Suelta.Core.Lista.GrupoFecha.SeTePaso, Suelta.Core.Lista.AgrupadorLista.Clasificar(r, alEncender));
    }

    [Fact]
    public async Task Un_recordatorio_se_puede_obtener_por_su_id_y_uno_eliminado_no()
    {
        var r = await Guardar("mañana pagar el recibo");
        Assert.NotNull(await _servicio.ObtenerAsync(r.Id));

        await _servicio.EliminarAsync([r], Ahora);

        Assert.Null(await _servicio.ObtenerAsync(r.Id));
    }

    public async ValueTask DisposeAsync()
    {
        await _repo.CerrarAsync();
        SQLitePCL.raw.sqlite3_shutdown();
        try { File.Delete(_ruta); } catch (IOException) { }
    }
}

public class RegistroErroresTests
{
    [Fact]
    public void Guarda_el_tipo_de_error_pero_nunca_el_mensaje_con_lo_que_dijo_la_persona()
    {
        var ruta = Path.Combine(Path.GetTempPath(), $"suelta-log-{Guid.NewGuid():N}.log");
        var registro = new RegistroErrores(ruta);

        try
        {
            try
            {
                throw new InvalidOperationException("no se pudo guardar: cumpleaños de Martín el 5 de noviembre");
            }
            catch (Exception e)
            {
                registro.Error("Guardar", e);
            }

            var contenido = registro.Leer();

            Assert.Contains("InvalidOperationException", contenido);
            Assert.Contains("[Guardar]", contenido);
            Assert.DoesNotContain("Martín", contenido);
            Assert.DoesNotContain("noviembre", contenido);
        }
        finally
        {
            File.Delete(ruta);
        }
    }

    [Fact]
    public void Un_registro_en_una_ruta_imposible_no_provoca_otro_fallo()
    {
        var registro = new RegistroErrores(Path.Combine(Path.GetTempPath(), $"no-existe-{Guid.NewGuid():N}", "x.log"));

        registro.Error("Prueba", new Exception("x"));

        Assert.Equal("", registro.Leer());
    }
}
