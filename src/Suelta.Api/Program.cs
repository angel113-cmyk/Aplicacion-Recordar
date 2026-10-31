using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Suelta.Api;
using Suelta.Api.Datos;
using Suelta.Core.Respaldo;

var builder = WebApplication.CreateBuilder(args);

var rutaBase = builder.Configuration["Datos:Ruta"] ?? "suelta-api.db";
builder.Services.AddDbContext<ApiDb>(o => o.UseSqlite($"Data Source={rutaBase}"));

// La misma serialización (generada en compilación) que usa el cliente de la app.
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.TypeInfoResolverChain.Insert(0, JsonContexto.Default));

// Límite de peticiones (HT-09): por dirección de red y por código, para frenar a quien pruebe códigos al azar.
var porMinuto = builder.Configuration.GetValue("Limites:PorMinuto", 120);
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    o.GlobalLimiter = PartitionedRateLimiter.CreateChained(
        PartitionedRateLimiter.Create<HttpContext, string>(http =>
            RateLimitPartition.GetFixedWindowLimiter(
                $"ip:{http.Connection.RemoteIpAddress}",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = porMinuto, Window = TimeSpan.FromMinutes(1) })),
        PartitionedRateLimiter.Create<HttpContext, string>(http =>
            RateLimitPartition.GetFixedWindowLimiter(
                $"codigo:{http.Request.Headers[ClienteRespaldo.EncabezadoCodigo]}",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = porMinuto, Window = TimeSpan.FromMinutes(1) })));

    // Crear cuentas es raro: pocas por hora y por dirección.
    o.AddPolicy("creacion", http =>
        RateLimitPartition.GetFixedWindowLimiter(
            $"ip:{http.Connection.RemoteIpAddress}",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = builder.Configuration.GetValue("Limites:CuentasPorHora", 10),
                Window = TimeSpan.FromHours(1),
            }));
});

if (builder.Configuration.GetValue("Respaldos:Activo", true))
    builder.Services.AddHostedService<RespaldoDiarioService>();

// Detrás de un proxy (Fly.io, Railway, nginx...) la dirección real de la persona llega en X-Forwarded-For.
// Solo se activa si se indica, porque si la API está expuesta directamente cualquiera podría falsearla.
var detrasDeProxy = builder.Configuration.GetValue("Proxy:ConfiarEnCabeceras", false);
if (detrasDeProxy)
{
    builder.Services.Configure<ForwardedHeadersOptions>(o =>
    {
        o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        o.KnownIPNetworks.Clear();
        o.KnownProxies.Clear();
    });
}

var app = builder.Build();

using (var alcance = app.Services.CreateScope())
    alcance.ServiceProvider.GetRequiredService<ApiDb>().Database.Migrate();

if (detrasDeProxy) app.UseForwardedHeaders();
app.UseRateLimiter();

app.MapGet("/salud", () => Results.Ok("ok"));

var respaldo = app.MapGroup("/v1/respaldo");
respaldo.MapPost("", CrearCuentaAsync).RequireRateLimiting("creacion");
respaldo.MapPost("/sincronizar", SincronizarAsync);
respaldo.MapDelete("", BorrarAsync);

app.Run();

// ---- endpoints ----

static async Task<Ok<CuentaCreada>> CrearCuentaAsync(ApiDb db, CancellationToken ct)
{
    string normalizado;
    string huella;

    // Con 80 bits de azar una repetición es imposible en la práctica, pero es barato comprobarlo.
    do
    {
        normalizado = CodigoRecuperacion.Generar();
        huella = CodigoRecuperacion.Huella(normalizado);
    }
    while (await db.Cuentas.AnyAsync(c => c.CodigoHash == huella, ct));

    db.Cuentas.Add(new Cuenta { Id = Guid.NewGuid(), CodigoHash = huella, CreadaEnUtc = DateTime.UtcNow });
    await db.SaveChangesAsync(ct);

    return TypedResults.Ok(new CuentaCreada(CodigoRecuperacion.Formatear(normalizado)));
}

