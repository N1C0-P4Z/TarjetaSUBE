using Microsoft.EntityFrameworkCore;

namespace TarjetaSUBE;

public class EstacionBicicletas
{
    public const decimal TarifaDiaria = 2418.75m;
    public const decimal MultaPorExceso = 1250m;

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

        var monto = TarifaDiaria + tarjetaGuardada.MultaPendiente;
        if (tarjetaGuardada.Saldo < monto)
            throw new ArgumentException("Saldo insuficiente.");

        tarjetaGuardada.Descontar(monto);
        tarjetaGuardada.MultaPendiente = 0;

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
        tarjeta.MultaPendiente = tarjetaGuardada.MultaPendiente;
        return alquiler;
    }

    public void Devolver(Alquiler alquiler, DateTime fecha)
    {
        if (alquiler.Fin is not null)
            throw new ArgumentException("La bicicleta ya fue devuelta.");

        if (fecha < alquiler.Inicio)
            throw new ArgumentException("La fecha de devolucion no puede ser anterior al inicio.");

        alquiler.Fin = fecha;

        if (fecha - alquiler.Inicio > TimeSpan.FromHours(1))
        {
            var tarjeta = alquiler.Tarjeta
                ?? Contexto.Db.Tarjetas.Find(alquiler.TarjetaId)
                ?? throw new ArgumentException("La tarjeta no existe.");

            tarjeta.MultaPendiente += MultaPorExceso;
        }

        Contexto.Db.SaveChanges();
    }
}