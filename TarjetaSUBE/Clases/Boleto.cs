using Microsoft.EntityFrameworkCore;

namespace TarjetaSUBE;

public class Boleto
{
    public int Id { get; set; }
    public DateTime Fecha { get; set; }
    public decimal Monto { get; set; }
    public decimal SaldoRestante { get; set; }
    public Tarjeta? Tarjeta { get; set; }
    public Colectivo? Colectivo { get; set; }

    public static List<Boleto> Listar()
    {
        return Contexto.Db.Boletos
            .Include(b => b.Tarjeta)
            .Include(b => b.Colectivo)
            .ToList();
    }
}
