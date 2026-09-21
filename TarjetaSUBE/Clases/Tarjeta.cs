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
    public List<Boleto> Boletos { get; set; } = new();

    public static List<Tarjeta> Listar()
    {
        return Contexto.Db.Tarjetas.ToList();
    }

    public static Tarjeta Crear()
    {
        var tarjeta = new Tarjeta();
        Contexto.Db.Tarjetas.Add(tarjeta);
        Contexto.Db.SaveChanges();
        return tarjeta;
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
