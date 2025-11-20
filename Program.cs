using System;
using System.IO;
using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

class Producer
{
    static void Main()
    {
        Console.OutputEncoding = Encoding.UTF8;
        string logPath = @"D:\Архітектура та проектування програмного забезпечення\modbus_producer_log.txt";

        try
        {
            var factory = new ConnectionFactory()
            {
                HostName = "localhost",
                UserName = "iryna",
                Password = "mypassword"
            };

            using var connection = factory.CreateConnection();
            // Створення каналу для обміну повідомленнями
            using var channel = connection.CreateModel();

            // Оголошення Topic Exchange для MODBUS
            channel.ExchangeDeclare(exchange: "modbus_topic_exchange", type: "topic");

            Console.WriteLine("=== MODBUS Learning Platform - Producer ===");
            Console.WriteLine("[*] Підключено до RabbitMQ");
            Console.WriteLine();

            while (true)
            {
                Console.WriteLine("\n--- Меню MODBUS операцій ---");
                Console.WriteLine("1. Read Coils (FC 01)");
                Console.WriteLine("2. Read Discrete Inputs (FC 02)");
                Console.WriteLine("3. Read Holding Registers (FC 03)");
                Console.WriteLine("4. Read Input Registers (FC 04)");
                Console.WriteLine("5. Write Single Coil (FC 05)");
                Console.WriteLine("6. Write Single Register (FC 06)");
                Console.WriteLine("0. Вихід");
                Console.Write("\nВиберіть операцію: ");

                var choice = Console.ReadLine();
                if (choice == "0") break;

                try
                {
                    var modbusRequest = CreateModbusRequest(choice);
                    if (modbusRequest == null) continue;

                    // Серіалізація запиту в JSON
                    var messageJson = JsonSerializer.Serialize(modbusRequest);
                    var body = Encoding.UTF8.GetBytes(messageJson);

                    // Визначення routing key на основі функції
                    string routingKey = $"modbus.request.fc{modbusRequest.FunctionCode:D2}";
                    // Відправка повідомлення у RabbitMQ 
                    channel.BasicPublish(
                        exchange: "modbus_topic_exchange",
                        routingKey: routingKey,
                        basicProperties: null,
                        body: body
                    );

                    Console.WriteLine($"\n[✓] Відправлено MODBUS запит:");
                    Console.WriteLine($"    Function Code: {modbusRequest.FunctionCode}");
                    Console.WriteLine($"    Slave ID: {modbusRequest.SlaveId}");
                    Console.WriteLine($"    Address: {modbusRequest.StartAddress}");
                    Console.WriteLine($"    Routing Key: {routingKey}");

                    File.AppendAllText(logPath,
                        $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] SENT: FC={modbusRequest.FunctionCode}, " +
                        $"SlaveID={modbusRequest.SlaveId}, Addr={modbusRequest.StartAddress}, " +
                        $"RoutingKey={routingKey}\n");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[!] Помилка при відправленні: {ex.Message}");
                    File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ERROR: {ex.Message}\n");
                }
            }

            Console.WriteLine("\n[*] Завершення роботи Producer...");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[!] Помилка з'єднання з RabbitMQ: {ex.Message}");
            File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] CONNECTION ERROR: {ex}\n");
        }
    }
    //Метод для створення MODBUS-запиту на основі вибору користувача 
    static ModbusRequest CreateModbusRequest(string choice)
    {
        try
        {
            Console.Write("Введіть Slave ID (1-247): ");
            byte slaveId = byte.Parse(Console.ReadLine() ?? "1");

            Console.Write("Введіть початкову адресу (0-65535): ");
            ushort startAddress = ushort.Parse(Console.ReadLine() ?? "0");

            ModbusRequest request = new ModbusRequest
            {
                SlaveId = slaveId,
                StartAddress = startAddress,
                Timestamp = DateTime.Now
            };

            switch (choice)
            {
                case "1": // Read Coils
                    request.FunctionCode = 1;
                    Console.Write("Кількість котушок для зчитування: ");
                    request.Quantity = ushort.Parse(Console.ReadLine() ?? "1");
                    break;

                case "2": // Read Discrete Inputs
                    request.FunctionCode = 2;
                    Console.Write("Кількість дискретних входів: ");
                    request.Quantity = ushort.Parse(Console.ReadLine() ?? "1");
                    break;

                case "3": // Read Holding Registers
                    request.FunctionCode = 3;
                    Console.Write("Кількість регістрів для зчитування: ");
                    request.Quantity = ushort.Parse(Console.ReadLine() ?? "1");
                    break;

                case "4": // Read Input Registers
                    request.FunctionCode = 4;
                    Console.Write("Кількість вхідних регістрів: ");
                    request.Quantity = ushort.Parse(Console.ReadLine() ?? "1");
                    break;

                case "5": // Write Single Coil
                    request.FunctionCode = 5;
                    Console.Write("Значення (0 або 1): ");
                    request.Value = ushort.Parse(Console.ReadLine() ?? "0");
                    break;

                case "6": // Write Single Register
                    request.FunctionCode = 6;
                    Console.Write("Значення (0-65535): ");
                    request.Value = ushort.Parse(Console.ReadLine() ?? "0");
                    break;

                default:
                    Console.WriteLine("[!] Невірний вибір!");
                    return null;
            }

            return request;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[!] Помилка введення: {ex.Message}");
            return null;
        }
    }
}

// Клас для представлення MODBUS запиту
class ModbusRequest
{
    public byte SlaveId { get; set; }
    public byte FunctionCode { get; set; }
    public ushort StartAddress { get; set; }
    public ushort Quantity { get; set; }
    public ushort Value { get; set; }
    public DateTime Timestamp { get; set; }
}