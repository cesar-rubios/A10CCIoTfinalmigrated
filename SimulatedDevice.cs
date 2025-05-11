using System;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace simulated_device
{
    class SimulatedDevice
    {
        private static DeviceClient s_deviceClient;
        private readonly static string s_connectionString = Environment.GetEnvironmentVariable("S_CONNECTION");

        private static int s_telemetryInterval = 60; // segundos

        private static async Task<DeviceClient> ConnectIoTHubWithRetriesAsync(int maxRetries = 5, int delayMilliseconds = 2000)
        {
            int attempt = 0;
            while (true)
            {
                attempt++;
                try
                {
                    Console.WriteLine($"Intento {attempt}: Conectando al IoT Hub...");
                    // Intenta crear el DeviceClient usando el protocolo MQTT.
                    var deviceClient = DeviceClient.CreateFromConnectionString(s_connectionString, TransportType.Mqtt);

                    // Opcionalmente, inicias algún método para probar la conexión, por ejemplo habilitando un método directo.
                    await deviceClient.SetMethodHandlerAsync("SetTelemetryInterval", SetTelemetryInterval, null);

                    Console.WriteLine("Conexión establecida correctamente");
                    return deviceClient;
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Fallo en el intento {attempt}: {ex.Message}");
                    Console.ResetColor();
                    if (attempt >= maxRetries)
                    {
                        Console.WriteLine("Excedido el número máximo de reintentos. Abortando...");
                        throw; // o manejarlo según la lógica de la aplicación
                    }
                    Console.WriteLine($"Esperando {delayMilliseconds} ms antes de reintentar...");
                    await Task.Delay(delayMilliseconds);
                    // Incrementar el delay para el siguiente intento si se desea un backoff exponencial
                    delayMilliseconds *= 2;
                }
            }
        }

        // Método para manejar la llamada directa (direct method)
        private static Task<MethodResponse> SetTelemetryInterval(MethodRequest methodRequest, object userContext)
        {
            var data = Encoding.UTF8.GetString(methodRequest.Data);
            if (Int32.TryParse(data, out s_telemetryInterval))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Telemetry interval set to {0} seconds", data);
                Console.ResetColor();

                string result = "{\"result\":\"Executed direct method: " + methodRequest.Name + "\"}";
                return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(result), 200));
            }
            else
            {
                string result = "{\"result\":\"Invalid parameter\"}";
                return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(result), 400));
            }
        }

        // Método asíncrono para enviar telemetría simulada
        private static async Task SendDeviceToCloudMessagesAsync()
        {
            // Inicializamos el generador de números aleatorios.
            Random rand = new Random();

            // nivel de glucosa en la sangre [70, 140] mg/dL
            // concentración de dióxido de carbono al final de la espiración [35.0, 45.0] mmHg
            // índice o recuento de episodios de arritmia detectados [0, 5]
            
            while (true)
            {
                // Generar los valores aleatorios dentro de los rangos indicados.
                double bloodGlucose = 70 + rand.NextDouble() * (140 - 70); // [70, 140] mg/dL
                double endTidalCO2 = 35.0 + rand.NextDouble() * (45.0 - 35.0); // [35.0, 45.0] mmHg
                int arrhythmiaIndex = rand.Next(0, 6); // [0, 5]
				bool isSimulated = true;//always true

                // Construimos el objeto de telemetría únicamente con los nuevos parámetros.
                var telemetryDataPoint = new
                {
					isSimulated = isSimulated,
                    bloodGlucose = bloodGlucose,
                    endTidalCO2 = endTidalCO2,
                    arrhythmiaIndex = arrhythmiaIndex
                };

                // Convertir el objeto a JSON.
                var messageString = JsonConvert.SerializeObject(telemetryDataPoint);
                var message = new Message(Encoding.ASCII.GetBytes(messageString));

                // Enviar el mensaje de telemetría al IoT Hub.
                await s_deviceClient.SendEventAsync(message);
                Console.WriteLine("{0} > Enviando mensaje: {1}", DateTime.Now, messageString);

                // Esperar el intervalo definido antes de enviar el siguiente mensaje.
                await Task.Delay(s_telemetryInterval * 1000);
            }
        }

        private static async Task Main(string[] args)
        {
            Console.WriteLine("IoT Hub Quickstarts #2 - Dispositivo simulado. Ctrl-C para salir.\n");

            // Intentar conectar con reintentos
            s_deviceClient = await ConnectIoTHubWithRetriesAsync();

            // Se inicia el envío de mensajes
            var sendTask = SendDeviceToCloudMessagesAsync();

            // He tenido que modificar esta línea para que el programa no termine inmediatamente.
            // en caso de dejar Console.ReadLine() el programa se cierra y docker cierra el contenedor.
            await Task.Delay(Timeout.Infinite);
        }
    }
}