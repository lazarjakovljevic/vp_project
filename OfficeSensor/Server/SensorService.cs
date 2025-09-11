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
                throw new FaultException<ValidationFault>(new ValidationFault
                {
                    Message = "Sesija nije aktivna",
                    FieldName = "Session",
                    InvalidValue = sessionActive,
                    ExpectedRange = "Aktivna sesija"
                });
            }

            try
            {
                // ZADATAK 3: Kompletna validacija podataka sa FaultException-ima
                ValidateSampleOrThrow(sample);

                // ZADATAK 6: Upis validnog uzorka
                sessionWriter.WriteLine($"{sample.Volume},{sample.RelativeHumidity},{sample.AirQuality},{sample.LightLevel},{sample.DateTime}");
                sessionWriter.Flush();

                // ZADATAK 7: Status "prenos u toku..."
                Console.WriteLine("Prenos u toku...");

                return new ServiceResponse
                {
                    Type = ResponseType.ACK,
                    Status = ResponseStatus.IN_PROGRESS,
                    Message = "Uzorak uspesno primljen"
                };
            }
            catch (FaultException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new FaultException<DataFormatFault>(new DataFormatFault
                {
                    Message = "Greska pri obradi uzorka",
                    Details = ex.Message,
                    FieldName = "Sample"
                });
            }
        }

        // ZADATAK 3: Validacija sa FaultException-ima
        private void ValidateSampleOrThrow(SensorSample sample)
        {
            if (sample == null)
            {
                throw new FaultException<DataFormatFault>(new DataFormatFault
                {
                    Message = "Uzorak je prazan",
                    Details = "SensorSample objekat je null",
                    FieldName = "Sample"
                });
            }

            if (sample.Volume < 0 || sample.Volume > 1000)
            {
                LogRejectedSample(sample, "Volume van dozvoljenog opsega");
                throw new FaultException<ValidationFault>(new ValidationFault
                {
                    Message = "Volume van dozvoljenog opsega",
                    FieldName = "Volume",
                    InvalidValue = sample.Volume,
                    ExpectedRange = "0-1000 mV"
                });
            }

            if (sample.RelativeHumidity <= 0 || sample.RelativeHumidity > 100)
            {
                LogRejectedSample(sample, "RelativeHumidity van dozvoljenog opsega");
                throw new FaultException<ValidationFault>(new ValidationFault
                {
                    Message = "RelativeHumidity van dozvoljenog opsega",
                    FieldName = "RelativeHumidity",
                    InvalidValue = sample.RelativeHumidity,
                    ExpectedRange = "0-100%"
                });
            }

            if (sample.AirQuality < 0 || sample.AirQuality > 100000)
            {
                LogRejectedSample(sample, "AirQuality van dozvoljenog opsega");
                throw new FaultException<ValidationFault>(new ValidationFault
                {
                    Message = "AirQuality van dozvoljenog opsega",
                    FieldName = "AirQuality",
                    InvalidValue = sample.AirQuality,
                    ExpectedRange = "0-100000 Ohms"
                });
            }

            if (sample.LightLevel < 0 || sample.LightLevel > 50000000)
            {
                LogRejectedSample(sample, "LightLevel van dozvoljenog opsega");
                throw new FaultException<ValidationFault>(new ValidationFault
                {
                    Message = "LightLevel van dozvoljenog opsega",
                    FieldName = "LightLevel",
                    InvalidValue = sample.LightLevel,
                    ExpectedRange = "0-50000000 Ohms"
                });
            }

            if (sample.DateTime == default)
            {
                LogRejectedSample(sample, "DateTime nije postavljen");
                throw new FaultException<DataFormatFault>(new DataFormatFault
                {
                    Message = "DateTime nije postavljen",
                    Details = "DateTime ima default vrednost",
                    FieldName = "DateTime"
                });
            }

            if (double.IsNaN(sample.Volume) || double.IsInfinity(sample.Volume))
            {
                LogRejectedSample(sample, "Volume nije validna numerička vrednost");
                throw new FaultException<DataFormatFault>(new DataFormatFault
                {
                    Message = "Volume nije validna numerička vrednost",
                    Details = "Vrednost je NaN ili Infinity",
                    FieldName = "Volume"
                });
            }
        }

        private void LogRejectedSample(SensorSample sample, string reason)
        {
            try
            {
                rejectsWriter?.WriteLine($"{sample?.Volume},{sample?.RelativeHumidity},{sample?.AirQuality},{sample?.LightLevel},{sample?.DateTime},{reason}");
                rejectsWriter?.Flush();
            }
            catch
            {
                // Ignorisemo logging error-e da ne bi prekinuli tok funkcije, i da se ne bi ispisao pogresan exception
            }
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
