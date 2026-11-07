using System.Security.Cryptography;

namespace LaberintoInteligente.Core;

public enum Estado { Menu, Jugando, Resultado }

public enum Modo { MaquinaVsMaquina = 1, MaquinaVsUsuario = 2, UsuarioVsUsuario = 3 }

/// <summary>
/// Máquina de estados del juego (Menu → Jugando → Resultado). No sabe nada de la
/// interfaz: recibe el reloj y la dirección que pulsa cada persona.
/// </summary>
public sealed class Juego
{
    public const int AnchoCeldas = 20, AltoCeldas = 14;
    /// <summary>DFS necesita ~5 veces más pasos que la ruta óptima; 30 ms lo hace competitivo.</summary>
    public const int IntervaloAgentePorDefecto = 30;

    public const uint Azul = 0x3C78FF, Naranja = 0xFF9628, Magenta = 0xC83CC8, Turquesa = 0x1EBEAA;

    public Estado Estado { get; private set; } = Estado.Menu;
    public Modo Modo { get; private set; }
    public Laberinto? Lab { get; private set; }
    public List<Participante> Participantes { get; } = new();
    public List<Participante> Ganadores { get; } = new();
    public double TiempoSegundos { get; private set; }
    public int IntervaloAgenteMs { get; set; }
    /// <summary>Si se fija, la siguiente partida usa esta semilla (solo una vez).</summary>
    public int? SemillaFija { get; set; }

    private long _t0;

    public Juego(int intervaloAgenteMs = IntervaloAgentePorDefecto, int? semillaFija = null)
    {
        IntervaloAgenteMs = intervaloAgenteMs;
        SemillaFija = semillaFija;
    }

    public static string NombreModo(Modo modo) => modo switch
    {
        Modo.MaquinaVsMaquina => "Máquina vs Máquina",
        Modo.MaquinaVsUsuario => "Máquina vs Usuario",
        _ => "Usuario vs Usuario",
    };

    /// <summary>Genera un laberinto nuevo (semilla nueva) y crea a los participantes.</summary>
    public void NuevaPartida(Modo modo, long ahoraMs)
    {
        int semilla = SemillaFija ?? RandomNumberGenerator.GetInt32(1_000_000);
        SemillaFija = null;
        Modo = modo;
        Lab = Generador.Generar(AnchoCeldas, AltoCeldas, semilla);

        Participantes.Clear();
        int iv = IntervaloAgenteMs;
        switch (modo)
        {
            case Modo.MaquinaVsMaquina:
                Participantes.Add(new Agente("Máquina 1", Magenta, Lab, new Random(semilla + 1), iv));
                Participantes.Add(new Agente("Máquina 2", Turquesa, Lab, new Random(semilla + 2), iv));
                break;
            case Modo.MaquinaVsUsuario:
                Participantes.Add(new Jugador("Usuario", Azul, Lab, Esquema.Flechas));
                Participantes.Add(new Agente("Máquina", Magenta, Lab, new Random(semilla + 1), iv));
                break;
            default:
                Participantes.Add(new Jugador("Usuario 1", Azul, Lab, Esquema.Flechas));
                Participantes.Add(new Jugador("Usuario 2", Naranja, Lab, Esquema.Wasd));
                break;
        }
        Ganadores.Clear();
        _t0 = ahoraMs;
        TiempoSegundos = 0;
        Estado = Estado.Jugando;
    }

    /// <summary>Avanza un cuadro del juego.</summary>
    /// <param name="entrada">Dirección que pulsa cada persona en este momento (o null).</param>
    public void Actualizar(long ahoraMs, Func<Jugador, Direccion?> entrada)
    {
        if (Estado != Estado.Jugando) return;
        foreach (var p in Participantes)
            p.Actualizar(ahoraMs, p is Jugador j ? entrada(j) : null);
        TiempoSegundos = (ahoraMs - _t0) / 1000.0;

        // El primero en llegar gana; si llegan en el mismo cuadro, es empate
        foreach (var p in Participantes)
            if (p.EnMeta) Ganadores.Add(p);
        if (Ganadores.Count > 0)
            Estado = Estado.Resultado;
    }

    public void IrAlMenu() => Estado = Estado.Menu;

    /// <summary>Otra partida de la misma modalidad con un mapa nuevo.</summary>
    public void Repetir(long ahoraMs)
    {
        if (Estado != Estado.Menu) NuevaPartida(Modo, ahoraMs);
    }
}
