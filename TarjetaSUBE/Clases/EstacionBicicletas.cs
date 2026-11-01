using Microsoft.EntityFrameworkCore;

namespace TarjetaSUBE;

public class EstacionBicicletas
{
    public const decimal TarifaDiaria = 2418.75m;

    public int Id { get; set; }
    public List<Alquiler> Alquileres { get; set; } = new();

    public static EstacionBicicletas Crear()
    {
        var estacion = new EstacionBicicletas();
        Contexto.Db.EstacionesBicicletas.Add(estacion);
        Contexto.Db.SaveChanges();
        return estacion;
    }

    public Alquiler Retirar(Tarjeta tarjeta, DateTime fecha)
    {
        var tarjetaGuardada = Contexto.Db.Tarjetas
            .Include(t => t.TarjetaTipo)
            .FirstOrDefault(t => t.Id == tarjeta.Id)
            ?? throw new ArgumentException("La tarjeta no existe.");

        var monto = TarifaDiaria;
        tarjetaGuardada.Descontar(monto);

        var alquiler = new Alquiler
        {
            Inicio = fecha,
            Monto = monto,
            Tarjeta = tarjetaGuardada,
            EstacionBicicletas = this,
        };

        Contexto.Db.Alquileres.Add(alquiler);
        Contexto.Db.SaveChanges();

        tarjeta.Saldo = tarjetaGuardada.Saldo;
        tarjeta.SaldoPendiente = tarjetaGuardada.SaldoPendiente;
        return alquiler;
    }

    public void Devolver(Alquiler alquiler, DateTime fecha)
    {
        if (fecha < alquiler.Inicio)
            throw new ArgumentException("La fecha de devolucion no puede ser anterior al inicio.");

        alquiler.Fin = fecha;
        Contexto.Db.SaveChanges();
    }
}