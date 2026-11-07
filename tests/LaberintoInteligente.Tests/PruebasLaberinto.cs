using LaberintoInteligente.Core;
using Xunit.Abstractions;

namespace LaberintoInteligente.Tests;

/// <summary>Pruebas del generador y de la búsqueda en profundidad sobre 100 semillas.</summary>
public class PruebasLaberinto
{
    private static readonly int[] Semillas = Enumerable.Range(0, 100).ToArray();
    private readonly ITestOutputHelper _salida;

    public PruebasLaberinto(ITestOutputHelper salida) => _salida = salida;

    private static bool RutaValida(Laberinto lab, List<Posicion>? ruta)
    {
        if (ruta is null || ruta.Count == 0 || ruta[0] != lab.Inicio || ruta[^1] != lab.Meta) return false;
        for (int i = 1; i < ruta.Count; i++)
            if (ruta[i].DistanciaManhattan(ruta[i - 1]) != 1) return false;
        return ruta.All(lab.EsCamino);
    }

    private static string Firma(Laberinto lab)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var c in lab.Matriz) sb.Append(c);
        return sb.ToString();
    }

    [Fact]
    public void Estructura_TamanoBordeEntradaYSalida()
    {
        foreach (int s in Semillas)
        {
            var lab = Generador.Generar(20, 14, s);
            Assert.Equal(29, lab.Filas);
            Assert.Equal(41, lab.Columnas);
            for (int x = 0; x < lab.Columnas; x++)
            {
                Assert.Equal(Laberinto.Muro, lab.Matriz[0, x]);
                Assert.Equal(Laberinto.Muro, lab.Matriz[lab.Filas - 1, x]);
            }
            for (int y = 0; y < lab.Filas; y++)
            {
                Assert.Equal(Laberinto.Muro, lab.Matriz[y, 0]);
                Assert.Equal(Laberinto.Muro, lab.Matriz[y, lab.Columnas - 1]);
            }
            Assert.True(lab.EsCamino(lab.Inicio));
            Assert.True(lab.EsCamino(lab.Meta));
        }
    }

    [Fact]
    public void TodoLaberintoEsResoluble()
    {
        foreach (int s in Semillas)
            Assert.True(Generador.Validar(Generador.Generar(20, 14, s)), $"semilla {s} sin solución");
    }

    [Fact]
    public void SemillasDistintasDanMapasDistintos_YLaMismaLoReproduce()
    {
        var firmas = Semillas.Select(s => Firma(Generador.Generar(20, 14, s))).ToHashSet();
        Assert.Equal(Semillas.Length, firmas.Count);
        Assert.Equal(Firma(Generador.Generar(20, 14, 7)), Firma(Generador.Generar(20, 14, 7)));
        var sinSemilla = Enumerable.Range(0, 30).Select(_ => Firma(Generador.Generar())).ToHashSet();
        Assert.Equal(30, sinSemilla.Count);
    }

    [Fact]
    public void DfsLlegaALaMetaConRutaValida()
    {
        foreach (int s in Semillas)
        {
            var lab = Generador.Generar(20, 14, s);
            foreach (var rng in new Random?[] { null, new Random(s), new Random(s + 1) })
            {
                var (ruta, _) = BusquedaDfs.Resolver(lab, rng);
                Assert.True(RutaValida(lab, ruta), $"ruta inválida en semilla {s}");
            }
        }
    }

    [Fact]
    public void CadaPasoDelAgenteEsContiguoYSobreCamino()
    {
        foreach (int s in Semillas)
        {
            var lab = Generador.Generar(20, 14, s);
            var dfs = new BusquedaDfs(lab, new Random(s));
            var previa = dfs.Actual;
            while (dfs.Paso())
            {
                Assert.Equal(1, dfs.Actual.DistanciaManhattan(previa));
                Assert.True(lab.EsCamino(dfs.Actual));
                previa = dfs.Actual;
            }
            Assert.Equal(lab.Meta, dfs.Actual);
        }
    }

    [Fact]
    public void SinSalida_DfsYValidarLoDetectan()
    {
        var lab = Generador.Generar(5, 5, 1, ciclos: 0);
        foreach (var d in Generador.Direcciones)
        {
            var v = lab.Meta.Mover(d);
            lab.Matriz[v.Y, v.X] = Laberinto.Muro;
        }
        var (ruta, _) = BusquedaDfs.Resolver(lab);
        Assert.Null(ruta);
        Assert.False(Generador.Validar(lab));
    }

    [Fact]
    public void SinCiclosDfsEsOptimo_ConCiclosNoSiempre()
    {
        foreach (int s in Semillas)
        {
            var lab = Generador.Generar(20, 14, s, ciclos: 0);
            var (ruta, _) = BusquedaDfs.Resolver(lab);
            Assert.Equal(Generador.RutaBfs(lab, lab.Inicio, lab.Meta)!.Count, ruta!.Count);
        }
        int masLarga = 0;
        foreach (int s in Semillas)
        {
            var lab = Generador.Generar(20, 14, s);
            var (ruta, _) = BusquedaDfs.Resolver(lab, new Random(s));
            if (ruta!.Count > Generador.RutaBfs(lab, lab.Inicio, lab.Meta)!.Count) masLarga++;
        }
        Assert.True(masLarga > 0);
        _salida.WriteLine($"Con ciclos, DFS no da la ruta más corta en {masLarga}/{Semillas.Length} mapas");
    }

    [Fact]
    public void DosAgentesConDistintaSemillaExploranDistinto()
    {
        int distintos = 0;
        foreach (int s in Semillas)
        {
            var lab = Generador.Generar(20, 14, s);
            var (_, p1) = BusquedaDfs.Resolver(lab, new Random(s + 1));
            var (_, p2) = BusquedaDfs.Resolver(lab, new Random(s + 2));
            if (p1 != p2) distintos++;
        }
        Assert.True(distintos > Semillas.Length * 0.9);
        _salida.WriteLine($"Los agentes difieren en pasos en {distintos}/{Semillas.Length} mapas");
    }

    [Fact]
    public void Estadisticas_PasosDeDfsFrenteARutaOptima()
    {
        var optimas = new List<int>();
        var pasos = new List<int>();
        foreach (int s in Semillas)
        {
            var lab = Generador.Generar(20, 14, s);
            optimas.Add(Generador.RutaBfs(lab, lab.Inicio, lab.Meta)!.Count - 1);
            pasos.Add(BusquedaDfs.Resolver(lab, new Random(s + 1)).Pasos);
        }
        _salida.WriteLine($"Ruta óptima promedio: {optimas.Average():F0} casillas");
        _salida.WriteLine($"DFS promedio: {pasos.Average():F0} pasos (mín {pasos.Min()}, máx {pasos.Max()})");
        Assert.True(pasos.Average() >= optimas.Average());
    }
}
