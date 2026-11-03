using Microsoft.EntityFrameworkCore;

namespace TarjetaSUBE;

public class Colectivo
{
    public const decimal TarifaInterurbana = 3000m;
    public const decimal Tarifa = 2150m;

    public int Id { get; set; }
    public int Linea { get; set; }
    public bool Interurbana { get; set; }
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

    public static Colectivo CrearInterurbano(int linea)
    {
        if (linea <= 0)
            throw new ArgumentException("La linea del colectivo debe ser mayor que cero.");

        var colectivo = new Colectivo { Linea = linea, Interurbana = true };
        Contexto.Db.Colectivos.Add(colectivo);
        Contexto.Db.SaveChanges();
        return colectivo;
    }

    public decimal TarifaVigente()
    {
        if (Interurbana)
            return TarifaInterurbana;

        return Tarifa;
    }

    public Boleto? PagarCon(Tarjeta tarjeta)
    {
        return PagarCon(tarjeta, DateTime.Now);
    }

    public Boleto? PagarCon(Tarjeta tarjeta, DateTime fechaViaje)
    {
        return CobroDeViaje.Cobrar(this, tarjeta, fechaViaje);
    }
}
