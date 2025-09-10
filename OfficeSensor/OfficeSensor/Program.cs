using Common;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    public class Program
    {
        static void Main(string[] args)
        {
            string csvPath = ConfigurationManager.AppSettings["csvPath"] ?? "dataset.csv";
            int maxRows = int.Parse(ConfigurationManager.AppSettings["maxRows"] ?? "100");

            Console.WriteLine("===== Office Sensor Client =====");
            Console.WriteLine($"Citam CSV: {csvPath}");
            Console.WriteLine($"Maksimalno redova: {maxRows}");

            try
            {
                using (var csvReader = new CsvReader(csvPath))
                {
                    var samples = csvReader.ReadSamples(maxRows);

                    if (samples.Count == 0)
                    {
                        Console.WriteLine("Nema validnih podataka u CSV fajlu.");
                        return;
                    }

                    // WCF klijent konfiguracija
                    var factory = new ChannelFactory<ISensorService>("SensorService");
                    var client = factory.CreateChannel();

                    try
                    {
                        var metadata = new SessionMetadata
                        {
                            Volume = samples[0].Volume,
                            RelativeHumidity = samples[0].RelativeHumidity,
                            AirQuality = samples[0].AirQuality,
                            LightLevel = samples[0].LightLevel,
                            DateTime = DateTime.Now
                        };

                        Console.WriteLine("\n=== Pokretanje sesije ===");
                        var startResponse = client.StartSession(metadata);
                        Console.WriteLine($"StartSession: {startResponse.Message}");

                        if (startResponse.Type == ResponseType.NACK)
                        {
                            Console.WriteLine("Greska pri pokretanju sesije!");
                            return;
                        }

                        Console.WriteLine("\n===== Slanje uzoraka =====");
                        for (int i = 0; i < samples.Count; i++)
                        {
                            var sample = samples[i];

                            Console.WriteLine($"Saljem uzorak {i + 1}/{samples.Count}: {sample.DateTime}");

                            var pushResponse = client.PushSample(sample);

                            if (pushResponse.Type == ResponseType.ACK)
                            {
                                Console.WriteLine($"  + {pushResponse.Message}");
                            }
                            else
                            {
                                Console.WriteLine($"  x {pushResponse.Message}");
                            }

                            System.Threading.Thread.Sleep(100);
                        }

                        Console.WriteLine("\n===== Zatvaranje sesije =====");
                        var endResponse = client.EndSession();
                        Console.WriteLine($"EndSession: {endResponse.Message}");
                    }
                    finally
                    {
                        if (client is ICommunicationObject commObj)
                        {
                            if (commObj.State == CommunicationState.Faulted)
                                commObj.Abort();
                            else
                                commObj.Close();
                        }
                        factory?.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Greska: {ex.Message}");
            }

            Console.WriteLine("\nPritisnite bilo koji taster za zatvaranje...");
            Console.ReadKey();

        }
    }
}
