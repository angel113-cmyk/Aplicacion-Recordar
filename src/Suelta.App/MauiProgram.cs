using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Media;
using Microsoft.Extensions.Logging;
using Suelta.App.Avisos;
using Suelta.App.Servicios;
using Suelta.Core.Avisos;
using Suelta.Core.Datos;
using Suelta.Core.Respaldo;
using Suelta.Core.Servicios;

namespace Suelta.App;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var registro = new RegistroErrores(Path.Combine(FileSystem.AppDataDirectory, "errores.log"));
		AppDomain.CurrentDomain.UnhandledException += (_, e) =>
		{
			if (e.ExceptionObject is Exception excepcion) registro.Error("NoControlado", excepcion);
		};
		TaskScheduler.UnobservedTaskException += (_, e) => registro.Error("Tarea", e.Exception);

		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseMauiCommunityToolkit();

		builder.Services.AddMauiBlazorWebView();

		builder.Services.AddSingleton(registro);
		builder.Services.AddSingleton<ISpeechToText>(SpeechToText.Default);
		builder.Services.AddSingleton<ServicioVoz>();
		builder.Services.AddSingleton<ServicioPermisos>();
		builder.Services.AddSingleton<EstadoApp>();
		builder.Services.AddSingleton<IRecordatorioRepositorio>(_ =>
			new RecordatorioRepositorio(Path.Combine(FileSystem.AppDataDirectory, "suelta.db3")));
		builder.Services.AddSingleton<IProgramadorAvisos>(_ => new ProgramadorAvisosAndroid(Android.App.Application.Context));
		builder.Services.AddSingleton<ServicioRecordatorios>();

		// Respaldo opcional (HU-26): nada sale del teléfono mientras no se active.
		builder.Services.AddSingleton(new HttpClient { Timeout = TimeSpan.FromSeconds(20) });
		builder.Services.AddSingleton<ConfiguracionServidor>();
		builder.Services.AddSingleton<IAlmacenRespaldo, AlmacenRespaldoApp>();
		builder.Services.AddSingleton<ClienteRespaldo>();
		builder.Services.AddSingleton<ServicioRespaldo>();

#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
