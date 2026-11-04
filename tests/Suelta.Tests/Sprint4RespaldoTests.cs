using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Suelta.Api.Datos;
using Suelta.Core.Datos;
using Suelta.Core.Dominio;
using Suelta.Core.Parser;
using Suelta.Core.Respaldo;
using Suelta.Core.Servicios;
using Microsoft.Extensions.DependencyInjection;

namespace Suelta.Tests;

public class CodigoRecuperacionTests
{
    [Fact]
    public void Genera_16_caracteres_validos_y_distintos_cada_vez()
    {
        var codigos = Enumerable.Range(0, 200).Select(_ => CodigoRecuperacion.Generar()).ToList();

        Assert.All(codigos, c => Assert.Equal(16, c.Length));
        Assert.All(codigos, c => Assert.NotNull(CodigoRecuperacion.Normalizar(c)));
        Assert.Equal(200, codigos.Distinct().Count());
    }

    [Fact]
    public void Se_muestra_en_grupos_de_cuatro_y_se_normaliza_de_vuelta()
    {
        var codigo = CodigoRecuperacion.Generar();

        var mostrado = CodigoRecuperacion.Formatear(codigo);

        Assert.Matches("^[0-9A-Z]{4}(-[0-9A-Z]{4}){3}$", mostrado);
        Assert.Equal(codigo, CodigoRecuperacion.Normalizar(mostrado));
        Assert.Equal(codigo, CodigoRecuperacion.Normalizar(mostrado.ToLowerInvariant().Replace('-', ' ')));
    }

    [Fact]
    public void Perdona_las_confusiones_tipicas_al_escribir()
    {
        Assert.Equal("0123456789ABCDEF", CodigoRecuperacion.Normalizar("O1Z3-456789-ABCDEF".Replace('Z', '2')));
        Assert.Equal("1111111111111111", CodigoRecuperacion.Normalizar("ILil-ILil-ILil-ILil"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("1234")]
    [InlineData("ABCD-EFGH-JKMN-PQRSX")]
    [InlineData("ABCD-EFGH-JKMN-PQR!")]
    [InlineData("UUUU-UUUU-UUUU-UUUU")]
    public void Rechaza_lo_que_no_es_un_codigo(string? texto)
    {
        Assert.Null(CodigoRecuperacion.Normalizar(texto));
    }

    [Fact]
    public void La_huella_es_estable_y_no_contiene_el_codigo()
    {
        var codigo = CodigoRecuperacion.Generar();

        var huella = CodigoRecuperacion.Huella(codigo);

        Assert.Equal(huella, CodigoRecuperacion.Huella(codigo));
        Assert.Equal(64, huella.Length);
        Assert.DoesNotContain(codigo, huella);
    }
}

/// <summary>Levanta la API de verdad (con su base de datos SQLite en un archivo temporal) para cada prueba.</summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _carpeta = Path.Combine(Path.GetTempPath(), $"suelta-api-{Guid.NewGuid():N}");
    private readonly int _porMinuto;

    public ApiFactory(int porMinuto = 1000)
    {
        _porMinuto = porMinuto;
        Directory.CreateDirectory(_carpeta);
    }

    public string RutaBase => Path.Combine(_carpeta, "api.db");

    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.UseSetting("Datos:Ruta", RutaBase);
        builder.UseSetting("Respaldos:Activo", "false");
        builder.UseSetting("Limites:PorMinuto", _porMinuto.ToString());
    }

    public ClienteRespaldo NuevoCliente() => new(CreateClient());

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        try { Directory.Delete(_carpeta, true); } catch (IOException) { }
    }
}

public class ApiRespaldoTests : IDisposable
{
    private static readonly DateTime Ahora = new(2026, 10, 29, 10, 0, 0);
    private readonly ApiFactory _api = new();

