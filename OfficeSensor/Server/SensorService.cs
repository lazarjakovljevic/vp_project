using System;
using System.Globalization;
using System.IO;
using System.ServiceModel;
using Common;

namespace Server
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class SensorService : ISensorService, IDisposable
    {
        private string currentSessionFile;
        private string rejectsFile;
        private StreamWriter sessionWriter;
        private StreamWriter rejectsWriter;
        private bool sessionActive = false;
        private bool disposed = false;

        public ServiceResponse StartSession(SessionMetadata metadata)
        {
            try
            {
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
                throw new FaultException<DataFormatFault>(
                    new DataFormatFault
                    {
                        Message = "Greska pri pokretanju sesije",
                        Details = ex.Message,
                        FieldName = "Session"
                    },
                    new FaultReason("Greska pri pokretanju sesije")
                );
            }
        }

        public ServiceResponse PushSample(SensorSample sample)
        {
            if (!sessionActive || sessionWriter == null)
            {
                throw new FaultException<ValidationFault>(
                    new ValidationFault
                    {
                        Message = "Sesija nije aktivna",
                        FieldName = "Session",
                        InvalidValue = sessionActive,
                        ExpectedRange = "Aktivna sesija"
                    },
                    new FaultReason("Sesija nije aktivna")
                );
            }

            try
            {
                Console.WriteLine("Prenos u toku...");

                ValidateSampleOrThrow(sample);

                sessionWriter.WriteLine($"{sample.Volume},{sample.RelativeHumidity},{sample.AirQuality},{sample.LightLevel},{sample.DateTime}");
                sessionWriter.Flush();              

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
                throw new FaultException<DataFormatFault>(
                    new DataFormatFault
                    {
                        Message = "Greska pri obradi uzorka",
                        Details = ex.Message,
                        FieldName = "Sample"
                    },
                    new FaultReason("Greska pri obradi uzorka")
                );
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

        private void ValidateSampleOrThrow(SensorSample sample)
        {
            if (sample == null)
            {
                ThrowDataFormatFault("Uzorak je prazan", "Sample", "SensorSample objekat je null");
            }

            if (sample.Volume < 0 || sample.Volume > 1000)
            {
                ThrowValidationFault("Volume van dozvoljenog opsega", "Volume", sample.Volume, "0-1000 mV", sample);
            }

            if (sample.LightLevel < 100 || sample.LightLevel > 50000000)
            {
                ThrowValidationFault("LightLevel van dozvoljenog opsega", "LightLevel", sample.LightLevel, "100-50000000 Ohms", sample);
            }

            if (sample.RelativeHumidity <= 0 || sample.RelativeHumidity > 30)
            {
                ThrowValidationFault("RelativeHumidity van dozvoljenog opsega", "RelativeHumidity", sample.RelativeHumidity, "0-30%", sample);
            }

            if (sample.AirQuality < 10000 || sample.AirQuality > 100000)
            {
                ThrowValidationFault("AirQuality van dozvoljenog opsega", "AirQuality", sample.AirQuality, "10000-100000 Ohms", sample);
            }

            if (sample.DateTime == default)
            {
                ThrowDataFormatFault("DateTime nije postavljen", "DateTime", "DateTime ima default vrednost");
            }

            if (double.IsNaN(sample.Volume) || double.IsInfinity(sample.Volume))
            {
                ThrowDataFormatFault("Volume nije validna numericka vrednost", "Volume", "Vrednost je NaN ili Infinity");
            }
        }

        private void ThrowValidationFault(string message, string fieldName, object invalidValue, string expectedRange, SensorSample sample)
        {
            LogRejectedSample(sample, message);
            throw new FaultException<ValidationFault>(
                new ValidationFault
                {
                    Message = message,
                    FieldName = fieldName,
                    InvalidValue = invalidValue,
                    ExpectedRange = expectedRange
                },
                new FaultReason($"Greska validacije: {message}")
            );
        }

        private void ThrowDataFormatFault(string message, string fieldName, string details)
        {
            throw new FaultException<DataFormatFault>(
                new DataFormatFault
                {
                    Message = message,
                    FieldName = fieldName,
                    Details = details
                },
                new FaultReason($"Greska formata: {message}")
            );
        }

        private void LogRejectedSample(SensorSample sample, string reason)
        {
            try
            {
                rejectsWriter?.WriteLine($"{sample?.Volume.ToString(CultureInfo.InvariantCulture)},{sample?.RelativeHumidity.ToString(CultureInfo.InvariantCulture)},{sample?.AirQuality.ToString(CultureInfo.InvariantCulture)},{sample?.LightLevel.ToString(CultureInfo.InvariantCulture)},{sample?.DateTime.ToString("yyyy-MM-dd HH:mm:ss")},{reason}");
                rejectsWriter?.Flush();
            }
            catch
            {
                // Ignorisemo logging erorr-e kako bi se ispisao pravi izuzetak korisniku
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                Console.WriteLine("=== DISPOSE POZVAN ===");
                if (disposing)
                {
                    Console.WriteLine("Zatvaram StreamWriter resurse...");
                    sessionWriter?.Close();
                    sessionWriter?.Dispose();
                    rejectsWriter?.Close();
                    rejectsWriter?.Dispose();
                    Console.WriteLine("Resursi uspesno zatvoreni");
                }
                disposed = true;
            }
        }

        ~SensorService()
        {
            Dispose(false);
        }
    }
}