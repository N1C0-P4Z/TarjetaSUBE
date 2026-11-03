using Microsoft.EntityFrameworkCore;

namespace TarjetaSUBE;

public class CobroDeViaje
{
    public static Boleto? Cobrar(Colectivo colectivo, Tarjeta tarjeta, DateTime fechaViaje)
    {
        var tarjetaGuardada = Contexto.Db.Tarjetas
            .Include(t => t.TarjetaTipo)
            .FirstOrDefault(t => t.Id == tarjeta.Id)
            ?? throw new ArgumentException("La tarjeta no existe.");

        tarjetaGuardada.ValidarFranjaFranquicia(fechaViaje);
        var esTrasbordo = tarjetaGuardada.CorrespondeTrasbordo(fechaViaje, colectivo.Linea);
        if (!esTrasbordo)
        {
            tarjetaGuardada.ValidarIntervaloMedioBoleto(fechaViaje);
        }

        var monto = esTrasbordo
            ? 0m
            : tarjetaGuardada.CalcularPrecioPasaje(colectivo.TarifaVigente(), fechaViaje);

        if (tarjetaGuardada.Saldo - monto < Tarjeta.SaldoMinimo)
        {
            throw new ArgumentException("Saldo insuficiente.");
        }

        var saldoPrevio = tarjetaGuardada.Saldo;
        tarjetaGuardada.Descontar(monto);

        var boleto = new Boleto
        {
            Fecha = fechaViaje,
            Monto = monto,
            Saldo = saldoPrevio,
            SaldoRestante = tarjetaGuardada.Saldo,
            EsTrasbordo = esTrasbordo,
            Tarjeta = tarjetaGuardada,
            Colectivo = colectivo,
        };

        Contexto.Db.Boletos.Add(boleto);
        Contexto.Db.SaveChanges();

        tarjeta.Saldo = tarjetaGuardada.Saldo;
        tarjeta.SaldoPendiente = tarjetaGuardada.SaldoPendiente;
        return boleto;
    }
}
