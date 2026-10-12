using Microsoft.EntityFrameworkCore;

namespace TarjetaSUBE;

public class Tarjeta
{
    public const decimal SaldoMaximo = 40000m;

    public static readonly decimal[] CargasAceptadas =
    [
        2000, 3000, 4000, 5000, 8000, 10000, 15000, 20000, 25000, 30000,
    ];

    public int Id { get; set; }
    public decimal Saldo { get; set; }
    public int TarjetaTipoId { get; set; }
    public TarjetaTipo? TarjetaTipo { get; set; }
    public List<Boleto> Boletos { get; set; } = new();

    public static List<Tarjeta> Listar()
    {
        return Contexto.Db.Tarjetas.Include(t => t.TarjetaTipo).ToList();
    }

    public static Tarjeta Crear(int? tarjetaTipoId = null)
    {
        var tipo = tarjetaTipoId is null
            ? Contexto.Db.TarjetasTipo.FirstOrDefault(t => t.Nombre == "Normal")
            : Contexto.Db.TarjetasTipo.Find(tarjetaTipoId.Value);

        if (tipo is null && tarjetaTipoId is null)
            tipo = TarjetaTipo.Crear("Normal", 1m);
        else if (tipo is null)
            throw new ArgumentException("El tipo de tarjeta no existe.");

        var tarjeta = new Tarjeta
        {
            TarjetaTipoId = tipo!.Id,
            TarjetaTipo = tipo,
        };

        Contexto.Db.Tarjetas.Add(tarjeta);
        Contexto.Db.SaveChanges();
        return tarjeta;
    }

    public decimal CalcularPrecioPasaje(decimal tarifaBasica)
    {
        var tipo = TarjetaTipo
            ?? Contexto.Db.TarjetasTipo.Find(TarjetaTipoId)
            ?? throw new ArgumentException("La tarjeta no tiene un tipo.");

        return tipo.CalcularPrecio(tarifaBasica);
    }

    public void Cargar(decimal monto)
    {
        if (!CargasAceptadas.Contains(monto))
            throw new ArgumentException("El monto de carga no esta aceptado.");

        if (Saldo + monto > SaldoMaximo)
            throw new ArgumentException("La carga supera el limite de saldo de la tarjeta.");

        Saldo += monto;
        Contexto.Db.SaveChanges();
    }
}