    private static RecordatorioDto Dto(string titulo, DateTime actualizadoUtc, Guid? id = null, bool eliminado = false) => new(
        id ?? Guid.NewGuid(), titulo, (int)Categoria.Otro, new DateTime(2026, 11, 5, 9, 0, 0), false, 0, 5, null, titulo, false,
        null, actualizadoUtc, actualizadoUtc, eliminado ? actualizadoUtc : null);

    private async Task<string> NuevaCuenta()
    {
        var (resultado, codigo) = await _api.NuevoCliente().CrearCuentaAsync();
        Assert.Equal(ResultadoRed.Ok, resultado);
        return CodigoRecuperacion.Normalizar(codigo)!;
    }

    [Fact]
    public async Task La_API_esta_viva()
    {
        var respuesta = await _api.CreateClient().GetAsync("/salud");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
    }

    [Fact]
    public async Task Crear_una_cuenta_da_un_codigo_y_el_servidor_solo_guarda_su_huella()
    {
        var (_, mostrado) = await _api.NuevoCliente().CrearCuentaAsync();
        var codigo = CodigoRecuperacion.Normalizar(mostrado)!;

        using var alcance = _api.Services.CreateScope();
        var db = alcance.ServiceProvider.GetRequiredService<ApiDb>();
        var cuenta = await db.Cuentas.SingleAsync();

        Assert.Equal(CodigoRecuperacion.Huella(codigo), cuenta.CodigoHash);
        Assert.DoesNotContain(codigo, cuenta.CodigoHash);
    }

    [Fact]
    public async Task Sin_codigo_o_con_un_codigo_desconocido_no_deja_entrar()
    {
        var cliente = _api.NuevoCliente();
        var vacio = new SincronizarSolicitud(null, []);

        var desconocido = await cliente.SincronizarAsync(CodigoRecuperacion.Generar(), vacio);
        Assert.Equal(ResultadoRed.CodigoInvalido, desconocido.Resultado);

        var sinCodigo = await _api.CreateClient().PostAsJsonAsync("/v1/respaldo/sincronizar", vacio, JsonContexto.Default.SincronizarSolicitud);
        Assert.Equal(HttpStatusCode.Unauthorized, sinCodigo.StatusCode);

        Assert.Equal(ResultadoRed.CodigoInvalido, await cliente.BorrarAsync("no-es-un-codigo"));
    }

    [Fact]
    public async Task Sincronizar_guarda_y_devuelve_lo_nuevo_a_otro_telefono()
    {
        var codigo = await NuevaCuenta();
        var telefonoA = _api.NuevoCliente();
        var telefonoB = _api.NuevoCliente();
        var dto = Dto("Pagar la luz", DateTime.UtcNow.AddMinutes(-5));

        var subida = await telefonoA.SincronizarAsync(codigo, new SincronizarSolicitud(null, [dto]));
        Assert.Equal(ResultadoRed.Ok, subida.Resultado);
        Assert.Empty(subida.Respuesta!.Cambios);

        var bajada = await telefonoB.SincronizarAsync(codigo, new SincronizarSolicitud(null, []));
        var recibido = Assert.Single(bajada.Respuesta!.Cambios);
        Assert.Equal(dto.Id, recibido.Id);
        Assert.Equal("Pagar la luz", recibido.Titulo);
        Assert.Equal(dto.FechaHora, recibido.FechaHora);
    }

    [Fact]
    public async Task Gana_la_ultima_modificacion_y_el_telefono_atrasado_recibe_la_version_nueva()
    {
        var codigo = await NuevaCuenta();
        var cliente = _api.NuevoCliente();
        var id = Guid.NewGuid();
        var ahora = DateTime.UtcNow;

        await cliente.SincronizarAsync(codigo, new SincronizarSolicitud(null, [Dto("Versión nueva", ahora, id)]));

        // Llega una versión más vieja (de un teléfono que estuvo sin conexión): no pisa a la nueva.
        var vieja = await cliente.SincronizarAsync(codigo, new SincronizarSolicitud(null, [Dto("Versión vieja", ahora.AddHours(-1), id)]));

        var devuelto = Assert.Single(vieja.Respuesta!.Cambios);
        Assert.Equal("Versión nueva", devuelto.Titulo);

        var todo = await cliente.SincronizarAsync(codigo, new SincronizarSolicitud(null, []));
        Assert.Equal("Versión nueva", Assert.Single(todo.Respuesta!.Cambios).Titulo);
    }

