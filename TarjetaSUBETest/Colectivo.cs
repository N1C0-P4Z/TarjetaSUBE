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

        var boleto = colectivo.PagarCon(tarjeta)!;

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

        var boleto = colectivo.PagarCon(tarjeta)!;

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

        var primero = colectivo.PagarCon(tarjeta)!;
        var segundo = colectivo.PagarCon(tarjeta)!;

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
        var tercero = colectivo.PagarCon(tarjeta)!;

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
        var tercero = colectivo.PagarCon(tarjeta)!;

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

        var boletoNormal = colectivo.PagarCon(tarjetaNormal)!;
        var primerMedio = colectivo.PagarCon(tarjetaMedio)!;
        primerMedio.Fecha = DateTime.Now.AddMinutes(-5);
        _db.SaveChanges();
        var segundoMedio = colectivo.PagarCon(tarjetaMedio)!;

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

        var primero = colectivo.PagarCon(tarjeta)!;
        primero.Fecha = DateTime.Now.AddMinutes(-5);
        _db.SaveChanges();

        var segundo = colectivo.PagarCon(tarjeta)!;

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

        var primero = colectivo.PagarCon(tarjeta)!;
        primero.Fecha = DateTime.Now.AddMinutes(-10);
        _db.SaveChanges();
        var segundo = colectivo.PagarCon(tarjeta)!;
        segundo.Fecha = DateTime.Now.AddMinutes(-5);
        _db.SaveChanges();
        var tercero = colectivo.PagarCon(tarjeta)!;

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

        var boleto = colectivo.PagarCon(tarjeta)!;

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

        var boleto1 = colectivo.PagarCon(tarjeta)!;
        var boleto2 = colectivo.PagarCon(tarjeta)!;
        var boleto3 = colectivo.PagarCon(tarjeta)!;

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

        var boleto1 = colectivo.PagarCon(tarjeta)!;
        var boleto2 = colectivo.PagarCon(tarjeta)!;

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
    public void CrearInterurbano_GuardaInterurbanaTrueYLaLinea()
    {
        var colectivo = Colectivo.CrearInterurbano(500);

        Assert.That(colectivo.Interurbana, Is.True);
        Assert.That(colectivo.Linea, Is.EqualTo(500));
        Assert.That(_db.Colectivos.Find(colectivo.Id)!.Interurbana, Is.True);
    }

    [Test]
    public void CrearInterurbano_LanzaExcepcion_CuandoLaLineaNoEsPositiva()
    {
        Assert.That(
            () => Colectivo.CrearInterurbano(0),
            Throws.TypeOf<ArgumentException>());
    }

    [Test]
    public void Crear_CreaColectivoUrbanoPorDefecto()
    {
        var colectivo = Colectivo.Crear(120);

        Assert.That(colectivo.Interurbana, Is.False);
    }

    [Test]
    public void PagarCon_NormalEnInterurbanaCobraTarifaInterurbana()
    {
        var tarjeta = Tarjeta.Crear();
        tarjeta.Cargar(5000);
        var colectivo = Colectivo.CrearInterurbano(501);

        var boleto = colectivo.PagarCon(tarjeta)!;

        Assert.That(boleto.Monto, Is.EqualTo(Colectivo.TarifaInterurbana));
        Assert.That(tarjeta.Saldo, Is.EqualTo(5000m - Colectivo.TarifaInterurbana));
    }

    [Test]
    public void PagarCon_MedioBoletoInterurbanoCobraLaMitad()
    {
        var tipo = TarjetaTipo.Crear("Medio boleto estudiantil", 0.5m);
        var tarjeta = Tarjeta.Crear(tipo.Id);
        tarjeta.Cargar(5000);
        var colectivo = Colectivo.CrearInterurbano(502);

        var primero = colectivo.PagarCon(tarjeta)!;
        primero.Fecha = DateTime.Now.AddMinutes(-5);
        _db.SaveChanges();
        var segundo = colectivo.PagarCon(tarjeta)!;

        Assert.That(primero.Monto, Is.EqualTo(Colectivo.TarifaInterurbana / 2));
        Assert.That(segundo.Monto, Is.EqualTo(Colectivo.TarifaInterurbana / 2));
    }

    [Test]
    public void PagarCon_GratuitoInterurbanoCobraCero()
    {
        var tipo = TarjetaTipo.Crear("Boleto gratuito estudiantil", 0m);
        var tarjeta = Tarjeta.Crear(tipo.Id);
        var colectivo = Colectivo.CrearInterurbano(503);

        var boleto = colectivo.PagarCon(tarjeta)!;

        Assert.That(boleto.Monto, Is.EqualTo(0m));
        Assert.That(tarjeta.Saldo, Is.EqualTo(0m));
    }

    [Test]
    public void PagarCon_FranquiciaCompletaInterurbana_DosViajesGratisTerceroTarifaInterurbana()
    {
        var tipo = TarjetaTipo.Crear("Franquicia completa", 0m);
        var tarjeta = Tarjeta.Crear(tipo.Id);
        tarjeta.Cargar(5000);
        var colectivo = Colectivo.CrearInterurbano(504);

        var primero = colectivo.PagarCon(tarjeta)!;
        var segundo = colectivo.PagarCon(tarjeta)!;
        var tercero = colectivo.PagarCon(tarjeta)!;

        Assert.That(primero.Monto, Is.EqualTo(0m));
        Assert.That(segundo.Monto, Is.EqualTo(0m));
        Assert.That(tercero.Monto, Is.EqualTo(Colectivo.TarifaInterurbana));
        Assert.That(tarjeta.Saldo, Is.EqualTo(5000m - Colectivo.TarifaInterurbana));
    }
}
