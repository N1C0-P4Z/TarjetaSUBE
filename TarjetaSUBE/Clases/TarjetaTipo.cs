namespace TarjetaSUBE;

public class TarjetaTipo
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public decimal Proporcion { get; set; }
    public List<Tarjeta> Tarjetas { get; set; } = new();

    public static List<TarjetaTipo> Listar()
    {
        return Contexto.Db.TarjetasTipo.ToList();
    }

    public static TarjetaTipo Crear(string nombre, decimal proporcion)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new ArgumentException("El nombre del tipo de tarjeta es obligatorio.");

        if (proporcion < 0 || proporcion > 1)
            throw new ArgumentException("La proporcion debe estar entre 0 y 1.");

        var tipo = new TarjetaTipo
        {
            Nombre = nombre.Trim(),
            Proporcion = proporcion,
        };

        Contexto.Db.TarjetasTipo.Add(tipo);
        Contexto.Db.SaveChanges();
        return tipo;
    }

    public static void Sembrar()
    {
        if (Contexto.Db.TarjetasTipo.Any())
            return;

        Crear("Normal", 1m);
        Crear("Medio boleto estudiantil", 0.5m);
        Crear("Boleto gratuito estudiantil", 0m);
        Crear("Franquicia completa", 0m);
    }

    public decimal CalcularPrecio(decimal tarifaBasica)
    {
        return tarifaBasica * Proporcion;
    }
}
