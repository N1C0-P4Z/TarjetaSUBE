using Microsoft.EntityFrameworkCore;

namespace TarjetaSUBE;

public class Tarjeta
{
    public const decimal SaldoMaximo = 56000m;
    public const decimal SaldoMinimo = -2000m;

    public static readonly decimal[] CargasAceptadas =
    [
        2000, 3000, 4000, 5000, 8000, 10000, 15000, 20000, 25000, 30000,
    ];

    public int Id { get; set; }
    public decimal Saldo { get; set; }
    public decimal SaldoPendiente { get; set; }
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
        return CalcularPrecioPasaje(tarifaBasica, DateTime.Now);
    }

    public decimal CalcularPrecioPasaje(decimal tarifaBasica, DateTime fecha)
    {
        var tipo = TarjetaTipo
            ?? Contexto.Db.TarjetasTipo.Find(TarjetaTipoId)
            ?? throw new ArgumentException("La tarjeta no tiene un tipo.");

        if (tipo.Nombre == "Franquicia completa")
        {
            var viajesHoy = ViajesEnElDia(fecha);

            if (viajesHoy < 2)
                return 0m;

            return tarifaBasica;
        }

        if (tipo.Nombre == "Medio boleto estudiantil")
        {
            if (ViajesEnElDia(fecha) >= 2)
                return tarifaBasica;

            return tipo.CalcularPrecio(tarifaBasica);
        }

        if (tipo.Nombre == "Normal")
            return PrecioConUsoFrecuente(tarifaBasica, fecha);

        return tipo.CalcularPrecio(tarifaBasica);
    }


    public void Cargar(decimal monto)
    {
        if (!CargasAceptadas.Contains(monto))
            throw new ArgumentException("El monto de carga no esta aceptado.");

        AcreditarCarga(monto);
        Contexto.Db.SaveChanges();
    }

    public void AcreditarCarga(decimal monto)
    {
        if (monto <= 0)
            return;

        var espacioDisponible = SaldoMaximo - Saldo;

        if (espacioDisponible <= 0)
        {
            SaldoPendiente += monto;
            return;
        }

        var montoAcreditado = Math.Min(monto, espacioDisponible);
        Saldo += montoAcreditado;
        SaldoPendiente += monto - montoAcreditado;
    }

    public void Descontar(decimal monto)
    {
        if (Saldo - monto < SaldoMinimo)
            throw new ArgumentException("Saldo insuficiente.");

        Saldo -= monto;
        AcreditarSaldoPendiente();
    }

    public bool CorrespondeTrasbordo(DateTime fecha, int linea)
    {
        if (!EnFranjaDeTrasbordo(fecha))
            return false;

        var anteriores = Contexto.Db.Boletos
            .Include(b => b.Colectivo)
            .Where(b => b.Tarjeta!.Id == Id && b.Fecha <= fecha)
            .ToList();

        var ancla = anteriores
            .Where(b => !b.EsTrasbordo)
            .OrderByDescending(b => b.Fecha)
            .FirstOrDefault();

        if (ancla is null || fecha - ancla.Fecha > TimeSpan.FromHours(1))
            return false;

        var lineasUsadas = anteriores
            .Where(b => b.Fecha >= ancla.Fecha)
            .Select(b => b.Linea)
            .ToHashSet();

        return !lineasUsadas.Contains(linea);
    }

    private static bool EnFranjaDeTrasbordo(DateTime fecha)
    {
        if (fecha.DayOfWeek == DayOfWeek.Sunday)
            return false;

        var hora = fecha.TimeOfDay;
        return hora >= new TimeSpan(7, 0, 0) && hora <= new TimeSpan(22, 0, 0);
    }

    public void ValidarFranjaFranquicia(DateTime fecha)
    {
        var tipo = TarjetaTipo
            ?? Contexto.Db.TarjetasTipo.Find(TarjetaTipoId);

        if (tipo is null || tipo.Nombre == "Normal")
            return;

        var dia = fecha.DayOfWeek;
        if (dia == DayOfWeek.Saturday || dia == DayOfWeek.Sunday)
            throw new ArgumentException("La franquicia solo se puede usar de lunes a viernes de 6 a 22.");

        var hora = fecha.TimeOfDay;
        if (hora < TimeSpan.FromHours(6) || hora >= TimeSpan.FromHours(22))
            throw new ArgumentException("La franquicia solo se puede usar de lunes a viernes de 6 a 22.");
    }

    public void ValidarIntervaloMedioBoleto(DateTime fecha)
    {
        var tipo = TarjetaTipo
            ?? Contexto.Db.TarjetasTipo.Find(TarjetaTipoId);

        if (tipo?.Nombre != "Medio boleto estudiantil")
            return;

        var ultimo = Contexto.Db.Boletos
            .Where(b => b.Tarjeta!.Id == Id)
            .OrderByDescending(b => b.Fecha)
            .FirstOrDefault();

        if (ultimo is not null && fecha - ultimo.Fecha < TimeSpan.FromMinutes(5))
            throw new ArgumentException("Deben pasar 5 minutos entre viajes con medio boleto.");
    }

    private decimal PrecioConUsoFrecuente(decimal tarifaBasica, DateTime fecha)
    {
        var numeroDeViaje = ViajesEnElMes(fecha) + 1;

        if (numeroDeViaje >= 80)
            return tarifaBasica * 0.75m;

        if (numeroDeViaje >= 30)
            return tarifaBasica * 0.80m;

        return tarifaBasica;
    }

    private int ViajesEnElDia(DateTime fecha)
    {
        var desde = fecha.Date;
        var hasta = desde.AddDays(1);

        return Contexto.Db.Boletos.Count(b =>
            b.Tarjeta!.Id == Id && b.Fecha >= desde && b.Fecha < hasta);
    }

    private int ViajesEnElMes(DateTime fecha)
    {
        var desde = new DateTime(fecha.Year, fecha.Month, 1);
        var hasta = desde.AddMonths(1);

        return Contexto.Db.Boletos.Count(b =>
            b.Tarjeta!.Id == Id && b.Fecha >= desde && b.Fecha < hasta);
    }

    private void AcreditarSaldoPendiente()
    {
        if (SaldoPendiente <= 0)
            return;

        var pendiente = SaldoPendiente;
        SaldoPendiente = 0;
        AcreditarCarga(pendiente);
    }
}
