using Microsoft.EntityFrameworkCore;
using TarjetaSUBE;

namespace TarjetaSUBETest;

public class ColectivoTests
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
    public void Crear_GuardaElColectivoConLaLineaIndicada()
    {
        var colectivo = Colectivo.Crear(120);

        Assert.That(colectivo.Id, Is.Not.EqualTo(0));
        Assert.That(colectivo.Linea, Is.EqualTo(120));
        Assert.That(_db.Colectivos.Find(colectivo.Id), Is.Not.Null);
    }

    [Test]
    public void Crear_LanzaExcepcion_CuandoLaLineaNoEsPositiva()
    {
        Assert.That(
            () => Colectivo.Crear(0),
            Throws.TypeOf<ArgumentException>());
    }

    [Test]
    public void Listar_DevuelveLosColectivosCreados()
    {
        Colectivo.Crear(101);
        Colectivo.Crear(120);

        var resultado = Colectivo.Listar();

        Assert.That(resultado, Has.Count.EqualTo(2));
    }

    [Test]
    public void PagarCon_DescuentaLaTarifaYGeneraUnBoleto()
    {
        var tarjeta = Tarjeta.Crear();
        tarjeta.Cargar(2000);
        var colectivo = Colectivo.Crear(142);

        var boleto = colectivo.PagarCon(tarjeta);

        Assert.That(boleto.Id, Is.Not.EqualTo(0));
        Assert.That(boleto.Monto, Is.EqualTo(Colectivo.Tarifa));
        Assert.That(tarjeta.Saldo, Is.EqualTo(2000m - Colectivo.Tarifa));
        Assert.That(boleto.SaldoRestante, Is.EqualTo(tarjeta.Saldo));
        Assert.That(_db.Boletos.Find(boleto.Id), Is.Not.Null);
    }

    [Test]
    public void PagarCon_PermiteViajarCuandoElSaldoAlcanzaJusto()
    {
        var tarjeta = Tarjeta.Crear();
        tarjeta.Cargar(2000);
        tarjeta.Saldo = Colectivo.Tarifa;
        _db.SaveChanges();
        var colectivo = Colectivo.Crear(101);

        var boleto = colectivo.PagarCon(tarjeta);

        Assert.That(boleto.SaldoRestante, Is.EqualTo(0m));
        Assert.That(tarjeta.Saldo, Is.EqualTo(0m));
    }

    [Test]
    public void PagarCon_LanzaExcepcion_CuandoNoHaySaldoSuficiente()
    {
        var tarjeta = Tarjeta.Crear();
        var colectivo = Colectivo.Crear(101);

        Assert.That(
            () => colectivo.PagarCon(tarjeta),
            Throws.TypeOf<ArgumentException>());
        Assert.That(tarjeta.Saldo, Is.EqualTo(0m));
        Assert.That(_db.Boletos.Count(), Is.EqualTo(0));
    }

    [Test]
    public void PagarCon_LanzaExcepcion_CuandoLaTarjetaNoExiste()
    {
        var colectivo = Colectivo.Crear(101);
        var tarjeta = new Tarjeta { Id = 999 };

        Assert.That(
            () => colectivo.PagarCon(tarjeta),
            Throws.TypeOf<ArgumentException>());
    }
}
