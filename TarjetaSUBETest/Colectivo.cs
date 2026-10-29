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
        tarjeta.Saldo = -Colectivo.Tarifa;
        _db.SaveChanges();
        var colectivo = Colectivo.Crear(101);

        Assert.That(
            () => colectivo.PagarCon(tarjeta),
            Throws.TypeOf<ArgumentException>());
        Assert.That(tarjeta.Saldo, Is.EqualTo(-Colectivo.Tarifa));
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

    [Test]
    public void PagarCon_FranquiciaCompletaSiemprePuedePagar()
    {
        var tipo = TarjetaTipo.Crear("Franquicia completa", 0m);
        var tarjeta = Tarjeta.Crear(tipo.Id);
        var colectivo = Colectivo.Crear(101);

        var primero = colectivo.PagarCon(tarjeta);
        var segundo = colectivo.PagarCon(tarjeta);

        Assert.That(primero.Monto, Is.EqualTo(0m));
        Assert.That(segundo.Monto, Is.EqualTo(0m));
        Assert.That(tarjeta.Saldo, Is.EqualTo(0m));
        Assert.That(_db.Boletos.Count(), Is.EqualTo(2));
    }

    [Test]
    public void PagarCon_FranquiciaCompleta_NoHaceMasDeDosViajesGratisPorDia()
    {
        var tipo = TarjetaTipo.Crear("Franquicia completa", 0m);
        var tarjeta = Tarjeta.Crear(tipo.Id);
        var colectivo = Colectivo.Crear(101);

        colectivo.PagarCon(tarjeta);
        colectivo.PagarCon(tarjeta);
        var tercero = colectivo.PagarCon(tarjeta);

        Assert.That(tercero.Monto, Is.Not.EqualTo(0m));
        Assert.That(_db.Boletos.Count(b => b.Monto == 0m), Is.EqualTo(2));
    }

    [Test]
    public void PagarCon_FranquiciaCompleta_DelTercerViajeCobraPrecioCompleto()
    {
        var tipo = TarjetaTipo.Crear("Franquicia completa", 0m);
        var tarjeta = Tarjeta.Crear(tipo.Id);
        var colectivo = Colectivo.Crear(101);

        colectivo.PagarCon(tarjeta);
        colectivo.PagarCon(tarjeta);
        var tercero = colectivo.PagarCon(tarjeta);

        Assert.That(tercero.Monto, Is.EqualTo(Colectivo.Tarifa));
        Assert.That(tarjeta.Saldo, Is.EqualTo(-Colectivo.Tarifa));
    }

    [Test]
    public void PagarCon_MedioBoletoCobraSiempreLaMitadDelNormal()
    {
        var normal = TarjetaTipo.Crear("Normal", 1m);
        var medio = TarjetaTipo.Crear("Medio boleto estudiantil", 0.5m);
        var tarjetaNormal = Tarjeta.Crear(normal.Id);
        var tarjetaMedio = Tarjeta.Crear(medio.Id);
        tarjetaNormal.Cargar(5000);
        tarjetaMedio.Cargar(5000);
        var colectivo = Colectivo.Crear(142);

        var boletoNormal = colectivo.PagarCon(tarjetaNormal);
        var primerMedio = colectivo.PagarCon(tarjetaMedio);
        primerMedio.Fecha = DateTime.Now.AddMinutes(-5);
        _db.SaveChanges();
        var segundoMedio = colectivo.PagarCon(tarjetaMedio);

        Assert.That(primerMedio.Monto, Is.EqualTo(boletoNormal.Monto / 2));
        Assert.That(segundoMedio.Monto, Is.EqualTo(boletoNormal.Monto / 2));
        Assert.That(tarjetaMedio.Saldo, Is.EqualTo(5000m - Colectivo.Tarifa));
    }

    [Test]
    public void PagarCon_MedioBoleto_NoPermiteOtroViajeAntesDeCincoMinutos()
    {
        var tipo = TarjetaTipo.Crear("Medio boleto estudiantil", 0.5m);
        var tarjeta = Tarjeta.Crear(tipo.Id);
        tarjeta.Cargar(5000);
        var colectivo = Colectivo.Crear(142);

        colectivo.PagarCon(tarjeta);
        var saldo = tarjeta.Saldo;

        Assert.That(
            () => colectivo.PagarCon(tarjeta),
            Throws.TypeOf<ArgumentException>());
        Assert.That(tarjeta.Saldo, Is.EqualTo(saldo));
        Assert.That(_db.Boletos.Count(), Is.EqualTo(1));
    }

    [Test]
    public void PagarCon_MedioBoleto_PermiteViajarCuandoPasaronCincoMinutos()
    {
        var tipo = TarjetaTipo.Crear("Medio boleto estudiantil", 0.5m);
        var tarjeta = Tarjeta.Crear(tipo.Id);
        tarjeta.Cargar(5000);
        var colectivo = Colectivo.Crear(142);

        var primero = colectivo.PagarCon(tarjeta);
        primero.Fecha = DateTime.Now.AddMinutes(-5);
        _db.SaveChanges();

        var segundo = colectivo.PagarCon(tarjeta);

        Assert.That(segundo.Monto, Is.EqualTo(Colectivo.Tarifa / 2));
        Assert.That(_db.Boletos.Count(), Is.EqualTo(2));
    }

    [Test]
    public void PagarCon_MedioBoleto_NoHaceMasDeDosViajesPorDia()
    {
        var tipo = TarjetaTipo.Crear("Medio boleto estudiantil", 0.5m);
        var tarjeta = Tarjeta.Crear(tipo.Id);
        tarjeta.Cargar(5000);
        var colectivo = Colectivo.Crear(142);

        var primero = colectivo.PagarCon(tarjeta);
        primero.Fecha = DateTime.Now.AddMinutes(-10);
        _db.SaveChanges();
        var segundo = colectivo.PagarCon(tarjeta);
        segundo.Fecha = DateTime.Now.AddMinutes(-5);
        _db.SaveChanges();
        var tercero = colectivo.PagarCon(tarjeta);

        Assert.That(primero.Monto, Is.EqualTo(Colectivo.Tarifa / 2));
        Assert.That(segundo.Monto, Is.EqualTo(Colectivo.Tarifa / 2));
        Assert.That(tercero.Monto, Is.EqualTo(Colectivo.Tarifa));
        Assert.That(_db.Boletos.Count(b => b.Monto == Colectivo.Tarifa / 2), Is.EqualTo(2));
    }

    [Test]
    public void PagarCon_PermiteViajarConSaldoCeroQuedandoEnNegativo()
    {
        var tarjeta = Tarjeta.Crear();
        var colectivo = Colectivo.Crear(101);

        var boleto = colectivo.PagarCon(tarjeta);

        Assert.That(boleto.Id, Is.Not.EqualTo(0));
        Assert.That(boleto.Monto, Is.EqualTo(Colectivo.Tarifa));
        Assert.That(tarjeta.Saldo, Is.EqualTo(-Colectivo.Tarifa));
        Assert.That(boleto.SaldoRestante, Is.EqualTo(tarjeta.Saldo));
        Assert.That(_db.Boletos.Count(), Is.EqualTo(1));
    }

    [Test]
    public void PagarConSaldoCeroYCargar2000()
    {
        var tarjeta = Tarjeta.Crear();
        var colectivo = Colectivo.Crear(101);

        colectivo.PagarCon(tarjeta);
        tarjeta.Cargar(2000);

        Assert.That(tarjeta.Saldo, Is.EqualTo(420m));
    }

    [Test]
    public void PagarCon_DescuentaElSaldoEnMultiplesViajes()
    {
        var tarjeta = Tarjeta.Crear();
        tarjeta.Cargar(5000);
        var colectivo = Colectivo.Crear(142);

        var boleto1 = colectivo.PagarCon(tarjeta);
        var boleto2 = colectivo.PagarCon(tarjeta);
        var boleto3 = colectivo.PagarCon(tarjeta);

        Assert.That(boleto1, Is.Not.Null);
        Assert.That(boleto2, Is.Not.Null);
        Assert.That(boleto3, Is.Not.Null);
        Assert.That(tarjeta.Saldo, Is.EqualTo(5000m - Colectivo.Tarifa * 3));
    }

    [Test]
    public void PagarCon_LanzaExcepcion_CuandoBajariaDelSaldoMinimo()
    {
        var tarjeta = Tarjeta.Crear();
        tarjeta.Cargar(2000);
        var colectivo = Colectivo.Crear(101);

        var boleto1 = colectivo.PagarCon(tarjeta);
        var boleto2 = colectivo.PagarCon(tarjeta);

        Assert.That(boleto1, Is.Not.Null);
        Assert.That(boleto2, Is.Not.Null);
        Assert.That(tarjeta.Saldo, Is.EqualTo(2000m - Colectivo.Tarifa * 2));

        Assert.That(
            () => colectivo.PagarCon(tarjeta),
            Throws.TypeOf<ArgumentException>());
        Assert.That(tarjeta.Saldo, Is.EqualTo(2000m - Colectivo.Tarifa * 2));
        Assert.That(_db.Boletos.Count(), Is.EqualTo(2));
    }

    [Test]
    public void PagarCon_UsoFrecuente_Viaje29CobraTarifaCompleta()
    {
        var tarjeta = Tarjeta.Crear();
        tarjeta.Cargar(2000);
        var colectivo = Colectivo.Crear(101);
        RegistrarViajes(tarjeta, colectivo, 28, DateTime.Now);

        var boleto = colectivo.PagarCon(tarjeta);

        Assert.That(boleto!.Monto, Is.EqualTo(Colectivo.Tarifa));
        Assert.That(tarjeta.Saldo, Is.EqualTo(2000m - Colectivo.Tarifa));
    }

    [Test]
    public void PagarCon_UsoFrecuente_Viaje30CobraVeintePorCientoMenos()
    {
        var tarjeta = Tarjeta.Crear();
        tarjeta.Cargar(2000);
        var colectivo = Colectivo.Crear(101);
        RegistrarViajes(tarjeta, colectivo, 29, DateTime.Now);

        var boleto = colectivo.PagarCon(tarjeta);

        Assert.That(boleto!.Monto, Is.EqualTo(Colectivo.Tarifa * 0.80m));
        Assert.That(tarjeta.Saldo, Is.EqualTo(2000m - Colectivo.Tarifa * 0.80m));
    }

    [Test]
    public void PagarCon_UsoFrecuente_Viaje79CobraVeintePorCientoMenos()
    {
        var tarjeta = Tarjeta.Crear();
        tarjeta.Cargar(2000);
        var colectivo = Colectivo.Crear(101);
        RegistrarViajes(tarjeta, colectivo, 78, DateTime.Now);

        var boleto = colectivo.PagarCon(tarjeta);

        Assert.That(boleto!.Monto, Is.EqualTo(Colectivo.Tarifa * 0.80m));
    }

    [Test]
    public void PagarCon_UsoFrecuente_Viaje80CobraVeinticincoPorCientoMenos()
    {
        var tarjeta = Tarjeta.Crear();
        tarjeta.Cargar(2000);
        var colectivo = Colectivo.Crear(101);
        RegistrarViajes(tarjeta, colectivo, 79, DateTime.Now);

        var boleto = colectivo.PagarCon(tarjeta);

        Assert.That(boleto!.Monto, Is.EqualTo(Colectivo.Tarifa * 0.75m));
        Assert.That(tarjeta.Saldo, Is.EqualTo(2000m - Colectivo.Tarifa * 0.75m));
    }

    [Test]
    public void PagarCon_UsoFrecuente_AlCambiarDeMesVuelveATarifaCompleta()
    {
        var tarjeta = Tarjeta.Crear();
        tarjeta.Cargar(2000);
        var colectivo = Colectivo.Crear(101);
        RegistrarViajes(tarjeta, colectivo, 80, DateTime.Now.AddMonths(-1));

        var boleto = colectivo.PagarCon(tarjeta);

        Assert.That(boleto!.Monto, Is.EqualTo(Colectivo.Tarifa));
    }

    [Test]
    public void PagarCon_UsoFrecuente_NoAplicaAMedioBoletoEstudiantil()
    {
        var tipo = TarjetaTipo.Crear("Medio boleto estudiantil", 0.5m);
        var tarjeta = Tarjeta.Crear(tipo.Id);
        tarjeta.Cargar(5000);
        var colectivo = Colectivo.Crear(101);
        var hoy = new DateTime(2026, 6, 15, 8, 0, 0);
        RegistrarViajes(tarjeta, colectivo, 40, new DateTime(2026, 6, 1, 8, 0, 0));

        Assert.That(
            tarjeta.CalcularPrecioPasaje(Colectivo.Tarifa, hoy),
            Is.EqualTo(Colectivo.Tarifa / 2));

        RegistrarViajes(tarjeta, colectivo, 2, hoy.AddMinutes(-10));

        Assert.That(
            tarjeta.CalcularPrecioPasaje(Colectivo.Tarifa, hoy),
            Is.EqualTo(Colectivo.Tarifa));
    }

    [Test]
    public void PagarCon_UsoFrecuente_NoAplicaABoletoGratuitoEstudiantil()
    {
        var tipo = TarjetaTipo.Crear("Boleto gratuito estudiantil", 0m);
        var tarjeta = Tarjeta.Crear(tipo.Id);
        var colectivo = Colectivo.Crear(101);
        RegistrarViajes(tarjeta, colectivo, 80, new DateTime(2026, 6, 1, 8, 0, 0));

        Assert.That(
            tarjeta.CalcularPrecioPasaje(Colectivo.Tarifa, new DateTime(2026, 6, 15, 8, 0, 0)),
            Is.EqualTo(0m));
    }

    [Test]
    public void PagarCon_UsoFrecuente_NoAplicaAFranquiciaCompleta()
    {
        var tipo = TarjetaTipo.Crear("Franquicia completa", 0m);
        var tarjeta = Tarjeta.Crear(tipo.Id);
        var colectivo = Colectivo.Crear(101);
        var hoy = new DateTime(2026, 6, 15, 8, 0, 0);
        RegistrarViajes(tarjeta, colectivo, 78, new DateTime(2026, 6, 1, 8, 0, 0));
        RegistrarViajes(tarjeta, colectivo, 2, hoy.AddMinutes(-10));

        Assert.That(tarjeta.CalcularPrecioPasaje(Colectivo.Tarifa, hoy), Is.EqualTo(Colectivo.Tarifa));
    }

    private static void RegistrarViajes(Tarjeta tarjeta, Colectivo colectivo, int cantidad, DateTime fecha)
    {
        for (var i = 0; i < cantidad; i++)
        {
            Contexto.Db.Boletos.Add(new Boleto
            {
                Fecha = fecha,
                Monto = Colectivo.Tarifa,
                Tarjeta = tarjeta,
                Colectivo = colectivo,
            });
        }

        Contexto.Db.SaveChanges();
    }
}