    [Fact]
    public async Task Las_cuentas_no_ven_los_recordatorios_de_las_otras()
    {
        var codigoA = await NuevaCuenta();
        var codigoB = await NuevaCuenta();
        var cliente = _api.NuevoCliente();

        await cliente.SincronizarAsync(codigoA, new SincronizarSolicitud(null, [Dto("Solo mío", DateTime.UtcNow)]));

        var deB = await cliente.SincronizarAsync(codigoB, new SincronizarSolicitud(null, []));
        Assert.Empty(deB.Respuesta!.Cambios);
    }

    [Fact]
    public async Task Los_eliminados_viajan_para_que_los_otros_telefonos_se_enteren()
    {
        var codigo = await NuevaCuenta();
        var cliente = _api.NuevoCliente();
        var id = Guid.NewGuid();
        var t = DateTime.UtcNow.AddMinutes(-10);

        await cliente.SincronizarAsync(codigo, new SincronizarSolicitud(null, [Dto("Borrar", t, id)]));
        await cliente.SincronizarAsync(codigo, new SincronizarSolicitud(null, [Dto("Borrar", t.AddMinutes(5), id, eliminado: true)]));

        var otro = await cliente.SincronizarAsync(codigo, new SincronizarSolicitud(null, []));
        Assert.NotNull(Assert.Single(otro.Respuesta!.Cambios).EliminadoEnUtc);
    }

    [Fact]
    public async Task Borrar_mis_datos_elimina_la_cuenta_y_todos_sus_recordatorios()
    {
        var codigo = await NuevaCuenta();
        var cliente = _api.NuevoCliente();
        await cliente.SincronizarAsync(codigo, new SincronizarSolicitud(null, [Dto("Uno", DateTime.UtcNow), Dto("Dos", DateTime.UtcNow)]));

        Assert.Equal(ResultadoRed.Ok, await cliente.BorrarAsync(codigo));

        using var alcance = _api.Services.CreateScope();
        var db = alcance.ServiceProvider.GetRequiredService<ApiDb>();
        Assert.Empty(await db.Cuentas.ToListAsync());
        Assert.Empty(await db.Recordatorios.ToListAsync());

        // El código ya no sirve.
        var despues = await cliente.SincronizarAsync(codigo, new SincronizarSolicitud(null, []));
        Assert.Equal(ResultadoRed.CodigoInvalido, despues.Resultado);
    }

    [Fact]
    public async Task Rechaza_titulos_enormes_sin_guardarlos()
    {
        var codigo = await NuevaCuenta();
        var cliente = _api.NuevoCliente();

        await cliente.SincronizarAsync(codigo, new SincronizarSolicitud(null, [Dto(new string('x', 5000), DateTime.UtcNow)]));

        using var alcance = _api.Services.CreateScope();
        Assert.Empty(await alcance.ServiceProvider.GetRequiredService<ApiDb>().Recordatorios.ToListAsync());
    }

    public void Dispose() => _api.Dispose();
}

public class ApiLimitesTests
{
    [Fact]
    public async Task Pasado_el_limite_por_minuto_responde_429()
    {
        using var api = new ApiFactory(porMinuto: 3);
        var http = api.CreateClient();

        var codigos = new List<HttpStatusCode>();
        for (var i = 0; i < 6; i++)
            codigos.Add((await http.GetAsync("/salud")).StatusCode);

        Assert.Equal(HttpStatusCode.OK, codigos[0]);
        Assert.Contains(HttpStatusCode.TooManyRequests, codigos);
    }

