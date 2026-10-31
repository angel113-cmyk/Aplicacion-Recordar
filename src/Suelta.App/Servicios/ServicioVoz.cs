using System.Globalization;
using CommunityToolkit.Maui.Media;

namespace Suelta.App.Servicios;

/// <summary>
/// Captura de voz en español mientras la persona mantiene apretado el botón (HT-03).
/// Solo escucha entre IniciarAsync y DetenerAsync.
/// </summary>
public sealed class ServicioVoz
{
    private static readonly CultureInfo Espanol = new("es-PE");

    private readonly ISpeechToText _stt;
    private Task<bool>? _inicio;
    private TaskCompletionSource<string>? _final;
    private string _ultimoParcial = "";

    public ServicioVoz(ISpeechToText stt)
    {
        _stt = stt;
        _stt.RecognitionResultUpdated += (_, e) =>
        {
            _ultimoParcial = e.RecognitionResult ?? "";
            TextoParcial?.Invoke(_ultimoParcial);
        };
        _stt.RecognitionResultCompleted += (_, e) =>
        {
            var r = e.RecognitionResult;
            _final?.TrySetResult(r.IsSuccessful && !string.IsNullOrWhiteSpace(r.Text) ? r.Text : _ultimoParcial);
        };
    }

    /// <summary>Lo que se va entendiendo mientras la persona habla.</summary>
    public event Action<string>? TextoParcial;

    /// <summary>Empieza a escuchar. Devuelve false si no hay permiso o el reconocimiento falló.</summary>
    public Task<bool> IniciarAsync() => _inicio = IniciarInternoAsync();

    /// <summary>Deja de escuchar y devuelve lo que se entendió ("" si no se entendió nada).</summary>
    public async Task<string> DetenerAsync()
    {
        var inicio = _inicio;
        _inicio = null;
        if (inicio is null || !await inicio) return "";

        try
        {
            await _stt.StopListenAsync(CancellationToken.None);

            var final = _final!.Task;
            var primero = await Task.WhenAny(final, Task.Delay(1500));
            return (primero == final ? await final : _ultimoParcial).Trim();
        }
        catch (Exception)
        {
            return _ultimoParcial.Trim();
        }
    }

    private async Task<bool> IniciarInternoAsync()
    {
        try
        {
            if (!await _stt.RequestPermissions(CancellationToken.None)) return false;

            _ultimoParcial = "";
            _final = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

            await _stt.StartListenAsync(
                new SpeechToTextOptions { Culture = Espanol, ShouldReportPartialResults = true },
                CancellationToken.None);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