static async Task<IResult> SincronizarAsync(HttpContext http, ApiDb db, SincronizarSolicitud solicitud, CancellationToken ct)
{
    var cuenta = await BuscarCuentaAsync(http, db, ct);
    if (cuenta is null) return TypedResults.Unauthorized();

    if (solicitud.Cambios.Count > Limites.CambiosPorPeticion) return TypedResults.BadRequest();

    var ahora = DateTime.UtcNow;
    var devolver = new List<RecordatorioDto>();

    var ids = solicitud.Cambios.Select(c => c.Id).Distinct().ToList();
    var existentes = await db.Recordatorios
        .Where(r => r.CuentaId == cuenta.Id && ids.Contains(r.Id))
        .ToDictionaryAsync(r => r.Id, ct);

    foreach (var cambio in solicitud.Cambios)
    {
        if (!EsValido(cambio)) continue;

        if (!existentes.TryGetValue(cambio.Id, out var actual))
        {
            var nuevo = new RecordatorioRemoto { CuentaId = cuenta.Id, Id = cambio.Id };
            Copiar(cambio, nuevo, ahora);
            db.Recordatorios.Add(nuevo);
            existentes[cambio.Id] = nuevo;
        }
        else if (cambio.ActualizadoEnUtc > actual.ActualizadoEnUtc)
        {
            // Gana la última modificación.
            Copiar(cambio, actual, ahora);
        }
        else if (cambio.ActualizadoEnUtc < actual.ActualizadoEnUtc)
        {
            // El servidor tiene una versión más nueva: se la devolvemos al teléfono.
            devolver.Add(ADto(actual));
        }
    }

    await db.SaveChangesAsync(ct);

    // Lo que otros teléfonos cambiaron desde la última vez.
    var desde = solicitud.DesdeUtc ?? DateTime.MinValue;
    var novedades = await db.Recordatorios
        .Where(r => r.CuentaId == cuenta.Id && r.RecibidoEnUtc > desde && !ids.Contains(r.Id))
        .ToListAsync(ct);
    devolver.AddRange(novedades.Select(ADto));

    // Unos segundos de margen: si dos teléfonos sincronizan a la vez, ninguno se pierde un cambio.
    return TypedResults.Ok(new SincronizarRespuesta(ahora.AddSeconds(-5), devolver));
}

static async Task<IResult> BorrarAsync(HttpContext http, ApiDb db, CancellationToken ct)
{
    var cuenta = await BuscarCuentaAsync(http, db, ct);
    if (cuenta is null) return TypedResults.Unauthorized();

    // Borra la cuenta y, en cascada, todos sus recordatorios (HU-26, HT-13).
    db.Cuentas.Remove(cuenta);
    await db.SaveChangesAsync(ct);
    return TypedResults.NoContent();
}

static async Task<Cuenta?> BuscarCuentaAsync(HttpContext http, ApiDb db, CancellationToken ct)
{
    var codigo = CodigoRecuperacion.Normalizar(http.Request.Headers[ClienteRespaldo.EncabezadoCodigo].ToString());
    if (codigo is null) return null;

    var huella = CodigoRecuperacion.Huella(codigo);
    return await db.Cuentas.FirstOrDefaultAsync(c => c.CodigoHash == huella, ct);
}

static bool EsValido(RecordatorioDto d) =>
    d.Id != Guid.Empty
    && d.Titulo.Length <= Limites.Titulo
    && d.Dijiste.Length <= Limites.Dijiste;

static void Copiar(RecordatorioDto d, RecordatorioRemoto r, DateTime recibidoEn)
{
    r.Titulo = d.Titulo;
    r.Categoria = d.Categoria;
    r.FechaHora = d.FechaHora;
    r.SinHora = d.SinHora;
    r.Repeticion = d.Repeticion;
    r.AnclaDia = d.AnclaDia;
    r.Monto = d.Monto;
    r.Dijiste = d.Dijiste;
    r.Hecho = d.Hecho;
    r.PospuestoHasta = d.PospuestoHasta;
    r.CreadoEnUtc = d.CreadoEnUtc;
    r.ActualizadoEnUtc = d.ActualizadoEnUtc;
    r.EliminadoEnUtc = d.EliminadoEnUtc;
    r.RecibidoEnUtc = recibidoEn;
}

static RecordatorioDto ADto(RecordatorioRemoto r) => new(
    r.Id, r.Titulo, r.Categoria, r.FechaHora, r.SinHora, r.Repeticion, r.AnclaDia, r.Monto, r.Dijiste,
    r.Hecho, r.PospuestoHasta, r.CreadoEnUtc, r.ActualizadoEnUtc, r.EliminadoEnUtc);

/// <summary>Lo necesita WebApplicationFactory en las pruebas.</summary>
public partial class Program;
