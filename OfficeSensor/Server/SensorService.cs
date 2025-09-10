using Common;
using System;
using System.IO;
using System.ServiceModel;

namespace Server
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class SensorService : ISensorService
    {
        private string currentSessionFile;
        private string rejectsFile;
        private StreamWriter sessionWriter;
        private StreamWriter rejectsWriter;
        private bool sessionActive = false;

        public ServiceResponse StartSession(SessionMetadata metadata)
        {
            try
            {
                // ZADATAK 6: Kreiranje measurements_session.csv strukture
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                currentSessionFile = $"measurements_{timestamp}.csv";
                rejectsFile = $"rejects_{timestamp}.csv";

                sessionWriter = new StreamWriter(currentSessionFile);
                rejectsWriter = new StreamWriter(rejectsFile);

                sessionWriter.WriteLine("Volume,RelativeHumidity,AirQuality,LightLevel,DateTime");
                rejectsWriter.WriteLine("Volume,RelativeHumidity,AirQuality,LightLevel,DateTime,RejectReason");

                sessionActive = true;
                Console.WriteLine($"Sesija pokrenuta: {currentSessionFile}");

                return new ServiceResponse
                {
                    Type = ResponseType.ACK,
                    Status = ResponseStatus.IN_PROGRESS,
                    Message = "Sesija uspesno pokrenuta"
                };
            }
            catch (Exception ex)
            {
                return new ServiceResponse
                {
                    Type = ResponseType.NACK,
                    Status = ResponseStatus.COMPLETED,
                    Message = $"Greska pri pokretanju sesije: {ex.Message}"
                };
            }
        }

        public ServiceResponse PushSample(SensorSample sample)
        {
            if (!sessionActive || sessionWriter == null)
            {
                return new ServiceResponse
                {
                    Type = ResponseType.NACK,
                    Status = ResponseStatus.COMPLETED,
                    Message = "Sesija nije aktivna"
                };
            }

            try
            {
                // ZADATAK 3: Kompletna validacija podataka
                string validationError = ValidateSample(sample);
                if (!string.IsNullOrEmpty(validationError))
                {
                    rejectsWriter.WriteLine($"{sample.Volume},{sample.RelativeHumidity},{sample.AirQuality},{sample.LightLevel},{sample.DateTime},{validationError}");
                    rejectsWriter.Flush();

                    return new ServiceResponse
                    {
                        Type = ResponseType.NACK,
                        Status = ResponseStatus.IN_PROGRESS,
                        Message = $"Uzorak odbacen: {validationError}"
                    };
                }

                sessionWriter.WriteLine($"{sample.Volume},{sample.RelativeHumidity},{sample.AirQuality},{sample.LightLevel},{sample.DateTime}");
                sessionWriter.Flush();

                Console.WriteLine("Prenos u toku...");

                return new ServiceResponse
                {
                    Type = ResponseType.ACK,
                    Status = ResponseStatus.IN_PROGRESS,
                    Message = "Uzorak uspesno primljen"
                };
            }
            catch (Exception ex)
            {
                return new ServiceResponse
                {
                    Type = ResponseType.NACK,
                    Status = ResponseStatus.IN_PROGRESS,
                    Message = $"Greska pri obradi uzorka: {ex.Message}"
                };
            }
        }

        // ZADATAK 3: Validacija svih tipova/jedinica, postojanje obaveznih polja, dozvoljeni opsezi (proveriti dodatno)
        private string ValidateSample(SensorSample sample)
        {
            if (sample == null)
                return "Uzorak je prazan";

            if (sample.Volume < 0 || sample.Volume > 1000)
                return "Volume van dozvoljenog opsega (0-1000 mV)";

            if (sample.LightLevel < 100 || sample.LightLevel > 50000000)
                return "LightLevel van dozvoljenog opsega";

            if (sample.RelativeHumidity <= 0 || sample.RelativeHumidity > 100)
                return "RelativeHumidity van dozvoljenog opsega (0-100%)";

            if (sample.AirQuality < 10000 || sample.AirQuality > 100000)
                return "AirQuality van dozvoljenog opsega";

            if (sample.DateTime == default)
                return "DateTime nije postavljen";

            return null;
        }

        public ServiceResponse EndSession()
        {
            try
            {
                sessionWriter?.Close();
                rejectsWriter?.Close();
                sessionActive = false;

                Console.WriteLine("Zavrsen prenos");

                return new ServiceResponse
                {
                    Type = ResponseType.ACK,
                    Status = ResponseStatus.COMPLETED,
                    Message = "Sesija uspesno zavrsena"
                };
            }
            catch (Exception ex)
            {
                return new ServiceResponse
                {
                    Type = ResponseType.NACK,
                    Status = ResponseStatus.COMPLETED,
                    Message = $"Greska pri zatvaranju sesije: {ex.Message}"
                };
            }
        }
    }
}
