using Android.App;
using Android.Runtime;

namespace Suelta.App;

// Probar el respaldo contra un servidor en tu PC (http://) solo se permite en compilaciones de desarrollo.
#if DEBUG
[Application(UsesCleartextTraffic = true)]
#else
[Application]
#endif
public class MainApplication : MauiApplication
{
	public MainApplication(IntPtr handle, JniHandleOwnership ownership)
		: base(handle, ownership)
	{
	}

	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
