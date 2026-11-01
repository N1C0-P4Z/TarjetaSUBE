using Microsoft.EntityFrameworkCore;
using TarjetaSUBE;

namespace TarjetaSUBETest;

public class EstacionBicicletasTests
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
    public void Retirar_DescuentaTarifaDiariaYGeneraUnAlquiler()
    {
        var tarjeta = Tarjeta.Crear();
        tarjeta.Cargar(3000);
        var estacion = EstacionBicicletas.Crear();
        var fecha = new DateTime(2026, 10, 21, 10, 0, 0);

        var alquiler = estacion.Retirar(tarjeta, fecha);

        Assert.That(alquiler.Id, Is.Not.EqualTo(0));
        Assert.That(alquiler.Monto, Is.EqualTo(EstacionBicicletas.TarifaDiaria));
        Assert.That(alquiler.Inicio, Is.EqualTo(fecha));
        Assert.That(tarjeta.Saldo, Is.EqualTo(3000m - EstacionBicicletas.TarifaDiaria));
        Assert.That(_db.Alquileres.Find(alquiler.Id), Is.Not.Null);
    }

    [Test]
    public void Devolver_AntesDeUnaHora_NoCobraDeNuevoYRegistraFin()
    {
        var tarjeta = Tarjeta.Crear();
        tarjeta.Cargar(3000);
        var estacion = EstacionBicicletas.Crear();
        var inicio = new DateTime(2026, 10, 21, 10, 0, 0);
        var alquiler = estacion.Retirar(tarjeta, inicio);
        var saldoTrasRetiro = tarjeta.Saldo;
        var devolucion = inicio.AddMinutes(45);

        estacion.Devolver(alquiler, devolucion);

        Assert.That(tarjeta.Saldo, Is.EqualTo(saldoTrasRetiro));
        Assert.That(alquiler.Fin, Is.EqualTo(devolucion));
        Assert.That(_db.Alquileres.Find(alquiler.Id)!.Fin, Is.EqualTo(devolucion));
    }

    [TestCase("Medio boleto estudiantil", 0.5)]
    [TestCase("Boleto gratuito estudiantil", 0)]
    [TestCase("Franquicia completa", 0)]
    public void Retirar_CobraTarifaDiariaCompleta_IndependienteDelTipo(string nombreTipo, decimal proporcion)
    {
        var tipo = TarjetaTipo.Crear(nombreTipo, proporcion);
        var tarjeta = Tarjeta.Crear(tipo.Id);
        tarjeta.Cargar(3000);
        var estacion = EstacionBicicletas.Crear();
        var fecha = new DateTime(2026, 10, 21, 10, 0, 0);

        var alquiler = estacion.Retirar(tarjeta, fecha);

        Assert.That(alquiler.Monto, Is.EqualTo(EstacionBicicletas.TarifaDiaria));
        Assert.That(tarjeta.Saldo, Is.EqualTo(3000m - EstacionBicicletas.TarifaDiaria));
    }

    [Test]
    public void Retirar_DomingoPorLaNoche_CobraTarifaDiaria()
    {
        var tarjeta = Tarjeta.Crear();
        tarjeta.Cargar(3000);
        var estacion = EstacionBicicletas.Crear();
        var fecha = new DateTime(2026, 10, 25, 22, 0, 0);

        var alquiler = estacion.Retirar(tarjeta, fecha);

        Assert.That(alquiler.Monto, Is.EqualTo(EstacionBicicletas.TarifaDiaria));
        Assert.That(tarjeta.Saldo, Is.EqualTo(3000m - EstacionBicicletas.TarifaDiaria));
    }

    [Test]
    public void Retirar_LanzaExcepcion_CuandoSaldoEsCero()
    {
        var tarjeta = Tarjeta.Crear();
        var estacion = EstacionBicicletas.Crear();
        var fecha = new DateTime(2026, 10, 21, 10, 0, 0);

        Assert.That(
            () => estacion.Retirar(tarjeta, fecha),
            Throws.TypeOf<ArgumentException>()
                .With.Message.EqualTo("Saldo insuficiente."));
        Assert.That(tarjeta.Saldo, Is.EqualTo(0m));
        Assert.That(_db.Alquileres.Count(), Is.EqualTo(0));
    }

    [Test]
    public void Retirar_LanzaExcepcion_CuandoSaldoEs400()
    {
        var tarjeta = Tarjeta.Crear();
        tarjeta.Saldo = 400m;
        _db.SaveChanges();
        var estacion = EstacionBicicletas.Crear();
        var fecha = new DateTime(2026, 10, 21, 10, 0, 0);

        Assert.That(
            () => estacion.Retirar(tarjeta, fecha),
            Throws.TypeOf<ArgumentException>()
                .With.Message.EqualTo("Saldo insuficiente."));
        Assert.That(tarjeta.Saldo, Is.EqualTo(400m));
        Assert.That(_db.Alquileres.Count(), Is.EqualTo(0));
    }

    [Test]
    public void Retirar_ConSaldo500_DescuentaTarifaDiariaYGeneraUnAlquiler()
    {
        var tarjeta = Tarjeta.Crear();
        tarjeta.Saldo = 500m;
        _db.SaveChanges();
        var estacion = EstacionBicicletas.Crear();
        var fecha = new DateTime(2026, 10, 21, 10, 0, 0);

        var alquiler = estacion.Retirar(tarjeta, fecha);

        Assert.That(alquiler.Monto, Is.EqualTo(EstacionBicicletas.TarifaDiaria));
        Assert.That(tarjeta.Saldo, Is.EqualTo(500m - EstacionBicicletas.TarifaDiaria));
        Assert.That(_db.Alquileres.Count(), Is.EqualTo(1));
    }
}