    [Fact]
    public async Task Crear_cuentas_tiene_un_limite_mas_estricto()
    {
        using var api = new ApiFactory();
        var cliente = api.NuevoCliente();

        var resultados = new List<ResultadoRed>();
        for (var i = 0; i < 12; i++)
            resultados.Add((await cliente.CrearCuentaAsync()).Resultado);

        Assert.Equal(10, resultados.Count(r => r == ResultadoRed.Ok));
        Assert.Contains(ResultadoRed.LimiteExcedido, resultados);
    }
}

/// <summary>El almacén del teléfono, en memoria.</summary>
internal sealed class AlmacenEnMemoria : IAlmacenRespaldo
{
    public string? Codigo { get; set; }
    public DateTime? CursorLocalUtc { get; set; }
    public DateTime? CursorServidorUtc { get; set; }
}

/// <summary>Un teléfono entero: su propia base de datos, sus avisos y su respaldo, hablando con la API real.</summary>
internal sealed class TelefonoDePrueba : IAsyncDisposable
{
    private readonly string _ruta = Path.Combine(Path.GetTempPath(), $"suelta-tel-{Guid.NewGuid():N}.db3");
    private readonly RecordatorioRepositorio _repo;

    public TelefonoDePrueba(ApiFactory api)
    {
        _repo = new RecordatorioRepositorio(_ruta);
        Programador = new ProgramadorFalso();
        Recordatorios = new ServicioRecordatorios(_repo, Programador);
        Almacen = new AlmacenEnMemoria();
        Respaldo = new ServicioRespaldo(_repo, Recordatorios, api.NuevoCliente(), Almacen);
    }

    public ProgramadorFalso Programador { get; }
    public ServicioRecordatorios Recordatorios { get; }
    public AlmacenEnMemoria Almacen { get; }
    public ServicioRespaldo Respaldo { get; }

    // Como en la app, el cambio se marca con la hora de verdad en que ocurre.
    public async Task<Recordatorio> Guardar(string frase, DateTime _)
    {
        var ahora = ZonaLima.Ahora();
        return await Recordatorios.GuardarAsync(ParserLocal.Interpretar(frase, ahora), ahora);
    }

    public async ValueTask DisposeAsync()
    {
        await _repo.CerrarAsync();
        SQLitePCL.raw.sqlite3_shutdown();
        try { File.Delete(_ruta); } catch (IOException) { }
    }
}

public class RespaldoDeExtremoAExtremoTests : IDisposable
{
    // Los avisos usan "ahora" real al aplicar lo recibido; las fechas de estas frases son futuras.
    private static readonly DateTime Ahora = ZonaLima.Ahora();
    private readonly ApiFactory _api = new();

    private string Frase(string texto) => $"{texto}";

    private static DateTime Manana => ZonaLima.Ahora().Date.AddDays(1);

    private static string FraseMananaALas(int hora) => $"mañana pagar el recibo a las {hora} am";

    [Fact]
    public async Task Activar_respaldo_da_un_codigo_y_sube_lo_que_ya_habia()
    {
        await using var telefono = new TelefonoDePrueba(_api);
        await telefono.Guardar(FraseMananaALas(9), Ahora);

        var resultado = await telefono.Respaldo.ActivarAsync();

        Assert.Equal(ResultadoRed.Ok, resultado);
        Assert.True(telefono.Respaldo.Activo);
        Assert.Matches("^[0-9A-Z]{4}(-[0-9A-Z]{4}){3}$", telefono.Respaldo.CodigoFormateado!);

        using var alcance = _api.Services.CreateScope();
        var remoto = Assert.Single(await alcance.ServiceProvider.GetRequiredService<ApiDb>().Recordatorios.ToListAsync());
        Assert.Equal("Pagar el recibo", remoto.Titulo);
    }

