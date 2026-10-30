using Microsoft.EntityFrameworkCore;
using TarjetaSUBE;

namespace TarjetaSUBETest;

public class BoletoTests
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
    public void Listar_DevuelveElBoletoConLaTarjetaYElColectivo()
    {
        var tarjeta = Tarjeta.Crear();
        tarjeta.Cargar(5000);
        var colectivo = Colectivo.Crear(153);
        colectivo.PagarCon(tarjeta);

        var resultado = Boleto.Listar();

        Assert.That(resultado, Has.Count.EqualTo(1));
        Assert.That(resultado[0].Monto, Is.EqualTo(Colectivo.Tarifa));
        Assert.That(resultado[0].Tarjeta!.Id, Is.EqualTo(tarjeta.Id));
        Assert.That(resultado[0].Colectivo!.Linea, Is.EqualTo(153));
    }

    [Test]
    public void DatosViaje_TarjetaNormal_ExponeTodosLosDatos()
    {
        var tipo = TarjetaTipo.Crear("Normal", 1m);
        var tarjeta = Tarjeta.Crear(tipo.Id);
        tarjeta.Cargar(5000);
        var colectivo = Colectivo.Crear(153);

        var saldoAntes = tarjeta.Saldo;
        var boleto = colectivo.PagarCon(tarjeta)!;

        Assert.That(boleto.Fecha.Date, Is.EqualTo(DateTime.Today));
        Assert.That(boleto.TipoTarjeta, Is.EqualTo("Normal"));
        Assert.That(boleto.Linea, Is.EqualTo(153));
        Assert.That(boleto.TotalAbonado, Is.EqualTo(Colectivo.Tarifa));
        Assert.That(boleto.Saldo, Is.EqualTo(saldoAntes));
        Assert.That(boleto.IdTarjeta, Is.EqualTo(tarjeta.Id));
        Assert.That(boleto.SaldoRestante, Is.EqualTo(tarjeta.Saldo));
    }

    [Test]
    public void DatosViaje_MedioBoletoEstudiantil_ExponeMitadDeTarifa()
    {
        var tipo = TarjetaTipo.Crear("Medio boleto estudiantil", 0.5m);
        var tarjeta = Tarjeta.Crear(tipo.Id);
        tarjeta.Cargar(5000);
        var colectivo = Colectivo.Crear(42);

        var saldoAntes = tarjeta.Saldo;
        var boleto = colectivo.PagarCon(tarjeta)!;

        Assert.That(boleto.TipoTarjeta, Is.EqualTo("Medio boleto estudiantil"));
        Assert.That(boleto.Linea, Is.EqualTo(42));
        Assert.That(boleto.TotalAbonado, Is.EqualTo(Colectivo.Tarifa * 0.5m));
        Assert.That(boleto.Saldo, Is.EqualTo(saldoAntes));
        Assert.That(boleto.IdTarjeta, Is.EqualTo(tarjeta.Id));
        Assert.That(boleto.SaldoRestante, Is.EqualTo(tarjeta.Saldo));
    }

    [Test]
    public void DatosViaje_BoletoGratuitoEstudiantil_ExponeMontoEnCero()
    {
        var tipo = TarjetaTipo.Crear("Boleto gratuito estudiantil", 0m);
        var tarjeta = Tarjeta.Crear(tipo.Id);
        var colectivo = Colectivo.Crear(100);

        var boleto = colectivo.PagarCon(tarjeta)!;

        Assert.That(boleto.TipoTarjeta, Is.EqualTo("Boleto gratuito estudiantil"));
        Assert.That(boleto.TotalAbonado, Is.EqualTo(0m));
        Assert.That(boleto.Saldo, Is.EqualTo(0m));
        Assert.That(boleto.SaldoRestante, Is.EqualTo(0m));
        Assert.That(boleto.IdTarjeta, Is.EqualTo(tarjeta.Id));
    }

    [Test]
    public void DatosViaje_FranquiciaCompleta_ExponeMontoEnCero()
    {
        var tipo = TarjetaTipo.Crear("Franquicia completa", 0m);
        var tarjeta = Tarjeta.Crear(tipo.Id);
        var colectivo = Colectivo.Crear(200);

        var boleto = colectivo.PagarCon(tarjeta)!;

        Assert.That(boleto.TipoTarjeta, Is.EqualTo("Franquicia completa"));
        Assert.That(boleto.TotalAbonado, Is.EqualTo(0m));
        Assert.That(boleto.Saldo, Is.EqualTo(0m));
        Assert.That(boleto.SaldoRestante, Is.EqualTo(0m));
        Assert.That(boleto.IdTarjeta, Is.EqualTo(tarjeta.Id));
    }

    [Test]
    public void DatosViaje_TarjetaNormalConSaldoNegativoPrevio_ExponeElSaldoPrevioYTotalAbonado()
    {
        var tipo = TarjetaTipo.Crear("Normal", 1m);
        var tarjeta = Tarjeta.Crear(tipo.Id);
        tarjeta.Saldo = 200m;
        _db.SaveChanges();
        var colectivo = Colectivo.Crear(101);

        var boleto = colectivo.PagarCon(tarjeta)!;

        Assert.That(boleto.TotalAbonado, Is.EqualTo(Colectivo.Tarifa));
        Assert.That(boleto.Saldo, Is.EqualTo(200m));
        Assert.That(boleto.SaldoRestante, Is.EqualTo(200m - Colectivo.Tarifa));
    }
}
