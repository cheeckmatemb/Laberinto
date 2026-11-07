using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace LaberintoInteligente.Interfaz;

public partial class App : Application
{
    /// <summary>Opciones de línea de comandos; Program las llena antes de arrancar.</summary>
    public static Opciones Opciones { get; set; } = new();

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        // Sin ventana cuando corre en modo headless (herramienta de capturas)
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime escritorio)
            escritorio.MainWindow = new VentanaPrincipal(Opciones);
        base.OnFrameworkInitializationCompleted();
    }
}
