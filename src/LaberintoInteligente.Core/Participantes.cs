namespace LaberintoInteligente.Core;

public enum Direccion { Arriba, Abajo, Izquierda, Derecha }

/// <summary>Esquema de teclas de una persona. La interfaz lo traduce a teclas reales.</summary>
public enum Esquema { Flechas, Wasd }

/// <summary>
/// Base común de personas y máquinas. Juego trata a todos igual: les pide que se
/// actualicen y revisa si llegaron a la meta.
/// </summary>
public abstract class Participante
{
    protected readonly Laberinto Lab;

    public string Nombre { get; }
    /// <summary>Color en formato 0xRRGGBB (la interfaz decide cómo pintarlo).</summary>
    public uint Color { get; }
    public Posicion Pos { get; protected set; }
    public int Pasos { get; protected set; }
    public abstract string Controles { get; }
    public bool EnMeta => Pos == Lab.Meta;

    protected Participante(string nombre, uint color, Laberinto lab)
    {
        Nombre = nombre;
        Color = color;
        Lab = lab;
        Pos = lab.Inicio;
    }

    /// <param name="ahoraMs">Reloj del juego en milisegundos.</param>
    /// <param name="direccion">Dirección pulsada (solo la usan las personas).</param>
    public abstract void Actualizar(long ahoraMs, Direccion? direccion);
}

/// <summary>Persona que se mueve con el teclado. Los muros bloquean el paso.</summary>
public sealed class Jugador : Participante
{
    public const int CooldownMs = 90; // tiempo mínimo entre dos movimientos
    private long _ultimo = long.MinValue / 2;

    public Esquema Esquema { get; }
    public override string Controles => Esquema == Esquema.Flechas ? "flechas" : "W A S D";

    public Jugador(string nombre, uint color, Laberinto lab, Esquema esquema) : base(nombre, color, lab)
        => Esquema = esquema;

    public override void Actualizar(long ahoraMs, Direccion? direccion)
    {
        if (direccion is null || ahoraMs - _ultimo < CooldownMs) return;
        var d = direccion switch
        {
            Direccion.Arriba => new Posicion(0, -1),
            Direccion.Abajo => new Posicion(0, 1),
            Direccion.Izquierda => new Posicion(-1, 0),
            _ => new Posicion(1, 0),
        };
        var destino = Pos.Mover(d);
        if (!Lab.EsCamino(destino)) return; // colisión: solo se avanza por camino
        Pos = destino;
        Pasos++;
        _ultimo = ahoraMs;
    }
}

/// <summary>
/// Máquina que se mueve con <see cref="BusquedaDfs"/>: cada vez que pasa su
/// intervalo da un paso de la búsqueda (avanzar o retroceder).
/// </summary>
public sealed class Agente : Participante
{
    private readonly BusquedaDfs _dfs;
    private long _ultimo = long.MinValue / 2;

    public int IntervaloMs { get; }
    public override string Controles => "DFS";
    /// <summary>Casillas exploradas, para dibujar el rastro.</summary>
    public IReadOnlyCollection<Posicion> Visitadas => _dfs.Visitadas;
    public List<Posicion>? Ruta => _dfs.Ruta;

    public Agente(string nombre, uint color, Laberinto lab, Random rng, int intervaloMs) : base(nombre, color, lab)
    {
        _dfs = new BusquedaDfs(lab, rng);
        IntervaloMs = intervaloMs;
    }

    public override void Actualizar(long ahoraMs, Direccion? direccion)
    {
        if (_dfs.Terminada || ahoraMs - _ultimo < IntervaloMs) return;
        _ultimo = ahoraMs;
        if (_dfs.Paso())
        {
            Pos = _dfs.Actual;
            Pasos = _dfs.Pasos;
        }
    }
}
