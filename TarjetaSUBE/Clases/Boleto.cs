using Microsoft.EntityFrameworkCore;

namespace TarjetaSUBE;

public class Boleto
{
    public int Id { get; set; }
    public DateTime Fecha { get; set; }
    public decimal Monto { get; set; }
    public decimal Saldo { get; set; }
    public decimal SaldoRestante { get; set; }
    public int TarjetaId { get; set; }
    public Tarjeta? Tarjeta { get; set; }
    public int ColectivoId { get; set; }
    public Colectivo? Colectivo { get; set; }

    public string TipoTarjeta => Tarjeta?.TarjetaTipo?.Nombre ?? string.Empty;
    public int Linea => Colectivo?.Linea ?? 0;
    public int IdTarjeta => Tarjeta?.Id ?? TarjetaId;
    public decimal TotalAbonado => Saldo < 0 && Monto > TarjetaSUBE.Colectivo.Tarifa
        ? Monto
        : Monto;

    public static List<Boleto> Listar()
    {
        return Contexto.Db.Boletos
            .Include(b => b.Tarjeta).ThenInclude(t => t!.TarjetaTipo)
            .Include(b => b.Colectivo)
            .ToList();
    }
}
