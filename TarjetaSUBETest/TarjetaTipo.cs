using Microsoft.EntityFrameworkCore;
using TarjetaSUBE;

namespace TarjetaSUBETest;

public class TarjetaTipoTests
{
    private SubeDbContext _db = null!;

    [SetUp]
    public void Setup()
    {
        var opciones = new DbContextOptionsBuilder<SubeDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new SubeDbContext(opciones);
        Contexto.Db = _db;
    }

    [TearDown]
    public void TearDown()
    {
        _db.Dispose();
    }

    [Test]
    public void Crear_GuardaElTipoConSuProporcion()
    {
        var tipo = TarjetaTipo.Crear("Universitario", 0.5m);

        Assert.That(tipo.Id, Is.Not.EqualTo(0));
        Assert.That(_db.TarjetasTipo.Find(tipo.Id)!.Proporcion, Is.EqualTo(0.5m));
    }

    [Test]
    public void Crear_LanzaExcepcion_CuandoElNombreEstaVacio()
    {
        Assert.That(
            () => TarjetaTipo.Crear("  ", 1m),
            Throws.TypeOf<ArgumentException>());
    }

    [Test]
    public void Crear_LanzaExcepcion_CuandoLaProporcionEstaFueraDeRango()
    {
        Assert.That(
            () => TarjetaTipo.Crear("Normal", 1.5m),
            Throws.TypeOf<ArgumentException>());
    }

    [Test]
    public void Sembrar_CreaLosCuatroTiposSinDuplicar()
    {
        TarjetaTipo.Sembrar();
        TarjetaTipo.Sembrar();

        var nombres = TarjetaTipo.Listar().Select(t => t.Nombre).ToList();

        Assert.That(nombres, Is.EquivalentTo(new[]
        {
            "Normal",
            "Medio boleto estudiantil",
            "Boleto gratuito estudiantil",
            "Franquicia completa",
        }));
    }

    [TestCase("Normal", 1, 2150)]
    [TestCase("Medio boleto estudiantil", 0.5, 1075)]
    [TestCase("Boleto gratuito estudiantil", 0, 0)]
    [TestCase("Franquicia completa", 0, 0)]
    [TestCase("Universitario", 0.5, 1075)]
    public void CalcularPrecio_UsaLaProporcionDelTipo(string nombre, double proporcion, int precioEsperado)
    {
        var tipo = TarjetaTipo.Crear(nombre, (decimal)proporcion);

        Assert.That(tipo.CalcularPrecio(Colectivo.Tarifa), Is.EqualTo((decimal)precioEsperado));
    }
}
