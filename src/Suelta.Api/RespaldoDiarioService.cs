using Microsoft.EntityFrameworkCore;
using Suelta.Api.Datos;

namespace Suelta.Api;

/// <summary>
/// Una copia de seguridad de la base de datos del servidor cada día (HT-18). Se conservan las últimas 7.
/// Usa VACUUM INTO de SQLite, que copia la base completa y consistente aunque esté en uso.
/// </summary>
public sealed class RespaldoDiarioService(IServiceProvider servicios, IConfiguration configuracion, ILogger<RespaldoDiarioService> registro)
    : BackgroundService
{
    private const int CopiasQueSeConservan = 7;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        using var temporizador = new PeriodicTimer(TimeSpan.FromHours(24));

        do
        {
            try
            {
                await CopiarAsync(ct);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                // Solo el tipo de error: nunca contenido de la base de datos.
                registro.LogError("Falló la copia diaria: {Tipo}", e.GetType().Name);
            }
        }
        while (await temporizador.WaitForNextTickAsync(ct));
    }

    public async Task CopiarAsync(CancellationToken ct)
    {
        var carpeta = configuracion["Datos:CarpetaRespaldos"] ?? "respaldos";
        Directory.CreateDirectory(carpeta);

        var destino = Path.GetFullPath(Path.Combine(carpeta, $"suelta-{DateTime.UtcNow:yyyyMMdd}.db"));
        if (File.Exists(destino)) File.Delete(destino);

        using var alcance = servicios.CreateScope();
        var db = alcance.ServiceProvider.GetRequiredService<ApiDb>();
        await db.Database.ExecuteSqlRawAsync($"VACUUM INTO '{destino.Replace("'", "''")}'", ct);

        foreach (var vieja in Directory.GetFiles(carpeta, "suelta-*.db").OrderDescending().Skip(CopiasQueSeConservan))
            File.Delete(vieja);
    }
}
