using Microsoft.EntityFrameworkCore;
using TarjetaSUBE;

namespace TarjetaSUBETest;

public class ColectivoTests
{
    private static DateTime FechaFranjaValida()
    {
        return new DateTime(2026, 10, 21, 10, 0, 0);
    }

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
        var fecha = FechaFranjaValida();
        var tipo = TarjetaTipo.Crear("Franquicia completa", 0m);
        var tarjeta = Tarjeta.Crear(tipo.Id);
        var colectivo = Colectivo.Crear(101);

        var primero = colectivo.PagarCon(tarjeta, fecha);
        var segundo = colectivo.PagarCon(tarjeta, fecha);

        Assert.That(primero.Monto, Is.EqualTo(0m));
        Assert.That(segundo.Monto, Is.EqualTo(0m));
        Assert.That(tarjeta.Saldo, Is.EqualTo(0m));
        Assert.That(_db.Boletos.Count(), Is.EqualTo(2));
    }

    [Test]
    public void PagarCon_FranquiciaCompleta_NoHaceMasDeDosViajesGratisPorDia()
    {
        var fecha = FechaFranjaValida();
        var tipo = TarjetaTipo.Crear("Franquicia completa", 0m);
        var tarjeta = Tarjeta.Crear(tipo.Id);
        tarjeta.Cargar(2000);
        var colectivo = Colectivo.Crear(101);

        colectivo.PagarCon(tarjeta, fecha);
        colectivo.PagarCon(tarjeta, fecha);
        var tercero = colectivo.PagarCon(tarjeta, fecha);

        Assert.That(tercero.Monto, Is.Not.EqualTo(0m));
        Assert.That(_db.Boletos.Count(b => b.Monto == 0m), Is.EqualTo(2));
    }

    [Test]
    public void PagarCon_FranquiciaCompleta_DelTercerViajeCobraPrecioCompleto()
    {
        var fecha = FechaFranjaValida();
        var tipo = TarjetaTipo.Crear("Franquicia completa", 0m);
        var tarjeta = Tarjeta.Crear(tipo.Id);
        tarjeta.Cargar(2000);
        var colectivo = Colectivo.Crear(101);

        colectivo.PagarCon(tarjeta, fecha);
        colectivo.PagarCon(tarjeta, fecha);
        var tercero = colectivo.PagarCon(tarjeta, fecha);

        Assert.That(tercero.Monto, Is.EqualTo(Colectivo.Tarifa));
        Assert.That(tarjeta.Saldo, Is.EqualTo(2000m - Colectivo.Tarifa));
    }

    [Test]
    public void PagarCon_MedioBoletoCobraSiempreLaMitadDelNormal()
    {
        var fecha = FechaFranjaValida();
        var normal = TarjetaTipo.Crear("Normal", 1m);
        var medio = TarjetaTipo.Crear("Medio boleto estudiantil", 0.5m);
        var tarjetaNormal = Tarjeta.Crear(normal.Id);
        var tarjetaMedio = Tarjeta.Crear(medio.Id);
        tarjetaNormal.Cargar(5000);
        tarjetaMedio.Cargar(5000);
        var colectivo = Colectivo.Crear(142);

        var boletoNormal = colectivo.PagarCon(tarjetaNormal, fecha);
        var primerMedio = colectivo.PagarCon(tarjetaMedio, fecha);
        primerMedio.Fecha = fecha.AddMinutes(-5);
        _db.SaveChanges();
        var segundoMedio = colectivo.PagarCon(tarjetaMedio, fecha);

        Assert.That(primerMedio.Monto, Is.EqualTo(boletoNormal.Monto / 2));
        Assert.That(segundoMedio.Monto, Is.EqualTo(boletoNormal.Monto / 2));
        Assert.That(tarjetaMedio.Saldo, Is.EqualTo(5000m - Colectivo.Tarifa));
    }

    [Test]
    public void PagarCon_MedioBoleto_NoPermiteOtroViajeAntesDeCincoMinutos()
    {
        var fecha = FechaFranjaValida();
        var tipo = TarjetaTipo.Crear("Medio boleto estudiantil", 0.5m);
        var tarjeta = Tarjeta.Crear(tipo.Id);
        tarjeta.Cargar(5000);
        var colectivo = Colectivo.Crear(142);

        colectivo.PagarCon(tarjeta, fecha);
        var saldo = tarjeta.Saldo;

        Assert.That(
            () => colectivo.PagarCon(tarjeta, fecha),
            Throws.TypeOf<ArgumentException>());
        Assert.That(tarjeta.Saldo, Is.EqualTo(saldo));
        Assert.That(_db.Boletos.Count(), Is.EqualTo(1));
    }

    [Test]
    public void PagarCon_MedioBoleto_PermiteViajarCuandoPasaronCincoMinutos()
    {
        var fecha = FechaFranjaValida();
        var tipo = TarjetaTipo.Crear("Medio boleto estudiantil", 0.5m);
        var tarjeta = Tarjeta.Crear(tipo.Id);
        tarjeta.Cargar(5000);
        var colectivo = Colectivo.Crear(142);

        var primero = colectivo.PagarCon(tarjeta, fecha);
        primero.Fecha = fecha.AddMinutes(-5);
        _db.SaveChanges();

        var segundo = colectivo.PagarCon(tarjeta, fecha);

        Assert.That(segundo.Monto, Is.EqualTo(Colectivo.Tarifa / 2));
        Assert.That(_db.Boletos.Count(), Is.EqualTo(2));
    }

    [Test]
    public void PagarCon_MedioBoleto_NoHaceMasDeDosViajesPorDia()
    {
        var fecha = FechaFranjaValida();
        var tipo = TarjetaTipo.Crear("Medio boleto estudiantil", 0.5m);
        var tarjeta = Tarjeta.Crear(tipo.Id);
        tarjeta.Cargar(5000);
        var colectivo = Colectivo.Crear(142);

        var primero = colectivo.PagarCon(tarjeta, fecha);
        primero.Fecha = fecha.AddMinutes(-10);
        _db.SaveChanges();
        var segundo = colectivo.PagarCon(tarjeta, fecha);
        segundo.Fecha = fecha.AddMinutes(-5);
        _db.SaveChanges();
        var tercero = colectivo.PagarCon(tarjeta, fecha);

        Assert.That(primero.Monto, Is.EqualTo(Colectivo.Tarifa / 2));
        Assert.That(segundo.Monto, Is.EqualTo(Colectivo.Tarifa / 2));
        Assert.That(tercero.Monto, Is.EqualTo(Colectivo.Tarifa));
        Assert.That(_db.Boletos.Count(b => b.Monto == Colectivo.Tarifa / 2), Is.EqualTo(2));
    }

    [Test]
    public void PagarCon_PermiteViajarQuedandoEnNegativo()
    {
        var tarjeta = Tarjeta.Crear();
        tarjeta.Saldo = 200m;
        _db.SaveChanges();
        var colectivo = Colectivo.Crear(101);

        var boleto = colectivo.PagarCon(tarjeta)!;

        Assert.That(boleto.Id, Is.Not.EqualTo(0));
        Assert.That(boleto.Monto, Is.EqualTo(Colectivo.Tarifa));
        Assert.That(tarjeta.Saldo, Is.EqualTo(200m - Colectivo.Tarifa));
        Assert.That(boleto.SaldoRestante, Is.EqualTo(tarjeta.Saldo));
        Assert.That(_db.Boletos.Count(), Is.EqualTo(1));
    }

    [Test]
    public void PagarConSaldoCeroYCargar2000()
    {
        var tarjeta = Tarjeta.Crear();
        tarjeta.Saldo = 200m;
        _db.SaveChanges();
        var colectivo = Colectivo.Crear(101);

        colectivo.PagarCon(tarjeta);
        tarjeta.Cargar(2000);

        Assert.That(tarjeta.Saldo, Is.EqualTo(50m));
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

        Assert.That(boleto1, Is.Not.Null);
        Assert.That(tarjeta.Saldo, Is.EqualTo(2000m - Colectivo.Tarifa));

        Assert.That(
            () => colectivo.PagarCon(tarjeta),
            Throws.TypeOf<ArgumentException>()
                .With.Message.EqualTo("Saldo insuficiente."));
        Assert.That(tarjeta.Saldo, Is.EqualTo(2000m - Colectivo.Tarifa));
        Assert.That(_db.Boletos.Count(), Is.EqualTo(1));
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

    [Test]
    public void PagarCon_MedioBoletoEnFranja_CobraMitadDeTarifa()
    {
        var fecha = FechaFranjaValida();
        var tipo = TarjetaTipo.Crear("Medio boleto estudiantil", 0.5m);
        var tarjeta = Tarjeta.Crear(tipo.Id);
        tarjeta.Cargar(2000);
        var colectivo = Colectivo.Crear(101);

        var boleto = colectivo.PagarCon(tarjeta, fecha);

        Assert.That(boleto!.Monto, Is.EqualTo(Colectivo.Tarifa / 2));
    }

    [Test]
    public void PagarCon_GratuitoEnFranja_MontoCero()
    {
        var fecha = FechaFranjaValida();
        var tipo = TarjetaTipo.Crear("Boleto gratuito estudiantil", 0m);
        var tarjeta = Tarjeta.Crear(tipo.Id);
        var colectivo = Colectivo.Crear(101);

        var boleto = colectivo.PagarCon(tarjeta, fecha);

        Assert.That(boleto!.Monto, Is.EqualTo(0m));
    }

    [Test]
    public void PagarCon_FranquiciaCompletaEnFranja_MontoCero()
    {
        var fecha = FechaFranjaValida();
        var tipo = TarjetaTipo.Crear("Franquicia completa", 0m);
        var tarjeta = Tarjeta.Crear(tipo.Id);
        var colectivo = Colectivo.Crear(101);

        var boleto = colectivo.PagarCon(tarjeta, fecha);

        Assert.That(boleto!.Monto, Is.EqualTo(0m));
    }

    [Test]
    public void PagarCon_Franquicia_ALasSeisPermitePagar()
    {
        var fecha = new DateTime(2026, 10, 21, 6, 0, 0);
        var tipo = TarjetaTipo.Crear("Medio boleto estudiantil", 0.5m);
        var tarjeta = Tarjeta.Crear(tipo.Id);
        tarjeta.Cargar(2000);
        var colectivo = Colectivo.Crear(101);

        var boleto = colectivo.PagarCon(tarjeta, fecha);

        Assert.That(boleto, Is.Not.Null);
    }

    [Test]
    public void PagarCon_Franquicia_AntesDeLasSeis_Rechaza()
    {
        var fecha = new DateTime(2026, 10, 21, 5, 59, 0);
        var tipo = TarjetaTipo.Crear("Medio boleto estudiantil", 0.5m);
        var tarjeta = Tarjeta.Crear(tipo.Id);
        tarjeta.Cargar(2000);
        var colectivo = Colectivo.Crear(101);
        var saldo = tarjeta.Saldo;

        Assert.That(
            () => colectivo.PagarCon(tarjeta, fecha),
            Throws.TypeOf<ArgumentException>()
                .With.Message.EqualTo("La franquicia solo se puede usar de lunes a viernes de 6 a 22."));
        Assert.That(tarjeta.Saldo, Is.EqualTo(saldo));
        Assert.That(_db.Boletos.Count(), Is.EqualTo(0));
    }

    [Test]
    public void PagarCon_Franquicia_ALasVeintidos_Rechaza()
    {
        var fecha = new DateTime(2026, 10, 21, 22, 0, 0);
        var tipo = TarjetaTipo.Crear("Medio boleto estudiantil", 0.5m);
        var tarjeta = Tarjeta.Crear(tipo.Id);
        tarjeta.Cargar(2000);
        var colectivo = Colectivo.Crear(101);
        var saldo = tarjeta.Saldo;

        Assert.That(
            () => colectivo.PagarCon(tarjeta, fecha),
            Throws.TypeOf<ArgumentException>()
                .With.Message.EqualTo("La franquicia solo se puede usar de lunes a viernes de 6 a 22."));
        Assert.That(tarjeta.Saldo, Is.EqualTo(saldo));
        Assert.That(_db.Boletos.Count(), Is.EqualTo(0));
    }

    [Test]
    public void PagarCon_Franquicia_Sabado_Rechaza()
    {
        var fecha = new DateTime(2026, 10, 24, 10, 0, 0);
        var tipo = TarjetaTipo.Crear("Medio boleto estudiantil", 0.5m);
        var tarjeta = Tarjeta.Crear(tipo.Id);
        tarjeta.Cargar(2000);
        var colectivo = Colectivo.Crear(101);
        var saldo = tarjeta.Saldo;

        Assert.That(
            () => colectivo.PagarCon(tarjeta, fecha),
            Throws.TypeOf<ArgumentException>()
                .With.Message.EqualTo("La franquicia solo se puede usar de lunes a viernes de 6 a 22."));
        Assert.That(tarjeta.Saldo, Is.EqualTo(saldo));
        Assert.That(_db.Boletos.Count(), Is.EqualTo(0));
    }

    [Test]
    public void PagarCon_Franquicia_Domingo_Rechaza()
    {
        var fecha = new DateTime(2026, 10, 25, 10, 0, 0);
        var tipo = TarjetaTipo.Crear("Medio boleto estudiantil", 0.5m);
        var tarjeta = Tarjeta.Crear(tipo.Id);
        tarjeta.Cargar(2000);
        var colectivo = Colectivo.Crear(101);
        var saldo = tarjeta.Saldo;

        Assert.That(
            () => colectivo.PagarCon(tarjeta, fecha),
            Throws.TypeOf<ArgumentException>()
                .With.Message.EqualTo("La franquicia solo se puede usar de lunes a viernes de 6 a 22."));
        Assert.That(tarjeta.Saldo, Is.EqualTo(saldo));
        Assert.That(_db.Boletos.Count(), Is.EqualTo(0));
    }

    [TestCase("Medio boleto estudiantil", 0.5)]
    [TestCase("Boleto gratuito estudiantil", 0)]
    [TestCase("Franquicia completa", 0)]
    public void PagarCon_FranquiciaFueraDeFranja_RechazaPorTipo(string nombreTipo, decimal proporcion)
    {
        var fecha = new DateTime(2026, 10, 24, 10, 0, 0);
        var tipo = TarjetaTipo.Crear(nombreTipo, proporcion);
        var tarjeta = Tarjeta.Crear(tipo.Id);
        if (nombreTipo == "Medio boleto estudiantil")
            tarjeta.Cargar(2000);
        var colectivo = Colectivo.Crear(101);
        var saldo = tarjeta.Saldo;

        Assert.That(
            () => colectivo.PagarCon(tarjeta, fecha),
            Throws.TypeOf<ArgumentException>()
                .With.Message.EqualTo("La franquicia solo se puede usar de lunes a viernes de 6 a 22."));
        Assert.That(tarjeta.Saldo, Is.EqualTo(saldo));
        Assert.That(_db.Boletos.Count(), Is.EqualTo(0));
    }

    [Test]
    public void PagarCon_NormalSabado_PagaTarifaCompleta()
    {
        var fecha = new DateTime(2026, 10, 24, 10, 0, 0);
        var tarjeta = Tarjeta.Crear();
        tarjeta.Cargar(2000);
        var colectivo = Colectivo.Crear(101);

        var boleto = colectivo.PagarCon(tarjeta, fecha);

        Assert.That(boleto!.Monto, Is.EqualTo(Colectivo.Tarifa));
        Assert.That(_db.Boletos.Count(), Is.EqualTo(1));
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

    [Test]
    public void PagarCon_Trasbordo_SegundaLineaDentroDeLaHoraNoSeCobra()
    {
        var tarjeta = TarjetaConSaldo();
        var primero = Colectivo.Crear(101);
        var segundo = Colectivo.Crear(120);
        var salida = new DateTime(2026, 10, 26, 10, 0, 0);

        var boleto = primero.PagarCon(tarjeta, salida);
        var trasbordo = segundo.PagarCon(tarjeta, salida.AddMinutes(30));

        Assert.That(boleto!.EsTrasbordo, Is.False);
        Assert.That(boleto.Monto, Is.EqualTo(Colectivo.Tarifa));
        Assert.That(trasbordo!.EsTrasbordo, Is.True);
        Assert.That(trasbordo.Monto, Is.EqualTo(0m));
        Assert.That(tarjeta.Saldo, Is.EqualTo(10000m - Colectivo.Tarifa));
    }

    [Test]
    public void PagarCon_Trasbordo_MismaLineaDentroDeLaHoraSeCobra()
    {
        var tarjeta = TarjetaConSaldo();
        var colectivo = Colectivo.Crear(101);
        var salida = new DateTime(2026, 10, 26, 10, 0, 0);

        colectivo.PagarCon(tarjeta, salida);
        var segundo = colectivo.PagarCon(tarjeta, salida.AddMinutes(30));

        Assert.That(segundo!.EsTrasbordo, Is.False);
        Assert.That(segundo.Monto, Is.EqualTo(Colectivo.Tarifa));
        Assert.That(tarjeta.Saldo, Is.EqualTo(10000m - Colectivo.Tarifa * 2));
    }

    [Test]
    public void PagarCon_Trasbordo_MasDeUnaHoraDespuesDelPrimerBoletoSeCobra()
    {
        var tarjeta = TarjetaConSaldo();
        var primero = Colectivo.Crear(101);
        var segundo = Colectivo.Crear(120);
        var salida = new DateTime(2026, 10, 26, 10, 0, 0);

        primero.PagarCon(tarjeta, salida);
        var trasbordo = segundo.PagarCon(tarjeta, salida.AddMinutes(40));
        var siguiente = Colectivo.Crear(142).PagarCon(tarjeta, salida.AddMinutes(90));

        Assert.That(trasbordo!.EsTrasbordo, Is.True);
        Assert.That(siguiente!.EsTrasbordo, Is.False);
        Assert.That(siguiente.Monto, Is.EqualTo(Colectivo.Tarifa));
        Assert.That(tarjeta.Saldo, Is.EqualTo(10000m - Colectivo.Tarifa * 2));
    }

    [Test]
    public void PagarCon_Trasbordo_NoTieneTopeDentroDeLaHoraSiLasLineasSonDistintas()
    {
        var tarjeta = TarjetaConSaldo();
        var salida = new DateTime(2026, 10, 26, 10, 0, 0);
        var lineas = new[] { 101, 120, 142, 160 };

        Boleto? ultimo = null;
        for (var i = 0; i < lineas.Length; i++)
            ultimo = Colectivo.Crear(lineas[i]).PagarCon(tarjeta, salida.AddMinutes(20 * i));

        Assert.That(ultimo!.Fecha, Is.EqualTo(salida.AddHours(1)));
        Assert.That(ultimo.EsTrasbordo, Is.True);
        Assert.That(ultimo.Monto, Is.EqualTo(0m));
        Assert.That(_db.Boletos.Count(b => b.EsTrasbordo), Is.EqualTo(3));
        Assert.That(tarjeta.Saldo, Is.EqualTo(10000m - Colectivo.Tarifa));
    }

    [Test]
    public void PagarCon_Trasbordo_FueraDeLaFranjaHorariaSeCobra()
    {
        var domingo = new DateTime(2026, 10, 25, 10, 0, 0);
        var antesDeLasSiete = new DateTime(2026, 10, 26, 6, 30, 0);
        var despuesDeLasVeintidos = new DateTime(2026, 10, 26, 21, 30, 0);

        AssertTrasbordoCobrado(domingo, domingo.AddMinutes(30));
        AssertTrasbordoCobrado(antesDeLasSiete, antesDeLasSiete.AddMinutes(20));
        AssertTrasbordoCobrado(despuesDeLasVeintidos, new DateTime(2026, 10, 26, 22, 1, 0));
    }

    [Test]
    public void PagarCon_Trasbordo_EnLosLimitesDeLaFranjaNoSeCobra()
    {
        var aLasSiete = new DateTime(2026, 10, 26, 6, 40, 0);
        var aLasVeintidos = new DateTime(2026, 10, 26, 21, 30, 0);
        var sabado = new DateTime(2026, 10, 24, 10, 0, 0);

        AssertTrasbordoGratis(aLasSiete, new DateTime(2026, 10, 26, 7, 0, 0));
        AssertTrasbordoGratis(aLasVeintidos, new DateTime(2026, 10, 26, 22, 0, 0));
        AssertTrasbordoGratis(sabado, sabado.AddMinutes(30));
    }

    [TestCase("Medio boleto estudiantil", 0.5)]
    [TestCase("Boleto gratuito estudiantil", 0)]
    [TestCase("Franquicia completa", 0)]
    public void PagarCon_Trasbordo_AplicaACualquierTipoDeTarjeta(string nombre, double proporcion)
    {
        var tipo = TarjetaTipo.Crear(nombre, (decimal)proporcion);
        var tarjeta = Tarjeta.Crear(tipo.Id);
        tarjeta.Cargar(10000);
        var salida = new DateTime(2026, 10, 26, 10, 0, 0);

        var boleto = Colectivo.Crear(101).PagarCon(tarjeta, salida);
        var trasbordo = Colectivo.Crear(120).PagarCon(tarjeta, salida.AddMinutes(30));
        var otro = Colectivo.Crear(142).PagarCon(tarjeta, salida.AddMinutes(50));

        Assert.That(boleto!.EsTrasbordo, Is.False);
        Assert.That(trasbordo!.EsTrasbordo, Is.True);
        Assert.That(trasbordo.Monto, Is.EqualTo(0m));
        Assert.That(otro!.EsTrasbordo, Is.True);
        Assert.That(otro.Monto, Is.EqualTo(0m));
    }

    private static Tarjeta TarjetaConSaldo()
    {
        var tarjeta = Tarjeta.Crear();
        tarjeta.Cargar(10000);
        return tarjeta;
    }

    private static void AssertTrasbordoCobrado(DateTime primero, DateTime segundo)
    {
        var tarjeta = TarjetaConSaldo();
        Colectivo.Crear(101).PagarCon(tarjeta, primero);
        var boleto = Colectivo.Crear(120).PagarCon(tarjeta, segundo);

        Assert.That(boleto!.EsTrasbordo, Is.False);
        Assert.That(boleto.Monto, Is.EqualTo(Colectivo.Tarifa));
    }

    private static void AssertTrasbordoGratis(DateTime primero, DateTime segundo)
    {
        var tarjeta = TarjetaConSaldo();
        Colectivo.Crear(101).PagarCon(tarjeta, primero);
        var boleto = Colectivo.Crear(120).PagarCon(tarjeta, segundo);

        Assert.That(boleto!.EsTrasbordo, Is.True);
        Assert.That(boleto.Monto, Is.EqualTo(0m));
    }
}
