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

                        try
                        {
                            var startResponse = client.StartSession(metadata);
                            Console.WriteLine($"StartSession: {startResponse.Message}");

                            if (startResponse.Type == ResponseType.NACK)
                            {
                                Console.WriteLine("Greska pri pokretanju sesije!");
                                return;
                            }
                        }
                        catch (FaultException<ValidationFault> ex)
                        {
                            Console.WriteLine($"Validacijska greska pri pokretanju: {ex.Detail.Message}");
                            Console.WriteLine($"Polje: {ex.Detail.FieldName}, Vrednost: {ex.Detail.InvalidValue}");
                            return;
                        }
                        catch (FaultException<DataFormatFault> ex)
                        {
                            Console.WriteLine($"Format greška pri pokretanju: {ex.Detail.Message}");
                            Console.WriteLine($"Detalji: {ex.Detail.Details}");
                            return;
                        }


                        Console.WriteLine("\n=== Slanje uzoraka ===");
                        int successCount = 0;
                        int rejectCount = 0;

                        for (int i = 0; i < samples.Count; i++)
                        {
                            var sample = samples[i];

                            Console.WriteLine($"Saljem uzorak {i + 1}/{samples.Count}: {sample.DateTime}");

                            try
                            {
                                var pushResponse = client.PushSample(sample);

                                if (pushResponse.Type == ResponseType.ACK)
                                {
                                    Console.WriteLine($"   {pushResponse.Message}");
                                    successCount++;
                                }
                                else
                                {
                                    Console.WriteLine($"   {pushResponse.Message}");
                                    rejectCount++;
                                }
                            }
                            catch (FaultException<ValidationFault> ex)
                            {
                                Console.WriteLine($"    Validacijska greska: {ex.Detail.Message}");
                                Console.WriteLine($"    Polje: {ex.Detail.FieldName}");
                                Console.WriteLine($"    Vrednost: {ex.Detail.InvalidValue}");
                                Console.WriteLine($"    Ocekivani opseg: {ex.Detail.ExpectedRange}");
                                rejectCount++;
                            }
                            catch (FaultException<DataFormatFault> ex)
                            {
                                Console.WriteLine($"    Format greska: {ex.Detail.Message}");
                                Console.WriteLine($"    Polje: {ex.Detail.FieldName}");
                                Console.WriteLine($"    Detalji: {ex.Detail.Details}");
                                rejectCount++;
                            }
                            catch (FaultException ex)
                            {
                                Console.WriteLine($"    Neocekivana WCF greska: {ex.Message}");
                                rejectCount++;
                            }

                            System.Threading.Thread.Sleep(100);
                        }

                        Console.WriteLine("\n=== Zatvaranje sesije ===");
                        var endResponse = client.EndSession();
                        Console.WriteLine($"EndSession: {endResponse.Message}");


                        Console.WriteLine($"\n=== STATISTIKE ===");
                        Console.WriteLine($"Uspesno poslato: {successCount}");
                        Console.WriteLine($"Odbaceno: {rejectCount}");
                        Console.WriteLine($"Ukupno: {samples.Count}");
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
                Console.WriteLine($"Neocekivana greška: {ex.Message}");
            }

            Console.WriteLine("\nPritisnite bilo koji taster za zatvaranje...");
            Console.ReadKey();
        }
    }
}
