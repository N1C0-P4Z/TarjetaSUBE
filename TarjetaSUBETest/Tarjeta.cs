using Microsoft.EntityFrameworkCore;
using TarjetaSUBE;

namespace TarjetaSUBETest;

public class TarjetaTests
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
    public void Crear_GuardaLaTarjetaConSaldoCero()
    {
        var tarjeta = Tarjeta.Crear();

        Assert.That(tarjeta.Id, Is.Not.EqualTo(0));
        Assert.That(tarjeta.Saldo, Is.EqualTo(0m));
        Assert.That(_db.Tarjetas.Find(tarjeta.Id), Is.Not.Null);
    }

    [TestCase(2000)]
    [TestCase(3000)]
    [TestCase(4000)]
    [TestCase(5000)]
    [TestCase(8000)]
    [TestCase(10000)]
    [TestCase(15000)]
    [TestCase(20000)]
    [TestCase(25000)]
    [TestCase(30000)]
    public void Cargar_AceptaCadaMontoPermitido(int monto)
    {
        var tarjeta = Tarjeta.Crear();

        tarjeta.Cargar(monto);

        Assert.That(tarjeta.Saldo, Is.EqualTo((decimal)monto));
    }

    [TestCase(1000)]
    [TestCase(1580)]
    [TestCase(0)]
    [TestCase(-2000)]
    public void Cargar_LanzaExcepcion_CuandoElMontoNoEstaAceptado(int monto)
    {
        var tarjeta = Tarjeta.Crear();

        Assert.That(
            () => tarjeta.Cargar(monto),
            Throws.TypeOf<ArgumentException>());
        Assert.That(tarjeta.Saldo, Is.EqualTo(0m));
    }

    [Test]
    public void Cargar_AcumulaHastaElLimiteDeSaldo()
    {
        var tarjeta = Tarjeta.Crear();

        tarjeta.Cargar(20000);
        tarjeta.Cargar(20000);

        Assert.That(tarjeta.Saldo, Is.EqualTo(Tarjeta.SaldoMaximo));
    }

    [Test]
    public void Cargar_LanzaExcepcion_CuandoSuperaElLimiteDeSaldo()
    {
        var tarjeta = Tarjeta.Crear();
        tarjeta.Cargar(30000);

        Assert.That(
            () => tarjeta.Cargar(15000),
            Throws.TypeOf<ArgumentException>());
        Assert.That(tarjeta.Saldo, Is.EqualTo(30000m));
    }

    [Test]
    public void Listar_DevuelveLasTarjetasCreadas()
    {
        Tarjeta.Crear();
        Tarjeta.Crear();

        var resultado = Tarjeta.Listar();

        Assert.That(resultado, Has.Count.EqualTo(2));
    }
}
