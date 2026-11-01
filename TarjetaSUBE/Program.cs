using Microsoft.EntityFrameworkCore;
using TarjetaSUBE;

Contexto.Db.Database.EnsureCreated();
TarjetaTipo.Sembrar();

var salir = false;
while (!salir)
{
    MostrarMenu();
    var opcion = Console.ReadLine();

    try
    {
        switch (opcion)
        {
            case "1":
                CrearTarjeta();
                break;
            case "2":
                CargarTarjeta();
                break;
            case "3":
                CrearColectivo();
                break;
            case "4":
                PagarViaje();
                break;
            case "5":
                ListarBoletos();
                break;
            case "6":
                CrearEstacionBicicletas();
                break;
            case "7":
                RetirarBici();
                break;
            case "8":
                DevolverBici();
                break;
            case "9":
                ListarAlquileres();
                break;
            case "0":
                salir = true;
                break;
            default:
                Console.WriteLine("Opcion invalida.");
                break;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
    }
}

Contexto.Db.Dispose();

void MostrarMenu()
{
    Console.WriteLine();
    Console.WriteLine("=== Transporte Rosario ===");
    Console.WriteLine("1. Crear tarjeta");
    Console.WriteLine("2. Cargar saldo");
    Console.WriteLine("3. Crear colectivo");
    Console.WriteLine("4. Pagar viaje");
    Console.WriteLine("5. Listar boletos");
    Console.WriteLine("6. Crear estacion de bicicletas");
    Console.WriteLine("7. Retirar bici");
    Console.WriteLine("8. Devolver bici");
    Console.WriteLine("9. Listar alquileres");
    Console.WriteLine("0. Salir");
    Console.Write("Opcion: ");
}

void CrearTarjeta()
{
    foreach (var tipo in TarjetaTipo.Listar())
        Console.WriteLine($"{tipo.Id} - {tipo.Nombre}");

    Console.Write("Id del tipo: ");
    var tipoId = Convert.ToInt32(Console.ReadLine());
    var tarjeta = Tarjeta.Crear(tipoId);
    Console.WriteLine($"Tarjeta #{tarjeta.Id} creada ({tarjeta.TarjetaTipo?.Nombre}). Saldo: {tarjeta.Saldo:C}");
}

void CargarTarjeta()
{
    foreach (var existente in Tarjeta.Listar())
        Console.WriteLine($"#{existente.Id} - {existente.TarjetaTipo?.Nombre} - Saldo: {existente.Saldo:C}");

    Console.Write("Id de la tarjeta: ");
    var id = Convert.ToInt32(Console.ReadLine());
    var tarjeta = Contexto.Db.Tarjetas.Find(id)
        ?? throw new ArgumentException("La tarjeta no existe.");

    Console.WriteLine("Montos aceptados: " + string.Join(", ", Tarjeta.CargasAceptadas));
    Console.Write("Monto: ");
    var monto = Convert.ToDecimal(Console.ReadLine());

    tarjeta.Cargar(monto);
    Console.WriteLine($"Carga realizada. Saldo: {tarjeta.Saldo:C}. Pendiente: {tarjeta.SaldoPendiente:C}");
}

void CrearColectivo()
{
    Console.Write("Linea: ");
    var linea = Convert.ToInt32(Console.ReadLine());
    Console.Write("Interurbana (s/n): ");
    var interurbana = Console.ReadLine()?.Trim().ToLowerInvariant() == "s";
    var colectivo = interurbana ? Colectivo.CrearInterurbano(linea) : Colectivo.Crear(linea);
    var etiqueta = colectivo.Interurbana ? " interurbana" : "";
    Console.WriteLine($"Colectivo #{colectivo.Id} de la linea {colectivo.Linea}{etiqueta} creado.");
}

void PagarViaje()
{
    foreach (var existente in Colectivo.Listar())
    {
        var etiqueta = existente.Interurbana ? " interurbana" : "";
        Console.WriteLine($"#{existente.Id} - Linea {existente.Linea}{etiqueta}");
    }

    Console.Write("Id del colectivo: ");
    var colectivoId = Convert.ToInt32(Console.ReadLine());
    var colectivo = Contexto.Db.Colectivos.Find(colectivoId)
        ?? throw new ArgumentException("El colectivo no existe.");

    foreach (var existenteTarjeta in Tarjeta.Listar())
        Console.WriteLine($"#{existenteTarjeta.Id} - {existenteTarjeta.TarjetaTipo?.Nombre} - Saldo: {existenteTarjeta.Saldo:C}");

    Console.Write("Id de la tarjeta: ");
    var tarjetaId = Convert.ToInt32(Console.ReadLine());
    var tarjeta = Contexto.Db.Tarjetas.Find(tarjetaId)
        ?? throw new ArgumentException("La tarjeta no existe.");

    var boleto = colectivo.PagarCon(tarjeta);
    Console.WriteLine($"Boleto #{boleto.Id}. Linea {colectivo.Linea}. Monto: {boleto.Monto:C}. Saldo restante: {boleto.SaldoRestante:C}. Pendiente: {tarjeta.SaldoPendiente:C}");
}

void ListarBoletos()
{
    foreach (var boleto in Boleto.Listar())
        Console.WriteLine($"#{boleto.Id} - {boleto.Fecha:g} - Linea {boleto.Colectivo?.Linea} - Tarjeta #{boleto.Tarjeta?.Id} - {boleto.Monto:C} - Saldo {boleto.SaldoRestante:C}");
}

void CrearEstacionBicicletas()
{
    var estacion = EstacionBicicletas.Crear();
    Console.WriteLine($"Estacion de bicicletas #{estacion.Id} creada.");
}

void RetirarBici()
{
    foreach (var existente in Contexto.Db.EstacionesBicicletas)
        Console.WriteLine($"#{existente.Id}");

    Console.Write("Id de la estacion: ");
    var estacionId = Convert.ToInt32(Console.ReadLine());
    var estacion = Contexto.Db.EstacionesBicicletas.Find(estacionId)
        ?? throw new ArgumentException("La estacion no existe.");

    foreach (var existenteTarjeta in Tarjeta.Listar())
        Console.WriteLine($"#{existenteTarjeta.Id} - {existenteTarjeta.TarjetaTipo?.Nombre} - Saldo: {existenteTarjeta.Saldo:C}");

    Console.Write("Id de la tarjeta: ");
    var tarjetaId = Convert.ToInt32(Console.ReadLine());
    var tarjeta = Contexto.Db.Tarjetas.Find(tarjetaId)
        ?? throw new ArgumentException("La tarjeta no existe.");

    var alquiler = estacion.Retirar(tarjeta, DateTime.Now);
    Console.WriteLine($"Alquiler #{alquiler.Id}. Monto: {EstacionBicicletas.TarifaDiaria:C}. Saldo restante: {tarjeta.Saldo:C}.");
}

void DevolverBici()
{
    foreach (var existente in Contexto.Db.Alquileres.Where(a => a.Fin == null))
        Console.WriteLine($"#{existente.Id} - Tarjeta #{existente.TarjetaId} - Estacion #{existente.EstacionBicicletasId} - Inicio {existente.Inicio:g}");

    Console.Write("Id del alquiler: ");
    var alquilerId = Convert.ToInt32(Console.ReadLine());
    var alquiler = Contexto.Db.Alquileres
        .Include(a => a.EstacionBicicletas)
        .FirstOrDefault(a => a.Id == alquilerId && a.Fin == null)
        ?? throw new ArgumentException("El alquiler no existe.");

    alquiler.EstacionBicicletas!.Devolver(alquiler, DateTime.Now);
    Console.WriteLine("Devolucion registrada. El saldo no cambia.");
}

void ListarAlquileres()
{
    foreach (var alquiler in Contexto.Db.Alquileres
                 .Include(a => a.Tarjeta)
                 .Include(a => a.EstacionBicicletas))
    {
        var fin = alquiler.Fin.HasValue ? alquiler.Fin.Value.ToString("g") : "-";
        Console.WriteLine($"#{alquiler.Id} - Tarjeta #{alquiler.Tarjeta?.Id} - Estacion #{alquiler.EstacionBicicletas?.Id} - Inicio {alquiler.Inicio:g} - Fin {fin} - {alquiler.Monto:C}");
    }
}