using Avalonia;
using LaberintoInteligente.Core;

namespace LaberintoInteligente.Interfaz;

/// <summary>Opciones de arranque. Ejemplo: LaberintoInteligente --modo 2 --semilla 42</summary>
public sealed record Opciones(Modo? Modo = null, int? Semilla = null,
                              int Velocidad = Juego.IntervaloAgentePorDefecto)
{
    public static Opciones Leer(string[] args)
    {
        var o = new Opciones();
        for (int i = 0; i + 1 < args.Length; i++)
        {
            if (!int.TryParse(args[i + 1], out int valor)) continue;
            switch (args[i])
            {
                case "--modo" when valor is >= 1 and <= 3: o = o with { Modo = (Modo)valor }; break;
                case "--semilla": o = o with { Semilla = valor }; break;
                case "--velocidad" when valor > 0: o = o with { Velocidad = valor }; break;
            }
        }
        return o;
    }
}

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        App.Opciones = Opciones.Leer(args);
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    // También la usa el diseñador de Avalonia
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
                  .UsePlatformDetect()
                  .WithInterFont()
                  .LogToTrace();
}
