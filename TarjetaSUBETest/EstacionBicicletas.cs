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

    [Test]
    public void Devolver_DosExcesosElMismoDia_DejaDosMultasPendientes()
    {
        var tarjeta = Tarjeta.Crear();
        tarjeta.Cargar(10000);
        var estacion = EstacionBicicletas.Crear();
        var dia = new DateTime(2026, 10, 21, 10, 0, 0);
        var primero = estacion.Retirar(tarjeta, dia);
        var segundo = estacion.Retirar(tarjeta, dia.AddMinutes(10));
        var saldoTrasRetiros = tarjeta.Saldo;

        estacion.Devolver(primero, dia.AddHours(1).AddMinutes(1));
        estacion.Devolver(segundo, dia.AddMinutes(10).AddHours(1).AddMinutes(1));

        Assert.That(tarjeta.MultaPendiente, Is.EqualTo(EstacionBicicletas.MultaPorExceso * 2));
        Assert.That(tarjeta.Saldo, Is.EqualTo(saldoTrasRetiros));
    }

    [Test]
    public void Retirar_ConVariasMultasAcumuladas_CobraTarifaMasLasMultas()
    {
        var tarjeta = Tarjeta.Crear();
        tarjeta.Cargar(10000);
        var estacion = EstacionBicicletas.Crear();
        var dia = new DateTime(2026, 10, 21, 10, 0, 0);
        var primero = estacion.Retirar(tarjeta, dia);
        var segundo = estacion.Retirar(tarjeta, dia.AddMinutes(10));
        estacion.Devolver(primero, dia.AddHours(1).AddMinutes(1));
        estacion.Devolver(segundo, dia.AddMinutes(10).AddHours(1).AddMinutes(1));
        var saldoAntes = tarjeta.Saldo;
        var total = EstacionBicicletas.TarifaDiaria + EstacionBicicletas.MultaPorExceso * 2;

        var siguiente = estacion.Retirar(tarjeta, dia.AddHours(4));

        Assert.That(siguiente.Monto, Is.EqualTo(total));
        Assert.That(tarjeta.Saldo, Is.EqualTo(saldoAntes - total));
        Assert.That(tarjeta.MultaPendiente, Is.EqualTo(0m));
    }

    [Test]
    public void Retirar_ConMultaPendiente_LanzaExcepcion_CuandoElSaldoNoCubre()
    {
        var tarjeta = Tarjeta.Crear();
        tarjeta.Cargar(3000);
        var estacion = EstacionBicicletas.Crear();
        var inicio = new DateTime(2026, 10, 21, 10, 0, 0);
        var alquiler = estacion.Retirar(tarjeta, inicio);
        estacion.Devolver(alquiler, inicio.AddHours(1).AddMinutes(1));
        var saldoAntes = tarjeta.Saldo;

        Assert.That(
            () => estacion.Retirar(tarjeta, inicio.AddHours(3)),
            Throws.TypeOf<ArgumentException>()
                .With.Message.EqualTo("Saldo insuficiente."));
        Assert.That(tarjeta.Saldo, Is.EqualTo(saldoAntes));
        Assert.That(tarjeta.MultaPendiente, Is.EqualTo(EstacionBicicletas.MultaPorExceso));
        Assert.That(_db.Alquileres.Count(), Is.EqualTo(1));
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
    public void Retirar_ConSaldo500_RechazaYNoMueveElSaldo()
    {
        var tarjeta = Tarjeta.Crear();
        tarjeta.Saldo = 500m;
        _db.SaveChanges();
        var estacion = EstacionBicicletas.Crear();
        var fecha = new DateTime(2026, 10, 21, 10, 0, 0);

        Assert.That(
            () => estacion.Retirar(tarjeta, fecha),
            Throws.TypeOf<ArgumentException>()
                .With.Message.EqualTo("Saldo insuficiente."));
        Assert.That(tarjeta.Saldo, Is.EqualTo(500m));
        Assert.That(_db.Alquileres.Count(), Is.EqualTo(0));
    }

    [Test]
    public void Retirar_ConSaldoJusto_DescuentaLaTarifaYDejaSaldoEnCero()
    {
        var tarjeta = Tarjeta.Crear();
        tarjeta.Saldo = EstacionBicicletas.TarifaDiaria;
        _db.SaveChanges();
        var estacion = EstacionBicicletas.Crear();
        var fecha = new DateTime(2026, 10, 21, 10, 0, 0);

        var alquiler = estacion.Retirar(tarjeta, fecha);

        Assert.That(alquiler.Monto, Is.EqualTo(EstacionBicicletas.TarifaDiaria));
        Assert.That(tarjeta.Saldo, Is.EqualTo(0m));
        Assert.That(_db.Alquileres.Count(), Is.EqualTo(1));
    }

    [Test]
    public void Devolver_DosVeces_NoSumaOtraMulta()
    {
        var tarjeta = Tarjeta.Crear();
        tarjeta.Cargar(3000);
        var estacion = EstacionBicicletas.Crear();
        var inicio = new DateTime(2026, 10, 21, 10, 0, 0);
        var alquiler = estacion.Retirar(tarjeta, inicio);
        var devolucion = inicio.AddHours(1).AddMinutes(1);

        estacion.Devolver(alquiler, devolucion);

        Assert.That(
            () => estacion.Devolver(alquiler, devolucion.AddHours(2)),
            Throws.TypeOf<ArgumentException>()
                .With.Message.EqualTo("La bicicleta ya fue devuelta."));
        Assert.That(tarjeta.MultaPendiente, Is.EqualTo(EstacionBicicletas.MultaPorExceso));
        Assert.That(alquiler.Fin, Is.EqualTo(devolucion));
        Assert.That(tarjeta.Saldo, Is.EqualTo(3000m - EstacionBicicletas.TarifaDiaria));
    }
}
