namespace TarjetaSUBE;

public class TarjetaTipo
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public decimal Proporcion { get; set; }
    public int? ViajesConTarifaEspecialPorDia { get; set; }
    public bool AplicaUsoFrecuente { get; set; }
    public bool TieneFranjaHoraria { get; set; }
    public int MinutosEntreViajes { get; set; }
    public List<Tarjeta> Tarjetas { get; set; } = new();

    public static List<TarjetaTipo> Listar()
    {
        return Contexto.Db.TarjetasTipo.ToList();
    }

    public static TarjetaTipo Crear(string nombre, decimal proporcion)
    {
        var tipo = Armar(nombre, proporcion, null, false, false, 0);

        CompletarReglasConocidas(tipo);
        Guardar(tipo);
        return tipo;
    }

    public static TarjetaTipo Crear(
        string nombre,
        decimal proporcion,
        int? viajesConTarifaEspecialPorDia,
        bool aplicaUsoFrecuente,
        bool tieneFranjaHoraria,
        int minutosEntreViajes)
    {
        var tipo = Armar(
            nombre,
            proporcion,
            viajesConTarifaEspecialPorDia,
            aplicaUsoFrecuente,
            tieneFranjaHoraria,
            minutosEntreViajes);

        Guardar(tipo);
        return tipo;
    }

    private static TarjetaTipo Armar(
        string nombre,
        decimal proporcion,
        int? viajesConTarifaEspecialPorDia,
        bool aplicaUsoFrecuente,
        bool tieneFranjaHoraria,
        int minutosEntreViajes)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new ArgumentException("El nombre del tipo de tarjeta es obligatorio.");
        }

        if (proporcion < 0 || proporcion > 1)
        {
            throw new ArgumentException("La proporcion debe estar entre 0 y 1.");
        }

        return new TarjetaTipo
        {
            Nombre = nombre.Trim(),
            Proporcion = proporcion,
            ViajesConTarifaEspecialPorDia = viajesConTarifaEspecialPorDia,
            AplicaUsoFrecuente = aplicaUsoFrecuente,
            TieneFranjaHoraria = tieneFranjaHoraria,
            MinutosEntreViajes = minutosEntreViajes,
        };
    }

    private static void Guardar(TarjetaTipo tipo)
    {
        Contexto.Db.TarjetasTipo.Add(tipo);
        Contexto.Db.SaveChanges();
    }

    private static void CompletarReglasConocidas(TarjetaTipo tipo)
    {
        if (tipo.Nombre == "Normal")
        {
            tipo.ViajesConTarifaEspecialPorDia = null;
            tipo.AplicaUsoFrecuente = true;
            tipo.TieneFranjaHoraria = false;
            tipo.MinutosEntreViajes = 0;
        }
        else if (tipo.Nombre == "Medio boleto estudiantil")
        {
            tipo.ViajesConTarifaEspecialPorDia = 2;
            tipo.AplicaUsoFrecuente = false;
            tipo.TieneFranjaHoraria = true;
            tipo.MinutosEntreViajes = 5;
        }
        else if (tipo.Nombre == "Boleto gratuito estudiantil")
        {
            tipo.ViajesConTarifaEspecialPorDia = 2;
            tipo.AplicaUsoFrecuente = false;
            tipo.TieneFranjaHoraria = true;
            tipo.MinutosEntreViajes = 0;
        }
        else if (tipo.Nombre == "Franquicia completa")
        {
            tipo.ViajesConTarifaEspecialPorDia = null;
            tipo.AplicaUsoFrecuente = false;
            tipo.TieneFranjaHoraria = true;
            tipo.MinutosEntreViajes = 0;
        }
    }

    public static void Sembrar()
    {
        if (Contexto.Db.TarjetasTipo.Any())
        {
            return;
        }

        Crear("Normal", 1m);
        Crear("Medio boleto estudiantil", 0.5m);
        Crear("Boleto gratuito estudiantil", 0m);
        Crear("Franquicia completa", 0m);
    }

    public decimal CalcularPrecio(decimal tarifaBasica)
    {
        return tarifaBasica * Proporcion;
    }

    public decimal CalcularPrecio(decimal tarifaBasica, int viajesEnElDia, int viajesEnElMes, DateTime fecha)
    {
        if (AplicaUsoFrecuente)
        {
            var numeroDeViaje = viajesEnElMes + 1;

            if (numeroDeViaje >= 80)
            {
                return tarifaBasica * 0.75m;
            }
            else if (numeroDeViaje >= 30)
            {
                return tarifaBasica * 0.80m;
            }
            else
            {
                return tarifaBasica;
            }
        }

        if (ViajesConTarifaEspecialPorDia is int cupo && viajesEnElDia >= cupo)
        {
            return tarifaBasica;
        }

        return tarifaBasica * Proporcion;
    }
}
