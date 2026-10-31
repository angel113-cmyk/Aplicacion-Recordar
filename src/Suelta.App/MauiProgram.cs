using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Media;
using Microsoft.Extensions.Logging;
using Suelta.App.Servicios;
using Suelta.Core.Datos;
using Suelta.Core.Servicios;

namespace Suelta.App;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseMauiCommunityToolkit();

		builder.Services.AddMauiBlazorWebView();

		builder.Services.AddSingleton<ISpeechToText>(SpeechToText.Default);
		builder.Services.AddSingleton<ServicioVoz>();
		builder.Services.AddSingleton<ServicioPermisos>();
		builder.Services.AddSingleton<EstadoApp>();
		builder.Services.AddSingleton<IRecordatorioRepositorio>(_ =>
			new RecordatorioRepositorio(Path.Combine(FileSystem.AppDataDirectory, "suelta.db3")));
		builder.Services.AddSingleton<ServicioRecordatorios>();

#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
