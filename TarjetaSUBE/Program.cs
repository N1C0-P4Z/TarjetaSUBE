using TarjetaSUBE;

Contexto.Db.Database.EnsureCreated();

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
    Console.WriteLine("0. Salir");
    Console.Write("Opcion: ");
}

void CrearTarjeta()
{
    var tarjeta = Tarjeta.Crear();
    Console.WriteLine($"Tarjeta #{tarjeta.Id} creada. Saldo: {tarjeta.Saldo:C}");
}

void CargarTarjeta()
{
    foreach (var existente in Tarjeta.Listar())
        Console.WriteLine($"#{existente.Id} - Saldo: {existente.Saldo:C}");

    Console.Write("Id de la tarjeta: ");
    var id = Convert.ToInt32(Console.ReadLine());
    var tarjeta = Contexto.Db.Tarjetas.Find(id)
        ?? throw new ArgumentException("La tarjeta no existe.");

    Console.WriteLine("Montos aceptados: " + string.Join(", ", Tarjeta.CargasAceptadas));
    Console.Write("Monto: ");
    var monto = Convert.ToDecimal(Console.ReadLine());

    tarjeta.Cargar(monto);
    Console.WriteLine($"Carga realizada. Saldo: {tarjeta.Saldo:C}");
}

void CrearColectivo()
{
    Console.Write("Linea: ");
    var linea = Convert.ToInt32(Console.ReadLine());
    var colectivo = Colectivo.Crear(linea);
    Console.WriteLine($"Colectivo #{colectivo.Id} de la linea {colectivo.Linea} creado.");
}

void PagarViaje()
{
    foreach (var existente in Colectivo.Listar())
        Console.WriteLine($"#{existente.Id} - Linea {existente.Linea}");

    Console.Write("Id del colectivo: ");
    var colectivoId = Convert.ToInt32(Console.ReadLine());
    var colectivo = Contexto.Db.Colectivos.Find(colectivoId)
        ?? throw new ArgumentException("El colectivo no existe.");

    foreach (var existenteTarjeta in Tarjeta.Listar())
        Console.WriteLine($"#{existenteTarjeta.Id} - Saldo: {existenteTarjeta.Saldo:C}");

    Console.Write("Id de la tarjeta: ");
    var tarjetaId = Convert.ToInt32(Console.ReadLine());
    var tarjeta = Contexto.Db.Tarjetas.Find(tarjetaId)
        ?? throw new ArgumentException("La tarjeta no existe.");

    var boleto = colectivo.PagarCon(tarjeta);
    Console.WriteLine($"Boleto #{boleto.Id}. Linea {colectivo.Linea}. Monto: {boleto.Monto:C}. Saldo restante: {boleto.SaldoRestante:C}");
}

void ListarBoletos()
{
    foreach (var boleto in Boleto.Listar())
        Console.WriteLine($"#{boleto.Id} - {boleto.Fecha:g} - Linea {boleto.Colectivo?.Linea} - Tarjeta #{boleto.Tarjeta?.Id} - {boleto.Monto:C} - Saldo {boleto.SaldoRestante:C}");
}
