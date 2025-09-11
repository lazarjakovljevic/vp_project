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

        private SensorSample previousSample = null;
        private double lightLevelSum = 0;
        private double relativeHumiditySum = 0;
        private double airQualitySum = 0;
        private int analyticsCount = 0;

        public delegate void TransferEventHandler(object sender, TransferEventArgs e);
        public delegate void SampleReceivedEventHandler(object sender, SampleReceivedEventArgs e);
        public delegate void WarningEventHandler(object sender, WarningEventArgs e);
        public delegate void SpikeEventHandler(object sender, SpikeEventArgs e);
        public delegate void OutOfBandEventHandler(object sender, OutOfBandEventArgs e);

        public event TransferEventHandler OnTransferStarted;
        public event SampleReceivedEventHandler OnSampleReceived;
        public event TransferEventHandler OnTransferCompleted;
        public event WarningEventHandler OnWarningRaised;
        public event SpikeEventHandler OnLightLevelSpike;
        public event SpikeEventHandler OnRelativeHumiditySpike;
        public event SpikeEventHandler OnAirQualitySpike;
        public event OutOfBandEventHandler OnOutOfBandWarning;

        private int sampleCount = 0;

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
                RaiseTransferStarted("Sesija pokrenuta", currentSessionFile);

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
                ValidateSampleOrThrow(sample);

                Console.WriteLine("Prenos u toku...");

                sessionWriter.WriteLine($"{sample.Volume},{sample.RelativeHumidity},{sample.AirQuality},{sample.LightLevel},{sample.DateTime}");
                sessionWriter.Flush();

                sampleCount++;
                RaiseSampleReceived(sample, sampleCount);
                PerformAnalytics(sample);

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

                RaiseTransferCompleted("Sesija zavrsena", currentSessionFile);

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
                // ignorisemo logging erorr-e kako bi se ispisao pravi izuzetak korisniku
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

        protected virtual void RaiseTransferStarted(string message, string sessionId = null)
        {
            if (OnTransferStarted != null)
            {
                var args = new TransferEventArgs(message, sessionId);
                OnTransferStarted(this, args);
            }
        }

        protected virtual void RaiseSampleReceived(SensorSample sample, int count)
        {
            if (OnSampleReceived != null)
            {
                var args = new SampleReceivedEventArgs(sample, count);
                OnSampleReceived(this, args);
            }
        }

        protected virtual void RaiseTransferCompleted(string message, string sessionId = null)
        {
            if (OnTransferCompleted != null)
            {
                var args = new TransferEventArgs(message, sessionId);
                OnTransferCompleted(this, args);
            }
        }

        protected virtual void RaiseWarning(string warningType, string message, double currentValue, double thresholdValue, string fieldName)
        {
            if (OnWarningRaised != null)
            {
                var args = new WarningEventArgs(warningType, message, currentValue, thresholdValue, fieldName);
                OnWarningRaised(this, args);
            }
        }

        protected virtual void RaiseLightLevelSpike(string direction, double delta, double previousValue, double currentValue, double threshold)
        {
            if (OnLightLevelSpike != null)
            {
                var args = new SpikeEventArgs("LightLevel", direction, delta, previousValue, currentValue, threshold);
                OnLightLevelSpike(this, args);
            }
        }

        protected virtual void RaiseRelativeHumiditySpike(string direction, double delta, double previousValue, double currentValue, double threshold)
        {
            if (OnRelativeHumiditySpike != null)
            {
                var args = new SpikeEventArgs("RelativeHumidity", direction, delta, previousValue, currentValue, threshold);
                OnRelativeHumiditySpike(this, args);
            }
        }

        protected virtual void RaiseAirQualitySpike(string direction, double delta, double previousValue, double currentValue, double threshold)
        {
            if (OnAirQualitySpike != null)
            {
                var args = new SpikeEventArgs("AirQuality", direction, delta, previousValue, currentValue, threshold);
                OnAirQualitySpike(this, args);
            }
        }

        protected virtual void RaiseOutOfBandWarning(string fieldName, string direction, double currentValue, double runningMean, double deviationPercent)
        {
            if (OnOutOfBandWarning != null)
            {
                var args = new OutOfBandEventArgs(fieldName, direction, currentValue, runningMean, deviationPercent);
                OnOutOfBandWarning(this, args);
            }
        }

        private void PerformAnalytics(SensorSample currentSample)
        {
            if (previousSample == null)
            {
                previousSample = currentSample;
                UpdateRunningSums(currentSample);
                return;
            }

            var lightThreshold = ConfigurationHelper.LightThreshold;
            var rhThreshold = ConfigurationHelper.RelativeHumidityThreshold;
            var aqThreshold = ConfigurationHelper.AirQualityThreshold;
            var deviationThreshold = ConfigurationHelper.DeviationThreshold;

            double deltaL = Math.Abs(currentSample.LightLevel - previousSample.LightLevel);
            if (deltaL > lightThreshold)
            {
                string direction = currentSample.LightLevel > previousSample.LightLevel ? "Iznad" : "Ispod";
                RaiseLightLevelSpike(direction, deltaL, previousSample.LightLevel, currentSample.LightLevel, lightThreshold);
            }

            double deltaRH = Math.Abs(currentSample.RelativeHumidity - previousSample.RelativeHumidity);
            if (deltaRH > rhThreshold)
            {
                string direction = currentSample.RelativeHumidity > previousSample.RelativeHumidity ? "Iznad" : "Ispod";
                RaiseRelativeHumiditySpike(direction, deltaRH, previousSample.RelativeHumidity, currentSample.RelativeHumidity, rhThreshold);
            }

            double deltaAQ = Math.Abs(currentSample.AirQuality - previousSample.AirQuality);
            if (deltaAQ > aqThreshold)
            {
                string direction = currentSample.AirQuality > previousSample.AirQuality ? "Iznad" : "Ispod";
                RaiseAirQualitySpike(direction, deltaAQ, previousSample.AirQuality, currentSample.AirQuality, aqThreshold);
            }

            UpdateRunningSums(currentSample);

            if (analyticsCount > 1) 
            {
                double lightMean = lightLevelSum / analyticsCount;
                double rhMean = relativeHumiditySum / analyticsCount;
                double aqMean = airQualitySum / analyticsCount;

                CheckOutOfBandWarning("LightLevel", currentSample.LightLevel, lightMean, deviationThreshold);
                CheckOutOfBandWarning("RelativeHumidity", currentSample.RelativeHumidity, rhMean, deviationThreshold);
                CheckOutOfBandWarning("AirQuality", currentSample.AirQuality, aqMean, deviationThreshold);
            }

            previousSample = currentSample;
        }

        private void UpdateRunningSums(SensorSample sample)
        {
            analyticsCount++;
            lightLevelSum += sample.LightLevel;
            relativeHumiditySum += sample.RelativeHumidity;
            airQualitySum += sample.AirQuality;
        }

        private void CheckOutOfBandWarning(string fieldName, double currentValue, double runningMean, double deviationThreshold)
        {
            double lowerBound = runningMean * (100 - deviationThreshold) / 100; 
            double upperBound = runningMean * (100 + deviationThreshold) / 100;  

            if (currentValue < lowerBound)
            {
                double deviationPercent = Math.Abs((currentValue - runningMean) / runningMean) * 100;
                RaiseOutOfBandWarning(fieldName, "Ispod", currentValue, runningMean, deviationPercent);
            }
            else if (currentValue > upperBound)
            {
                double deviationPercent = Math.Abs((currentValue - runningMean) / runningMean) * 100;
                RaiseOutOfBandWarning(fieldName, "Iznad", currentValue, runningMean, deviationPercent);
            }
        }
    }
}