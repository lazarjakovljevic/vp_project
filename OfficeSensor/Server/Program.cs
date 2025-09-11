using System;
using Common;
using System.ServiceModel;

namespace Server
{
    public class Program
    {
        private static SensorService service;

        static void Main(string[] args)
        {
            ServiceHost host = new ServiceHost(typeof(SensorService));

            service = new SensorService();

            Console.CancelKeyPress += (sender, e) => {
                Console.WriteLine("\nGasim server...");
                service?.Dispose(); 
            };

            try
            {
                host.Open();
                Console.WriteLine("WCF Servis pokrenut na net.tcp://localhost:4000/SensorService");
                Console.WriteLine("Pritisnite bilo koji taster za zatvaranje servisa...");
                Console.ReadKey();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Greska pri pokretanju servisa: {ex.Message}");
                Console.WriteLine("Pritisnite bilo koji taster za zatvaranje...");
                Console.ReadKey();
            }
            finally
            {
                if (host?.State == CommunicationState.Opened)
                    host.Close();
            }
        }
    }
}