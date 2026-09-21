namespace TarjetaSUBE;

public class Colectivo
{
    public const decimal Tarifa = 1580m;

    public int Id { get; set; }
    public int Linea { get; set; }
    public List<Boleto> Boletos { get; set; } = new();

    public static List<Colectivo> Listar()
    {
        return Contexto.Db.Colectivos.ToList();
    }

    public static Colectivo Crear(int linea)
    {
        if (linea <= 0)
            throw new ArgumentException("La linea del colectivo debe ser mayor que cero.");

        var colectivo = new Colectivo { Linea = linea };
        Contexto.Db.Colectivos.Add(colectivo);
        Contexto.Db.SaveChanges();
        return colectivo;
    }

    public Boleto PagarCon(Tarjeta tarjeta)
    {
        var tarjetaGuardada = Contexto.Db.Tarjetas.Find(tarjeta.Id)
            ?? throw new ArgumentException("La tarjeta no existe.");

        if (tarjetaGuardada.Saldo < Tarifa)
            throw new ArgumentException("Saldo insuficiente. No se permite saldo negativo.");

        tarjetaGuardada.Saldo -= Tarifa;

        var boleto = new Boleto
        {
            Fecha = DateTime.Now,
            Monto = Tarifa,
            SaldoRestante = tarjetaGuardada.Saldo,
            Tarjeta = tarjetaGuardada,
            Colectivo = this,
        };

        Contexto.Db.Boletos.Add(boleto);
        Contexto.Db.SaveChanges();

        tarjeta.Saldo = tarjetaGuardada.Saldo;
        return boleto;
    }
}