    [Fact]
    public async Task Recuperar_en_un_telefono_nuevo_trae_los_recordatorios_con_sus_avisos()
    {
        await using var viejo = new TelefonoDePrueba(_api);
        await viejo.Guardar(FraseMananaALas(9), Ahora);
        await viejo.Guardar("5 de noviembre cumpleaños de Martín", Ahora);
        await viejo.Respaldo.ActivarAsync();
        var codigo = viejo.Respaldo.CodigoFormateado!;

        await using var nuevo = new TelefonoDePrueba(_api);
        var resultado = await nuevo.Respaldo.RecuperarAsync(codigo.ToLowerInvariant());

        Assert.Equal(ResultadoRed.Ok, resultado);
        var recuperados = await nuevo.Recordatorios.ListarAsync();
        Assert.Equal(2, recuperados.Count);
        Assert.Contains(recuperados, r => r.Titulo == "Cumpleaños de Martín" && r.Repeticion == Repeticion.Anual);
        Assert.NotEmpty(nuevo.Programador.Programados);
    }

    [Fact]
    public async Task Un_codigo_que_no_existe_no_deja_el_telefono_a_medias()
    {
        await using var telefono = new TelefonoDePrueba(_api);

        var resultado = await telefono.Respaldo.RecuperarAsync(CodigoRecuperacion.Formatear(CodigoRecuperacion.Generar()));

        Assert.Equal(ResultadoRed.CodigoInvalido, resultado);
        Assert.False(telefono.Respaldo.Activo);
    }

    [Theory]
    [InlineData("")]
    [InlineData("hola")]
    [InlineData("1234-5678")]
    public async Task Un_texto_que_no_parece_codigo_se_rechaza_sin_llamar_al_servidor(string texto)
    {
        await using var telefono = new TelefonoDePrueba(_api);

        Assert.Equal(ResultadoRed.FormatoInvalido, await telefono.Respaldo.RecuperarAsync(texto));
    }

    [Fact]
    public async Task Los_cambios_viajan_en_los_dos_sentidos_entre_dos_telefonos()
    {
        await using var a = new TelefonoDePrueba(_api);
        await a.Respaldo.ActivarAsync();
        await using var b = new TelefonoDePrueba(_api);
        await b.Respaldo.RecuperarAsync(a.Respaldo.CodigoFormateado!);

        // A crea; B lo recibe.
        var deA = await a.Guardar(FraseMananaALas(9), Ahora);
        await a.Respaldo.SincronizarAsync();
        await b.Respaldo.SincronizarAsync();
        Assert.Equal("Pagar el recibo", Assert.Single(await b.Recordatorios.ListarAsync()).Titulo);

        // B lo marca como hecho; A lo recibe.
        var enB = Assert.Single(await b.Recordatorios.ListarAsync());
        await b.Recordatorios.MarcarHechoAsync(enB, ZonaLima.Ahora());
        await b.Respaldo.SincronizarAsync();
        var sincA = await a.Respaldo.SincronizarAsync();

        Assert.Equal(1, sincA.Recibidos);
        Assert.True((await a.Recordatorios.ObtenerAsync(deA.Id))!.Hecho);

        // A lo elimina; B ya no lo tiene.
        var hechoEnA = (await a.Recordatorios.ObtenerAsync(deA.Id))!;
        await a.Recordatorios.EliminarAsync([hechoEnA], ZonaLima.Ahora());
        await a.Respaldo.SincronizarAsync();
        await b.Respaldo.SincronizarAsync();
        Assert.Empty(await b.Recordatorios.ListarAsync());
    }

    [Fact]
    public async Task Lo_recibido_reprograma_los_avisos_y_lo_eliminado_los_cancela()
    {
        await using var a = new TelefonoDePrueba(_api);
        await a.Respaldo.ActivarAsync();
        await using var b = new TelefonoDePrueba(_api);
        await b.Respaldo.RecuperarAsync(a.Respaldo.CodigoFormateado!);

        var r = await a.Guardar(FraseMananaALas(9), Ahora);
        await a.Respaldo.SincronizarAsync();
        await b.Respaldo.SincronizarAsync();
        Assert.Contains(b.Programador.Programados, p => p.RecordatorioId == r.Id);

        await a.Recordatorios.EliminarAsync([(await a.Recordatorios.ObtenerAsync(r.Id))!], ZonaLima.Ahora());
        await a.Respaldo.SincronizarAsync();
        await b.Respaldo.SincronizarAsync();

        Assert.DoesNotContain(b.Programador.Programados, p => p.RecordatorioId == r.Id);
    }

