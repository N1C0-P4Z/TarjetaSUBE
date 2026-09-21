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
}
