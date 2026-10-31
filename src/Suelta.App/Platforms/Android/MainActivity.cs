using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Suelta.App.Avisos;
using Suelta.App.Servicios;

namespace Suelta.App;

[Activity(
    Theme = "@style/Maui.SplashTheme",
    MainLauncher = true,
    LaunchMode = LaunchMode.SingleTop,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        AperturaDesdeAviso.Registrar(Intent?.GetStringExtra(ConstantesAvisos.ExtraAbrirId));
    }

    // La app ya estaba abierta y se tocó una notificación.
    protected override void OnNewIntent(Intent? intent)
    {
        base.OnNewIntent(intent);
        AperturaDesdeAviso.Registrar(intent?.GetStringExtra(ConstantesAvisos.ExtraAbrirId));
    }
}
