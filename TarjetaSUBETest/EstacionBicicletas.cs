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
        Assert.That(tarjeta.MultaPendiente, Is.EqualTo(0m));
        Assert.That(alquiler.Fin, Is.EqualTo(devolucion));
        Assert.That(_db.Alquileres.Find(alquiler.Id)!.Fin, Is.EqualTo(devolucion));
    }

    [Test]
    public void Devolver_DentroDeUnaHora_NoGeneraMulta()
    {
        var tarjeta = Tarjeta.Crear();
        tarjeta.Cargar(3000);
        var estacion = EstacionBicicletas.Crear();
        var inicio = new DateTime(2026, 10, 21, 10, 0, 0);
        var alquiler = estacion.Retirar(tarjeta, inicio);

        estacion.Devolver(alquiler, inicio.AddHours(1));

        Assert.That(tarjeta.MultaPendiente, Is.EqualTo(0m));
        Assert.That(tarjeta.Saldo, Is.EqualTo(3000m - EstacionBicicletas.TarifaDiaria));
    }

    [Test]
    public void Devolver_DespuesDeUnaHora_DejaMultaPendiente()
    {
        var tarjeta = Tarjeta.Crear();
        tarjeta.Cargar(3000);
        var estacion = EstacionBicicletas.Crear();
        var inicio = new DateTime(2026, 10, 21, 10, 0, 0);
        var alquiler = estacion.Retirar(tarjeta, inicio);
        var saldoTrasRetiro = tarjeta.Saldo;

        estacion.Devolver(alquiler, inicio.AddHours(1).AddMinutes(1));

        Assert.That(tarjeta.MultaPendiente, Is.EqualTo(EstacionBicicletas.MultaPorExceso));
        Assert.That(tarjeta.Saldo, Is.EqualTo(saldoTrasRetiro));
    }

    [Test]
    public void Retirar_ConMultaAcumulada_CobraTarifaMasLaMulta()
    {
        var tarjeta = Tarjeta.Crear();
        tarjeta.Cargar(10000);
        var estacion = EstacionBicicletas.Crear();
        var inicio = new DateTime(2026, 10, 21, 10, 0, 0);
        var alquiler = estacion.Retirar(tarjeta, inicio);
        estacion.Devolver(alquiler, inicio.AddHours(1).AddMinutes(1));
        var saldoAntes = tarjeta.Saldo;
        var total = EstacionBicicletas.TarifaDiaria + EstacionBicicletas.MultaPorExceso;

        var siguiente = estacion.Retirar(tarjeta, inicio.AddHours(3));

        Assert.That(siguiente.Monto, Is.EqualTo(total));
        Assert.That(tarjeta.Saldo, Is.EqualTo(saldoAntes - total));
        Assert.That(tarjeta.MultaPendiente, Is.EqualTo(0m));
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
}