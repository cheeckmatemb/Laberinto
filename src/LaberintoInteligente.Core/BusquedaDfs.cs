namespace LaberintoInteligente.Core;

/// <summary>
/// Búsqueda en profundidad que avanza un paso a la vez. Cada llamada a
/// <see cref="Paso"/> profundiza hacia un vecino no visitado o, si no hay ninguno,
/// retrocede a la casilla anterior. Así el agente se mueve una casilla por paso y
/// se ve cómo explora y regresa, en lugar de saltar directo a la solución.
/// </summary>
public sealed class BusquedaDfs
{
    private readonly Laberinto _lab;
    private readonly Random? _rng;
    private readonly Stack<Posicion> _pila = new();          // camino actual desde la entrada
    private readonly HashSet<Posicion> _visitadas = new();   // casillas ya exploradas, evita ciclos
    private readonly List<Posicion> _vecinos = new(4);

    /// <summary>Casilla donde está el agente.</summary>
    public Posicion Actual { get; private set; }
    public bool Terminada { get; private set; }
    public bool Encontrada { get; private set; }
    /// <summary>Movimientos realizados, incluyendo los retrocesos.</summary>
    public int Pasos { get; private set; }
    public IReadOnlyCollection<Posicion> Visitadas => _visitadas;

    /// <summary>Ruta solución (de la entrada a la meta) si ya se encontró; si no, null.</summary>
    public List<Posicion>? Ruta => Encontrada ? _pila.Reverse().ToList() : null;

    /// <param name="rng">Si se da, baraja el orden de los vecinos: dos agentes con
    /// distinto rng exploran distinto el mismo mapa.</param>
    public BusquedaDfs(Laberinto lab, Random? rng = null)
    {
        _lab = lab;
        _rng = rng;
        _pila.Push(lab.Inicio);
        _visitadas.Add(lab.Inicio);
        Actual = lab.Inicio;
        if (lab.Inicio == lab.Meta)
            Terminada = Encontrada = true;
    }

    /// <summary>Avanza un paso. Devuelve false si la búsqueda ya había terminado.</summary>
    public bool Paso()
    {
        if (Terminada) return false;

        var actual = _pila.Peek();
        _vecinos.Clear();
        foreach (var d in Generador.Direcciones)
        {
            var v = actual.Mover(d);
            if (_lab.EsCamino(v) && !_visitadas.Contains(v))
                _vecinos.Add(v);
        }
        if (_rng is not null && _vecinos.Count > 1)
        {
            var arreglo = _vecinos.ToArray();
            _rng.Shuffle(arreglo);
            _vecinos.Clear();
            _vecinos.AddRange(arreglo);
        }

        if (_vecinos.Count > 0)
        {
            var siguiente = _vecinos[0];
            _visitadas.Add(siguiente);
            _pila.Push(siguiente);       // profundizar
        }
        else
        {
            _pila.Pop();                 // callejón sin salida: retroceder
            if (_pila.Count == 0)
            {
                Terminada = true;        // se exploró todo y no hay salida
                return false;
            }
        }

        Actual = _pila.Peek();
        Pasos++;
        if (Actual == _lab.Meta)
            Terminada = Encontrada = true;
        return true;
    }

    /// <summary>Ejecuta la búsqueda completa. Devuelve la ruta (o null) y los pasos.</summary>
    public static (List<Posicion>? Ruta, int Pasos) Resolver(Laberinto lab, Random? rng = null)
    {
        var b = new BusquedaDfs(lab, rng);
        while (b.Paso()) { }
        return (b.Ruta, b.Pasos);
    }
}
