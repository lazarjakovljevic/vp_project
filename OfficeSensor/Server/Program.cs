using System;
using Common;
using System.ServiceModel;

namespace Server
{
    public class Program
    {
        static void Main(string[] args)
        {
            ServiceHost host = new ServiceHost(typeof(SensorService));

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