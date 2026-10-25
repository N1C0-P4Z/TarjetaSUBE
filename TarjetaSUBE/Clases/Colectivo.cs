using Microsoft.EntityFrameworkCore;

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

    public Boleto? PagarCon(Tarjeta tarjeta)
    {
        return PagarCon(tarjeta, DateTime.Now);
    }

    public Boleto? PagarCon(Tarjeta tarjeta, DateTime fechaViaje)
    {
        var tarjetaGuardada = Contexto.Db.Tarjetas
            .Include(t => t.TarjetaTipo)
            .FirstOrDefault(t => t.Id == tarjeta.Id)
            ?? throw new ArgumentException("La tarjeta no existe.");

        tarjetaGuardada.ValidarFranjaFranquicia(fechaViaje);
        tarjetaGuardada.ValidarIntervaloMedioBoleto(fechaViaje);
        var monto = tarjetaGuardada.CalcularPrecioPasaje(Tarifa, fechaViaje);

        if (tarjetaGuardada.Saldo - monto < Tarjeta.SaldoMinimo)
            throw new ArgumentException("Saldo insuficiente.");

        var saldoPrevio = tarjetaGuardada.Saldo;
        tarjetaGuardada.Descontar(monto);

        var boleto = new Boleto
        {
            Fecha = fechaViaje,
            Monto = monto,
            Saldo = saldoPrevio,
            SaldoRestante = tarjetaGuardada.Saldo,
            Tarjeta = tarjetaGuardada,
            Colectivo = this,
        };

        Contexto.Db.Boletos.Add(boleto);
        Contexto.Db.SaveChanges();

        tarjeta.Saldo = tarjetaGuardada.Saldo;
        tarjeta.SaldoPendiente = tarjetaGuardada.SaldoPendiente;
        return boleto;
    }
}
