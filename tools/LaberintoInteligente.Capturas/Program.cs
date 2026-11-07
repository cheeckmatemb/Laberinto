// Simula partidas sin ventana y guarda capturas en docs/capturas/.
// Sirve para (1) probar el juego completo de forma automática y (2) generar las
// imágenes de la sección de evidencias del documento.
// Uso (desde la raíz del repositorio): dotnet run --project tools/LaberintoInteligente.Capturas

using Avalonia;
using Avalonia.Headless;
using Avalonia.Media.Imaging;
using LaberintoInteligente.Core;
using LaberintoInteligente.Interfaz;

const int CuadroMs = 16; // tiempo simulado por cuadro (~60 fps)

AppBuilder.Configure<App>()
    .UseSkia()
    .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false })
    .WithInterFont()
    .SetupWithoutStarting();

string salida = Path.GetFullPath(args.Length > 0 ? args[0] : Path.Combine("docs", "capturas"));
Directory.CreateDirectory(salida);

var juego = new Juego();
var lienzo = new LienzoJuego(juego);
lienzo.Measure(new Size(LienzoJuego.Ancho, LienzoJuego.Alto));
lienzo.Arrange(new Rect(0, 0, LienzoJuego.Ancho, LienzoJuego.Alto));

void Guardar(string nombre)
{
    // Escala 2x para que las capturas se vean nítidas en el documento
    using var bmp = new RenderTargetBitmap(new PixelSize(2 * (int)LienzoJuego.Ancho, 2 * (int)LienzoJuego.Alto),
                                           new Vector(192, 192));
    bmp.Render(lienzo);
    bmp.Save(Path.Combine(salida, nombre));
    Console.WriteLine($"guardada {nombre}");
}

// Cada persona simulada toma la ruta más corta; el segundo jugador reacciona 700 ms tarde
Direccion? Humano(Jugador j, long ahora)
{
    if (j.Esquema == Esquema.Wasd && ahora < 700) return null;
    var ruta = Generador.RutaBfs(juego.Lab!, j.Pos, juego.Lab!.Meta);
    if (ruta is null || ruta.Count < 2) return null;
    return (ruta[1].X - j.Pos.X, ruta[1].Y - j.Pos.Y) switch
    {
        (0, -1) => Direccion.Arriba,
        (0, 1) => Direccion.Abajo,
        (-1, 0) => Direccion.Izquierda,
        _ => Direccion.Derecha,
    };
}

long Avanzar(long ahora, long? duracionMs)
{
    long inicio = ahora;
    while (juego.Estado == Estado.Jugando)
    {
        ahora += CuadroMs;
        long t = ahora;
        juego.Actualizar(ahora, j => Humano(j, t));
        if (duracionMs is { } d && ahora - inicio >= d) break;
        if (ahora - inicio > 120_000) throw new InvalidOperationException("La partida no terminó.");
    }
    return ahora;
}

Guardar("01_menu.png");

var resumen = new List<string>();
foreach (var (modo, semilla, parcialMs, nombre) in new[]
{
    (Modo.MaquinaVsMaquina, 11, 6000L, "02_maquina_vs_maquina"),
    (Modo.MaquinaVsUsuario, 22, 5000L, "03_maquina_vs_usuario"),
    (Modo.UsuarioVsUsuario, 33, 5000L, "04_usuario_vs_usuario"),
})
{
    juego.SemillaFija = semilla;
    juego.NuevaPartida(modo, 0);
    long ahora = Avanzar(0, parcialMs);
    Guardar($"{nombre}.png");
    Avanzar(ahora, null);
    if (juego.Estado != Estado.Resultado || juego.Ganadores.Count == 0)
        throw new InvalidOperationException($"{modo} no terminó con ganador.");
    Guardar($"{nombre}_resultado.png");
    resumen.Add($"  {Juego.NombreModo(modo),-20} semilla {semilla,-4} {juego.TiempoSegundos,5:F1} s  " +
                $"ganador: {string.Join(", ", juego.Ganadores.Select(g => g.Nombre))}");
}

// Tres laberintos distintos recién generados (evidencia de generación aleatoria)
int i = 1;
foreach (int semilla in new[] { 101, 202, 303 })
{
    juego.SemillaFija = semilla;
    juego.NuevaPartida(Modo.UsuarioVsUsuario, 0);
    Guardar($"05_laberinto_{i++}.png");
}

Console.WriteLine("\nResumen de partidas simuladas:");
resumen.ForEach(Console.WriteLine);
