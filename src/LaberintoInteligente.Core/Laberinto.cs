using System.Security.Cryptography;

namespace LaberintoInteligente.Core;

/// <summary>Casilla de la matriz, como (columna, fila).</summary>
public readonly record struct Posicion(int X, int Y)
{
    public Posicion Mover(Posicion d) => new(X + d.X, Y + d.Y);
    public int DistanciaManhattan(Posicion o) => Math.Abs(X - o.X) + Math.Abs(Y - o.Y);
}

/// <summary>
/// Laberinto representado como una matriz de enteros: 1 es muro y 0 es camino.
/// Se accede como Matriz[y, x]. Cada celda del laberinto ocupa una casilla impar
/// y entre dos celdas vecinas hay una casilla de muro que se derriba para unirlas,
/// por eso la matriz mide (2*alto+1) x (2*ancho+1).
/// </summary>
public sealed class Laberinto
{
    public const int Muro = 1;
    public const int Camino = 0;

    public int[,] Matriz { get; }
    public Posicion Inicio { get; }
    public Posicion Meta { get; }
    public int Semilla { get; }

    public int Filas => Matriz.GetLength(0);
    public int Columnas => Matriz.GetLength(1);

    public Laberinto(int[,] matriz, Posicion inicio, Posicion meta, int semilla)
    {
        Matriz = matriz;
        Inicio = inicio;
        Meta = meta;
        Semilla = semilla;
    }

    public bool EsCamino(Posicion p) => Matriz[p.Y, p.X] == Camino;
}

/// <summary>Generación aleatoria, ruta más corta (BFS) y validación del laberinto.</summary>
public static class Generador
{
    /// <summary>Orden fijo de las direcciones: arriba, derecha, abajo, izquierda.</summary>
    public static readonly Posicion[] Direcciones = { new(0, -1), new(1, 0), new(0, 1), new(-1, 0) };

    /// <summary>Genera un laberinto nuevo.</summary>
    /// <param name="ancho">Ancho en celdas.</param>
    /// <param name="alto">Alto en celdas.</param>
    /// <param name="semilla">Reproduce un mapa concreto; si es null se elige una al azar.</param>
    /// <param name="ciclos">Fracción de muros interiores que se abren para crear rutas
    /// alternativas (0 produce un laberinto perfecto, sin ciclos).</param>
    public static Laberinto Generar(int ancho = 20, int alto = 14, int? semilla = null, double ciclos = 0.08)
    {
        int s = semilla ?? RandomNumberGenerator.GetInt32(1_000_000);
        var rng = new Random(s); // generador propio: la misma semilla da el mismo mapa

        int columnas = 2 * ancho + 1, filas = 2 * alto + 1;
        var m = new int[filas, columnas];
        for (int y = 0; y < filas; y++)
            for (int x = 0; x < columnas; x++)
                m[y, x] = Laberinto.Muro;

        // DFS iterativo con retroceso (recursive backtracker) para tallar los pasillos
        var inicio = new Posicion(1, 1);
        m[1, 1] = Laberinto.Camino;
        var pila = new Stack<Posicion>();
        pila.Push(inicio);
        var vecinos = new List<(Posicion celda, Posicion dir)>(4);
        while (pila.Count > 0)
        {
            var actual = pila.Peek();
            vecinos.Clear();
            foreach (var d in Direcciones)
            {
                var v = new Posicion(actual.X + 2 * d.X, actual.Y + 2 * d.Y); // celda a dos casillas
                if (v.X > 0 && v.X < columnas - 1 && v.Y > 0 && v.Y < filas - 1 && m[v.Y, v.X] == Laberinto.Muro)
                    vecinos.Add((v, d));
            }
            if (vecinos.Count > 0)
            {
                var (sig, dir) = vecinos[rng.Next(vecinos.Count)];
                m[actual.Y + dir.Y, actual.X + dir.X] = Laberinto.Camino; // derribar el muro intermedio
                m[sig.Y, sig.X] = Laberinto.Camino;
                pila.Push(sig);  // profundizar
            }
            else
            {
                pila.Pop();      // retroceder
            }
        }

        var meta = new Posicion(columnas - 2, filas - 2);
        if (ciclos > 0)
            AbrirCiclos(m, ciclos, rng);

        var lab = new Laberinto(m, inicio, meta, s);
        if (!Validar(lab))
            throw new InvalidOperationException("El laberinto generado no tiene ruta a la salida.");
        return lab;
    }

    /// <summary>
    /// Derriba al azar una fracción de los muros que separan dos pasillos. Solo se
    /// consideran muros entre celdas (x+y impar), nunca los pilares de las esquinas.
    /// </summary>
    private static void AbrirCiclos(int[,] m, double fraccion, Random rng)
    {
        int filas = m.GetLength(0), columnas = m.GetLength(1);
        var candidatos = new List<Posicion>();
        for (int y = 1; y < filas - 1; y++)
            for (int x = 1; x < columnas - 1; x++)
            {
                if (m[y, x] != Laberinto.Muro || (x + y) % 2 == 0) continue;
                bool horizontal = m[y, x - 1] == Laberinto.Camino && m[y, x + 1] == Laberinto.Camino;
                bool vertical = m[y - 1, x] == Laberinto.Camino && m[y + 1, x] == Laberinto.Camino;
                if (horizontal || vertical) candidatos.Add(new Posicion(x, y));
            }
        var arreglo = candidatos.ToArray();
        rng.Shuffle(arreglo);
        int cuantos = (int)(arreglo.Length * fraccion);
        for (int i = 0; i < cuantos; i++)
            m[arreglo[i].Y, arreglo[i].X] = Laberinto.Camino;
    }

    /// <summary>Ruta más corta entre dos casillas con búsqueda en anchura, o null si no hay.</summary>
    public static List<Posicion>? RutaBfs(Laberinto lab, Posicion inicio, Posicion meta)
    {
        var padre = new Dictionary<Posicion, Posicion?> { [inicio] = null };
        var cola = new Queue<Posicion>();
        cola.Enqueue(inicio);
        while (cola.Count > 0)
        {
            var actual = cola.Dequeue();
            if (actual == meta)
            {
                var ruta = new List<Posicion>();
                for (Posicion? p = actual; p is not null; p = padre[p.Value])
                    ruta.Add(p.Value);
                ruta.Reverse();
                return ruta;
            }
            foreach (var d in Direcciones)
            {
                var sig = actual.Mover(d);
                if (lab.EsCamino(sig) && !padre.ContainsKey(sig))
                {
                    padre[sig] = actual;
                    cola.Enqueue(sig);
                }
            }
        }
        return null;
    }

    /// <summary>
    /// Comprueba que existe ruta de la entrada a la salida. Usa BFS a propósito: es
    /// una verificación independiente del DFS del agente.
    /// </summary>
    public static bool Validar(Laberinto lab) => RutaBfs(lab, lab.Inicio, lab.Meta) is not null;
}
