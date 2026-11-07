using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using LaberintoInteligente.Core;

namespace LaberintoInteligente.Interfaz;

/// <summary>
/// Ventana del juego: lee el teclado y el mouse, mueve el reloj y pide redibujar.
/// Toda la lógica vive en <see cref="Juego"/>; el dibujo, en <see cref="LienzoJuego"/>.
/// </summary>
public sealed class VentanaPrincipal : Window
{
    // Orden = prioridad si hay varias teclas pulsadas
    private static readonly (Key Tecla, Direccion Dir)[] Flechas =
        { (Key.Up, Direccion.Arriba), (Key.Down, Direccion.Abajo), (Key.Left, Direccion.Izquierda), (Key.Right, Direccion.Derecha) };
    private static readonly (Key Tecla, Direccion Dir)[] Wasd =
        { (Key.W, Direccion.Arriba), (Key.S, Direccion.Abajo), (Key.A, Direccion.Izquierda), (Key.D, Direccion.Derecha) };

    private static readonly Dictionary<Key, Modo> TeclasModo = new()
    {
        [Key.D1] = Modo.MaquinaVsMaquina, [Key.NumPad1] = Modo.MaquinaVsMaquina,
        [Key.D2] = Modo.MaquinaVsUsuario, [Key.NumPad2] = Modo.MaquinaVsUsuario,
        [Key.D3] = Modo.UsuarioVsUsuario, [Key.NumPad3] = Modo.UsuarioVsUsuario,
    };

    private readonly Juego _juego;
    private readonly LienzoJuego _lienzo;
    private readonly HashSet<Key> _presionadas = new();
    private readonly Stopwatch _reloj = Stopwatch.StartNew();

    public VentanaPrincipal(Opciones opciones)
    {
        Title = "Laberinto Inteligente";
        Width = LienzoJuego.Ancho;
        Height = LienzoJuego.Alto;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Background = new SolidColorBrush(LienzoJuego.ColorFondo);

        _juego = new Juego(opciones.Velocidad, opciones.Semilla);
        _lienzo = new LienzoJuego(_juego);
        Content = _lienzo;
        if (opciones.Modo is { } modo)
            _juego.NuevaPartida(modo, Ahora);

        // Túnel: la ventana ve las teclas antes que cualquier control (evita que las
        // flechas muevan el foco en lugar de al jugador)
        AddHandler(KeyDownEvent, AlPresionar, RoutingStrategies.Tunnel);
        AddHandler(KeyUpEvent, AlSoltar, RoutingStrategies.Tunnel);
        Deactivated += (_, _) => _presionadas.Clear();
        _lienzo.PointerPressed += AlHacerClic;
        _lienzo.PointerMoved += (_, e) => _lienzo.Mouse = e.GetPosition(_lienzo);

        var temporizador = new DispatcherTimer(TimeSpan.FromMilliseconds(16), DispatcherPriority.Render, Cuadro);
        temporizador.Start();
    }

    private long Ahora => _reloj.ElapsedMilliseconds;

    /// <summary>Un cuadro del juego (~60 por segundo).</summary>
    private void Cuadro(object? sender, EventArgs e)
    {
        _juego.Actualizar(Ahora, LeerDireccion);
        _lienzo.InvalidateVisual();
    }

    private Direccion? LeerDireccion(Jugador j)
    {
        foreach (var (tecla, dir) in j.Esquema == Esquema.Flechas ? Flechas : Wasd)
            if (_presionadas.Contains(tecla)) return dir;
        return null;
    }

    private void AlPresionar(object? sender, KeyEventArgs e)
    {
        _presionadas.Add(e.Key);
        switch (_juego.Estado)
        {
            case Estado.Menu when TeclasModo.TryGetValue(e.Key, out var modo):
                _juego.NuevaPartida(modo, Ahora);
                break;
            case Estado.Menu when e.Key == Key.Escape:
                Close();
                break;
            case Estado.Jugando when e.Key == Key.Escape:
                _juego.IrAlMenu();
                break;
            case Estado.Resultado when e.Key is Key.Enter or Key.Escape:
                _juego.IrAlMenu();
                break;
            case Estado.Resultado when e.Key == Key.R:
                _juego.Repetir(Ahora);
                break;
        }
        e.Handled = true;
    }

    private void AlSoltar(object? sender, KeyEventArgs e)
    {
        _presionadas.Remove(e.Key);
        e.Handled = true;
    }

    private void AlHacerClic(object? sender, PointerPressedEventArgs e)
    {
        if (_juego.Estado != Estado.Menu) return;
        var modo = LienzoJuego.ModoEn(e.GetPosition(_lienzo));
        if (modo is not null)
            _juego.NuevaPartida(modo.Value, Ahora);
    }
}
