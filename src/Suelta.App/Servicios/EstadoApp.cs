namespace Suelta.App.Servicios;

public enum Pantalla
{
    Bienvenida,
    Permisos,
    Principal,
}

/// <summary>Recorrido de la primera apertura: bienvenida, permisos y pantalla principal (HU-01, HU-02).</summary>
public sealed class EstadoApp
{
    private const string ClaveBienvenidaVista = "bienvenida_vista";
    private const string ClavePermisosDecididos = "permisos_decididos";
    private const string ClaveAvisoAjustesMostrado = "aviso_ajustes_mostrado";

    public EstadoApp()
    {
        Actual = !Preferences.Get(ClaveBienvenidaVista, false) ? Pantalla.Bienvenida
            : !Preferences.Get(ClavePermisosDecididos, false) ? Pantalla.Permisos
            : Pantalla.Principal;
    }

    public Pantalla Actual { get; private set; }

    public event Action? Cambio;

    public void IrAPermisos()
    {
        Preferences.Set(ClaveBienvenidaVista, true);
        Ir(Pantalla.Permisos);
    }

    public void IrAPrincipal()
    {
        Preferences.Set(ClavePermisosDecididos, true);
        Ir(Pantalla.Principal);
    }

    /// <summary>El mensaje "Activa los avisos" se muestra una sola vez (HU-02).</summary>
    public bool AvisoAjustesYaMostrado
    {
        get => Preferences.Get(ClaveAvisoAjustesMostrado, false);
        set => Preferences.Set(ClaveAvisoAjustesMostrado, value);
    }

    private void Ir(Pantalla pantalla)
    {
        Actual = pantalla;
        Cambio?.Invoke();
    }
}
