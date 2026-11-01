namespace TarjetaSUBE;

public class Alquiler
{
    public int Id { get; set; }
    public DateTime Inicio { get; set; }
    public DateTime? Fin { get; set; }
    public decimal Monto { get; set; }
    public int TarjetaId { get; set; }
    public Tarjeta? Tarjeta { get; set; }
    public int EstacionBicicletasId { get; set; }
    public EstacionBicicletas? EstacionBicicletas { get; set; }
}