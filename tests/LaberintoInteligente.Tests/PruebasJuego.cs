using LaberintoInteligente.Core;

namespace LaberintoInteligente.Tests;

/// <summary>Pruebas de la máquina de estados, las modalidades y los participantes.</summary>
public class PruebasJuego
{
    /// <summary>Dirección que acerca a un jugador a la meta por la ruta más corta.</summary>
    private static Direccion? HaciaLaMeta(Juego juego, Jugador j)
    {
        var ruta = Generador.RutaBfs(juego.Lab!, j.Pos, juego.Lab!.Meta);
        if (ruta is null || ruta.Count < 2) return null;
        int dx = ruta[1].X - j.Pos.X, dy = ruta[1].Y - j.Pos.Y;
        return (dx, dy) switch
        {
            (0, -1) => Direccion.Arriba,
            (0, 1) => Direccion.Abajo,
            (-1, 0) => Direccion.Izquierda,
            _ => Direccion.Derecha,
        };
    }

    private static long JugarHastaElFinal(Juego juego, Func<Jugador, Direccion?> entrada, long limiteMs = 120_000)
    {
        long ahora = 0;
        while (juego.Estado == Estado.Jugando && ahora < limiteMs)
        {
            ahora += 16;
            juego.Actualizar(ahora, entrada);
        }
        return ahora;
    }

    [Fact]
    public void EmpiezaEnElMenu()
    {
        Assert.Equal(Estado.Menu, new Juego().Estado);
    }

    [Theory]
    [InlineData(Modo.MaquinaVsMaquina, 2, 0)]
    [InlineData(Modo.MaquinaVsUsuario, 1, 1)]
    [InlineData(Modo.UsuarioVsUsuario, 0, 2)]
    public void CadaModalidadCreaSusParticipantes(Modo modo, int agentes, int jugadores)
    {
        var juego = new Juego();
        juego.NuevaPartida(modo, 0);
        Assert.Equal(Estado.Jugando, juego.Estado);
        Assert.Equal(agentes, juego.Participantes.OfType<Agente>().Count());
        Assert.Equal(jugadores, juego.Participantes.OfType<Jugador>().Count());
        Assert.All(juego.Participantes, p => Assert.Equal(juego.Lab!.Inicio, p.Pos));
    }

    [Fact]
    public void UsuarioVsUsuario_UsaFlechasYWasd()
    {
        var juego = new Juego();
        juego.NuevaPartida(Modo.UsuarioVsUsuario, 0);
        var esquemas = juego.Participantes.OfType<Jugador>().Select(j => j.Esquema).ToList();
        Assert.Equal(new[] { Esquema.Flechas, Esquema.Wasd }, esquemas);
    }

    [Fact]
    public void MaquinaVsMaquina_TerminaConGanador()
    {
        for (int s = 0; s < 20; s++)
        {
            var juego = new Juego(semillaFija: s);
            juego.NuevaPartida(Modo.MaquinaVsMaquina, 0);
            JugarHastaElFinal(juego, _ => null);
            Assert.Equal(Estado.Resultado, juego.Estado);
            Assert.NotEmpty(juego.Ganadores);
            Assert.All(juego.Ganadores, g => Assert.True(g.EnMeta));
        }
    }

    [Fact]
    public void MaquinaVsUsuario_ElUsuarioPuedeGanar()
    {
        var juego = new Juego(semillaFija: 22);
        juego.NuevaPartida(Modo.MaquinaVsUsuario, 0);
        JugarHastaElFinal(juego, j => HaciaLaMeta(juego, j));
        Assert.Equal(Estado.Resultado, juego.Estado);
        Assert.IsType<Jugador>(juego.Ganadores[0]);
    }

    [Fact]
    public void UsuarioVsUsuario_GanaElPrimeroEnLlegar()
    {
        var juego = new Juego(semillaFija: 33);
        juego.NuevaPartida(Modo.UsuarioVsUsuario, 0);
        // El usuario 2 no se mueve: debe ganar el usuario 1
        JugarHastaElFinal(juego, j => j.Esquema == Esquema.Flechas ? HaciaLaMeta(juego, j) : null);
        Assert.Single(juego.Ganadores);
        Assert.Equal("Usuario 1", juego.Ganadores[0].Nombre);
    }

    [Fact]
    public void LlegarAlMismoTiempoEsEmpate()
    {
        var juego = new Juego(semillaFija: 5);
        juego.NuevaPartida(Modo.UsuarioVsUsuario, 0);
        JugarHastaElFinal(juego, j => HaciaLaMeta(juego, j));
        Assert.Equal(2, juego.Ganadores.Count);
    }

    [Fact]
    public void ElJugadorNoAtraviesaMuros()
    {
        var juego = new Juego(semillaFija: 1);
        juego.NuevaPartida(Modo.UsuarioVsUsuario, 0);
        var j = juego.Participantes.OfType<Jugador>().First();
        j.Actualizar(1000, Direccion.Izquierda); // a la izquierda del inicio está el borde
        j.Actualizar(2000, Direccion.Arriba);    // arriba también
        Assert.Equal(juego.Lab!.Inicio, j.Pos);
        Assert.Equal(0, j.Pasos);
    }

    [Fact]
    public void ElJugadorRespetaElCooldown()
    {
        var juego = new Juego(semillaFija: 1);
        juego.NuevaPartida(Modo.UsuarioVsUsuario, 0);
        var j = juego.Participantes.OfType<Jugador>().First();
        var d = HaciaLaMeta(juego, j);
        j.Actualizar(1000, d);
        j.Actualizar(1000 + Jugador.CooldownMs - 1, HaciaLaMeta(juego, j));
        Assert.Equal(1, j.Pasos);
    }

    [Fact]
    public void RepetirGeneraOtroMapaYVolverAlMenu()
    {
        var juego = new Juego(semillaFija: 9);
        juego.NuevaPartida(Modo.MaquinaVsMaquina, 0);
        int primera = juego.Lab!.Semilla;
        JugarHastaElFinal(juego, _ => null);
        juego.Repetir(0);
        Assert.Equal(Estado.Jugando, juego.Estado);
        Assert.Equal(Modo.MaquinaVsMaquina, juego.Modo);
        Assert.NotEqual(primera, juego.Lab!.Semilla);
        juego.IrAlMenu();
        Assert.Equal(Estado.Menu, juego.Estado);
    }
}
