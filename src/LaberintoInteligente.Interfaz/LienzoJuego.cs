using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using LaberintoInteligente.Core;

namespace LaberintoInteligente.Interfaz;

/// <summary>
/// Dibuja todas las pantallas (menú, partida y resultado). No contiene lógica de
/// juego: solo lee el estado de <see cref="Juego"/>.
/// </summary>
public sealed class LienzoJuego : Control
{
    public const double Ancho = 1000, Alto = 760;
    public const double AltoBarra = 130;          // barra superior fija: minimapa + información
    private const double MiniMaxAncho = 164, MiniMaxAlto = 116;

    public static readonly Color ColorFondo = Color.FromRgb(18, 20, 28);
    private static readonly Color ColPanel = Color.FromRgb(28, 32, 46);
    private static readonly Color ColMuro = Color.FromRgb(34, 40, 58);
    private static readonly Color ColCamino = Color.FromRgb(232, 235, 242);
    private static readonly Color ColEntrada = Color.FromRgb(60, 190, 100);
    private static readonly Color ColMeta = Color.FromRgb(235, 75, 75);
    private static readonly Color ColTexto = Color.FromRgb(240, 242, 248);
    private static readonly Color ColTextoSuave = Color.FromRgb(150, 158, 178);
    private static readonly Color ColAcento = Color.FromRgb(90, 140, 255);

    private static readonly IBrush Fondo = new SolidColorBrush(ColorFondo);
    private static readonly IBrush Panel = new SolidColorBrush(ColPanel);
    private static readonly IBrush Muro = new SolidColorBrush(ColMuro);
    private static readonly IBrush Camino = new SolidColorBrush(ColCamino);
    private static readonly IBrush Entrada = new SolidColorBrush(ColEntrada);
    private static readonly IBrush Meta = new SolidColorBrush(ColMeta);
    private static readonly IBrush Texto = new SolidColorBrush(ColTexto);
    private static readonly IBrush TextoSuave = new SolidColorBrush(ColTextoSuave);
    private static readonly IBrush Acento = new SolidColorBrush(ColAcento);
    private static readonly IPen BordeAcento = new Pen(Acento, 2);
    private static readonly IBrush Velo = new SolidColorBrush(Color.FromArgb(170, 18, 20, 28));

    private static readonly FontFamily Fuente = new("fonts:Inter#Inter");
    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-MX");

    private readonly Juego _juego;

    /// <summary>Posición del mouse, para resaltar la opción del menú.</summary>
    public Point? Mouse { get; set; }

    public LienzoJuego(Juego juego)
    {
        _juego = juego;
        Width = Ancho;
        Height = Alto;
        ClipToBounds = true;
    }

