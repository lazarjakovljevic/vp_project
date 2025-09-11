using System;
using Common;
using System.ServiceModel;

namespace Server
{
    public class Program
    {

        static void Main(string[] args)
        {
            var sensorService = new SensorService();
            var eventSubscriber = new EventSubscriber(sensorService);

            ServiceHost host = new ServiceHost(sensorService);

            Console.CancelKeyPress += (sender, e) => 
            {
                Console.WriteLine("\nGasim server...\n");
                sensorService?.Dispose();
            };

            try
            {
                host.Open();
                ConfigurationHelper.LogConfiguration();
                Console.WriteLine("\nWCF Servis pokrenut na net.tcp://localhost:4000/SensorService");
                Console.WriteLine("Event subscriber aktivan - dogadjaji ce biti prikazani u konzoli");
                Console.WriteLine("Pritisnite bilo koji taster za zatvaranje servisa...\n");
                Console.ReadKey();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Greska pri pokretanju servisa: {ex.Message}");
                Console.WriteLine("Pritisnite bilo koji taster za zatvaranje...\n");
                Console.ReadKey();
            }
            finally
            {
                eventSubscriber?.Unsubscribe();
                sensorService?.Dispose();

                if (host?.State == CommunicationState.Opened)
                    host.Close();
            }
        }
    }
}