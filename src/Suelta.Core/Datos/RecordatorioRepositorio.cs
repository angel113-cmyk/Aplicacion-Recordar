using SQLite;
using Suelta.Core.Dominio;
using Suelta.Core.Servicios;

namespace Suelta.Core.Datos;

/// <summary>
/// Base local SQLite (HT-05). El teléfono guarda todo primero; el servidor solo respalda.
/// El borrado es lógico (EliminadoEn) para poder deshacer y sincronizar después.
/// </summary>
public sealed class RecordatorioRepositorio : IRecordatorioRepositorio
{
    private readonly SQLiteAsyncConnection _db;
    private readonly SemaphoreSlim _inicio = new(1, 1);
    private bool _listo;

    public RecordatorioRepositorio(string rutaBaseDeDatos)
    {
        _db = new SQLiteAsyncConnection(rutaBaseDeDatos);
    }

    public async Task InsertarAsync(Recordatorio recordatorio)
    {
        await IniciarAsync();
        await _db.InsertAsync(recordatorio);
    }

    public async Task<List<Recordatorio>> ListarAsync()
    {
        await IniciarAsync();
        return await _db.Table<Recordatorio>().Where(r => r.EliminadoEn == null).ToListAsync();
    }

    public async Task CerrarAsync() => await _db.CloseAsync();

    private async Task IniciarAsync()
    {
        if (_listo) return;

        await _inicio.WaitAsync();
        try
        {
            if (_listo) return;
            await _db.CreateTableAsync<Recordatorio>(CreateFlags.ImplicitPK);
            _listo = true;
        }
        finally
        {
            _inicio.Release();
        }
    }
}