    // ------------------------------------------------------------------ utilidades
    private static Color ColorDe(uint rgb) => Color.FromRgb((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb);

    private static Color Mezclar(Color a, Color b, double t) => Color.FromRgb(
        (byte)(a.R * (1 - t) + b.R * t), (byte)(a.G * (1 - t) + b.G * t), (byte)(a.B * (1 - t) + b.B * t));

    private static void Escribir(DrawingContext ctx, string texto, double tamano, IBrush color, Point pos,
                                 bool centrado = false, bool negrita = false)
    {
        var ft = new FormattedText(texto, Cultura, FlowDirection.LeftToRight,
            new Typeface(Fuente, FontStyle.Normal, negrita ? FontWeight.Bold : FontWeight.Normal), tamano, color);
        var p = centrado ? new Point(pos.X - ft.Width / 2, pos.Y - ft.Height / 2) : pos;
        ctx.DrawText(ft, p);
    }

    // ----------------------------------------------------------------------- menú
    /// <summary>Rectángulos clicables de las tres modalidades.</summary>
    public static IEnumerable<(Modo Modo, Rect Rect)> RectsMenu()
    {
        foreach (var m in new[] { Modo.MaquinaVsMaquina, Modo.MaquinaVsUsuario, Modo.UsuarioVsUsuario })
            yield return (m, new Rect(Ancho / 2 - 290, 290 + ((int)m - 1) * 110, 580, 84));
    }

    public static Modo? ModoEn(Point p)
    {
        foreach (var (modo, rect) in RectsMenu())
            if (rect.Contains(p)) return modo;
        return null;
    }

    private void DibujarMenu(DrawingContext ctx)
    {
        ctx.FillRectangle(Fondo, new Rect(0, 0, Ancho, Alto));
        Escribir(ctx, "LABERINTO INTELIGENTE", 48, Texto, new Point(Ancho / 2, 130), true, true);
        Escribir(ctx, "Generación aleatoria y búsqueda en profundidad (DFS)", 19, TextoSuave,
                 new Point(Ancho / 2, 188), true);
        Escribir(ctx, "Elige una modalidad", 28, Texto, new Point(Ancho / 2, 250), true, true);

        foreach (var (modo, rect) in RectsMenu())
        {
            bool encima = Mouse is { } m && rect.Contains(m);
            var relleno = encima ? new SolidColorBrush(Mezclar(ColPanel, ColAcento, 0.35)) : Panel;
            ctx.DrawRectangle(relleno, BordeAcento, new RoundedRect(rect, 12));
            Escribir(ctx, ((int)modo).ToString(), 48, Acento, new Point(rect.X + 50, rect.Center.Y), true, true);
            Escribir(ctx, Juego.NombreModo(modo), 28, Texto, new Point(rect.X + 100, rect.Y + 12), negrita: true);
            string detalle = modo switch
            {
                Modo.MaquinaVsMaquina => "Dos agentes con DFS compiten por la salida",
                Modo.MaquinaVsUsuario => "Tú (flechas) contra un agente con DFS",
                _ => "Usuario 1 (flechas) contra Usuario 2 (W A S D)",
            };
            Escribir(ctx, detalle, 14, TextoSuave, new Point(rect.X + 100, rect.Y + 52));
        }
        Escribir(ctx, "Pulsa 1, 2 o 3, o haz clic  ·  Esc para salir", 14, TextoSuave,
                 new Point(Ancho / 2, Alto - 40), true);
    }

    // -------------------------------------------------------------------- partida
    private void DibujarMapa(DrawingContext ctx, Laberinto lab, double ox, double oy, double t)
    {
        ctx.FillRectangle(Camino, new Rect(ox, oy, lab.Columnas * t, lab.Filas * t));
        for (int y = 0; y < lab.Filas; y++)
            for (int x = 0; x < lab.Columnas; x++)
                if (lab.Matriz[y, x] == Laberinto.Muro)
                    ctx.FillRectangle(Muro, new Rect(ox + x * t, oy + y * t, t, t));
        ctx.FillRectangle(Entrada, new Rect(ox + lab.Inicio.X * t, oy + lab.Inicio.Y * t, t, t));
        ctx.FillRectangle(Meta, new Rect(ox + lab.Meta.X * t, oy + lab.Meta.Y * t, t, t));
    }

    /// <summary>Centro de cada participante; separa a los que comparten casilla.</summary>
    private static Dictionary<Participante, Point> Centros(IReadOnlyList<Participante> ps, double ox, double oy, double t)
    {
        var centros = new Dictionary<Participante, Point>();
        foreach (var grupo in ps.GroupBy(p => p.Pos))
        {
            var lista = grupo.ToList();
            for (int i = 0; i < lista.Count; i++)
            {
                double desplazo = (i - (lista.Count - 1) / 2.0) * t * 0.35;
                centros[lista[i]] = new Point(ox + grupo.Key.X * t + t / 2 + desplazo, oy + grupo.Key.Y * t + t / 2);
            }
        }
        return centros;
    }

    private void DibujarPartida(DrawingContext ctx)
    {
        var lab = _juego.Lab!;
        var ps = _juego.Participantes;
        ctx.FillRectangle(Fondo, new Rect(0, 0, Ancho, Alto));

        // Área principal: laberinto + rastro de exploración de los agentes
        double areaAlto = Alto - AltoBarra;
        double t = Math.Floor(Math.Min(Ancho / lab.Columnas, areaAlto / lab.Filas));
        double ox = Math.Floor((Ancho - lab.Columnas * t) / 2);
        double oy = AltoBarra + Math.Floor((areaAlto - lab.Filas * t) / 2);
        DibujarMapa(ctx, lab, ox, oy, t);
        foreach (var agente in ps.OfType<Agente>())
        {
            var rastro = new SolidColorBrush(Mezclar(ColorDe(agente.Color), ColCamino, 0.6));
            foreach (var c in agente.Visitadas)
                if (c != lab.Inicio && c != lab.Meta)
                    ctx.FillRectangle(rastro, new Rect(ox + c.X * t, oy + c.Y * t, t, t));
        }
        foreach (var (p, centro) in Centros(ps, ox, oy, t))
        {
            ctx.DrawEllipse(Fondo, null, centro, t * 0.40, t * 0.40);
            ctx.DrawEllipse(new SolidColorBrush(ColorDe(p.Color)), null, centro, t * 0.32, t * 0.32);
        }

        // Barra superior fija con el minimapa
        ctx.FillRectangle(Panel, new Rect(0, 0, Ancho, AltoBarra));
        double tm = Math.Max(2, Math.Floor(Math.Min(MiniMaxAncho / lab.Columnas, MiniMaxAlto / lab.Filas)));
        double mw = lab.Columnas * tm, mh = lab.Filas * tm;
        double mx = 20, my = Math.Floor((AltoBarra - mh) / 2);
        DibujarMapa(ctx, lab, mx, my, tm);
        ctx.DrawRectangle(null, BordeAcento, new Rect(mx - 2, my - 2, mw + 4, mh + 4));
        foreach (var p in ps)
        {
            var c = new Point(mx + p.Pos.X * tm + tm / 2, my + p.Pos.Y * tm + tm / 2);
            ctx.DrawEllipse(Fondo, null, c, tm + 1, tm + 1);
            ctx.DrawEllipse(new SolidColorBrush(ColorDe(p.Color)), null, c, tm, tm);
        }

        double x0 = mx + mw + 36;
        Escribir(ctx, Juego.NombreModo(_juego.Modo), 26, Texto, new Point(x0, 12), negrita: true);
        Escribir(ctx, $"Semilla {lab.Semilla}   ·   Tiempo {_juego.TiempoSegundos,5:F1} s", 18, TextoSuave,
                 new Point(x0, 50));
        for (int i = 0; i < ps.Count; i++)
        {
            double y = 82 + i * 22;
            ctx.DrawEllipse(new SolidColorBrush(ColorDe(ps[i].Color)), null, new Point(x0 + 8, y + 9), 8, 8);
            Escribir(ctx, $"{ps[i].Nombre} ({ps[i].Controles}): {ps[i].Pasos} pasos", 14, Texto, new Point(x0 + 24, y));
        }
        Escribir(ctx, "Esc: menú", 14, TextoSuave, new Point(Ancho - 90, 12));
    }

    private void DibujarResultado(DrawingContext ctx)
    {
        DibujarPartida(ctx);
        ctx.FillRectangle(Velo, new Rect(0, AltoBarra, Ancho, Alto - AltoBarra));

        var panel = new Rect(Ancho / 2 - 320, AltoBarra + (Alto - AltoBarra) / 2 - 145, 640, 290);
        ctx.DrawRectangle(Panel, BordeAcento, new RoundedRect(panel, 16));

        var ganadores = _juego.Ganadores;
        var (titulo, color) = ganadores.Count == 1
            ? ($"¡Gana {ganadores[0].Nombre}!", (IBrush)new SolidColorBrush(ColorDe(ganadores[0].Color)))
            : ("¡Empate!", Texto);
        Escribir(ctx, titulo, 46, color, new Point(panel.Center.X, panel.Y + 60), true, true);
        Escribir(ctx, $"Tiempo: {_juego.TiempoSegundos:F1} s   ·   Semilla del laberinto: {_juego.Lab!.Semilla}",
                 18, Texto, new Point(panel.Center.X, panel.Y + 120), true);
        var ps = _juego.Participantes;
        for (int i = 0; i < ps.Count; i++)
        {
            double y = panel.Y + 165 + i * 32;
            ctx.DrawEllipse(new SolidColorBrush(ColorDe(ps[i].Color)), null, new Point(panel.X + 150, y + 11), 9, 9);
            string llego = ganadores.Contains(ps[i]) ? "llegó a la salida" : "no llegó";
            Escribir(ctx, $"{ps[i].Nombre}: {ps[i].Pasos} pasos, {llego}", 18, Texto, new Point(panel.X + 172, y));
        }
        Escribir(ctx, "Enter: volver al menú   ·   R: otra partida (mismo modo, mapa nuevo)", 14, TextoSuave,
                 new Point(panel.Center.X, panel.Bottom - 32), true);
    }

    public override void Render(DrawingContext ctx)
    {
        switch (_juego.Estado)
        {
            case Estado.Menu: DibujarMenu(ctx); break;
            case Estado.Jugando: DibujarPartida(ctx); break;
            default: DibujarResultado(ctx); break;
        }
    }
}