    [Fact]
    public async Task Sin_conexion_los_cambios_se_acumulan_y_se_envian_al_volver()
    {
        await using var a = new TelefonoDePrueba(_api);
        await a.Respaldo.ActivarAsync();

        // Un teléfono sin red: apunta a una dirección que no responde.
        var sinRed = new ServicioRespaldo(
            new RecordatorioRepositorio(Path.Combine(Path.GetTempPath(), $"x-{Guid.NewGuid():N}.db3")),
            a.Recordatorios,
            new ClienteRespaldo(new HttpClient(new FalloDeRed()) { BaseAddress = new Uri("http://sin.red/") }),
            a.Almacen);
        await a.Guardar(FraseMananaALas(9), Ahora);

        var intento = await sinRed.SincronizarAsync();
        Assert.Equal(ResultadoRed.SinConexion, intento.Resultado);
        Assert.NotNull(a.Almacen.Codigo);

        // Vuelve la conexión: el cambio acumulado sube.
        var despues = await a.Respaldo.SincronizarAsync();
        Assert.Equal(ResultadoRed.Ok, despues.Resultado);
        Assert.True(despues.Enviados >= 1);

        using var alcance = _api.Services.CreateScope();
        Assert.Single(await alcance.ServiceProvider.GetRequiredService<ApiDb>().Recordatorios.ToListAsync());
    }

    [Fact]
    public async Task Borrar_mis_datos_vacia_el_servidor_desactiva_el_respaldo_y_deja_los_recordatorios_del_telefono()
    {
        await using var telefono = new TelefonoDePrueba(_api);
        await telefono.Guardar(FraseMananaALas(9), Ahora);
        await telefono.Respaldo.ActivarAsync();

        var resultado = await telefono.Respaldo.BorrarMisDatosAsync();

        Assert.Equal(ResultadoRed.Ok, resultado);
        Assert.False(telefono.Respaldo.Activo);
        Assert.Single(await telefono.Recordatorios.ListarAsync());

        using var alcance = _api.Services.CreateScope();
        var db = alcance.ServiceProvider.GetRequiredService<ApiDb>();
        Assert.Empty(await db.Cuentas.ToListAsync());
        Assert.Empty(await db.Recordatorios.ToListAsync());
    }

    [Fact]
    public async Task Si_no_hay_red_borrar_no_desactiva_nada()
    {
        await using var telefono = new TelefonoDePrueba(_api);
        await telefono.Respaldo.ActivarAsync();
        var sinRed = new ServicioRespaldo(
            new RecordatorioRepositorio(Path.Combine(Path.GetTempPath(), $"x-{Guid.NewGuid():N}.db3")),
            telefono.Recordatorios,
            new ClienteRespaldo(new HttpClient(new FalloDeRed()) { BaseAddress = new Uri("http://sin.red/") }),
            telefono.Almacen);

        Assert.Equal(ResultadoRed.SinConexion, await sinRed.BorrarMisDatosAsync());
        Assert.True(telefono.Respaldo.Activo);
    }

    [Fact]
    public async Task Sin_direccion_del_servidor_se_dice_que_falta()
    {
        var cliente = new ClienteRespaldo(new HttpClient());

        var (resultado, _) = await cliente.CrearCuentaAsync();

        Assert.Equal(ResultadoRed.SinServidor, resultado);
    }

    public void Dispose() => _api.Dispose();

    private sealed class FalloDeRed : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            throw new HttpRequestException("sin red");
    }
}
