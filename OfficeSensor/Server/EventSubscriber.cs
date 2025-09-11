using System;
using Common;

namespace Server
{
    public class EventSubscriber
    {
        private readonly SensorService sensorService;

        public EventSubscriber(SensorService service)
        {
            sensorService = service;

            sensorService.OnTransferStarted += OnTransferStarted;
            sensorService.OnSampleReceived += OnSampleReceived;
            sensorService.OnTransferCompleted += OnTransferCompleted;
            sensorService.OnWarningRaised += OnWarningRaised;
            sensorService.OnLightLevelSpike += OnLightLevelSpike;
            sensorService.OnRelativeHumiditySpike += OnRelativeHumiditySpike;
            sensorService.OnAirQualitySpike += OnAirQualitySpike;
            sensorService.OnOutOfBandWarning += OnOutOfBandWarning;
        }

        private void OnTransferStarted(object sender, TransferEventArgs e)
        {
            Console.WriteLine("=== DOGADJAJ: Transfer pokrenut ===");
            Console.WriteLine($"Poruka: {e.Message}");
            Console.WriteLine($"Sesija: {e.SessionId}");
            Console.WriteLine($"Vreme: {e.Timestamp:HH:mm:ss}");
            Console.WriteLine();
        }

        private void OnSampleReceived(object sender, SampleReceivedEventArgs e)
        {
            Console.WriteLine("=== DOGADJAJ: Uzorak primljen ===");
            Console.WriteLine($"Broj uzorka: {e.SampleCount}");
            Console.WriteLine($"Volume: {e.Sample.Volume} mV");
            Console.WriteLine($"Light Level: {e.Sample.LightLevel} Ohms");
            Console.WriteLine($"Relative Humidity: {e.Sample.RelativeHumidity}%");
            Console.WriteLine($"Air Quality: {e.Sample.AirQuality} Ohms");
            Console.WriteLine($"Vreme uzorka: {e.Sample.DateTime:HH:mm:ss}");
            Console.WriteLine($"Vreme prijema: {e.Timestamp:HH:mm:ss}");
            Console.WriteLine();
        }

        private void OnTransferCompleted(object sender, TransferEventArgs e)
        {
            Console.WriteLine("=== DOGADJAJ: Transfer zavrsen ===");
            Console.WriteLine($"Poruka: {e.Message}");
            Console.WriteLine($"Sesija: {e.SessionId}");
            Console.WriteLine($"Vreme: {e.Timestamp:HH:mm:ss}");
            Console.WriteLine();
        }

        private void OnWarningRaised(object sender, WarningEventArgs e)
        {
            Console.WriteLine("=== DOGADJAJ: Upozorenje ===");
            Console.WriteLine($"Tip: {e.WarningType}");
            Console.WriteLine($"Poruka: {e.Message}");
            Console.WriteLine($"Polje: {e.FieldName}");
            Console.WriteLine($"Trenutna vrednost: {e.CurrentValue}");
            Console.WriteLine($"Prag: {e.ThresholdValue}");
            Console.WriteLine($"Vreme: {e.Timestamp:HH:mm:ss}");
            Console.WriteLine();
        }

        private void OnLightLevelSpike(object sender, SpikeEventArgs e)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("=== DOGADJAJ: Nagla promena svetlosnog nivoa ===");
            Console.WriteLine($"Smer: {e.Direction} ocekivanog");
            Console.WriteLine($"Delta: {e.Delta:F2}");
            Console.WriteLine($"Prethodna vrednost: {e.PreviousValue:F2} Ohms");
            Console.WriteLine($"Trenutna vrednost: {e.CurrentValue:F2} Ohms");
            Console.WriteLine($"Prag: {e.Threshold:F2}");
            Console.WriteLine($"Vreme: {e.Timestamp:HH:mm:ss}");
            Console.ResetColor();
            Console.WriteLine();
        }

        private void OnRelativeHumiditySpike(object sender, SpikeEventArgs e)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("=== DOGADJAJ: Nagla promena relativne vlaznosti ===");
            Console.WriteLine($"Smer: {e.Direction} ocekivanog");
            Console.WriteLine($"Delta: {e.Delta:F2}");
            Console.WriteLine($"Prethodna vrednost: {e.PreviousValue:F2}%");
            Console.WriteLine($"Trenutna vrednost: {e.CurrentValue:F2}%");
            Console.WriteLine($"Prag: {e.Threshold:F2}");
            Console.WriteLine($"Vreme: {e.Timestamp:HH:mm:ss}");
            Console.ResetColor();
            Console.WriteLine();
        }

        private void OnAirQualitySpike(object sender, SpikeEventArgs e)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("=== DOGADJAJ: Nagla promena kvaliteta vazduha ===");
            Console.WriteLine($"Smer: {e.Direction} ocekivanog");
            Console.WriteLine($"Delta: {e.Delta:F2}");
            Console.WriteLine($"Prethodna vrednost: {e.PreviousValue:F2} Ohms");
            Console.WriteLine($"Trenutna vrednost: {e.CurrentValue:F2} Ohms");
            Console.WriteLine($"Prag: {e.Threshold:F2}");
            Console.WriteLine($"Vreme: {e.Timestamp:HH:mm:ss}");
            Console.ResetColor();
            Console.WriteLine();
        }

        private void OnOutOfBandWarning(object sender, OutOfBandEventArgs e)
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine("=== DOGADJAJ: Van opsega (+-25% odstupanje) ===");
            Console.WriteLine($"Polje: {e.FieldName}");
            Console.WriteLine($"Smer: {e.Direction} srednje vrednosti");
            Console.WriteLine($"Trenutna vrednost: {e.CurrentValue:F2}");
            Console.WriteLine($"Srednja vrednost: {e.RunningMean:F2}");
            Console.WriteLine($"Odstupanje: {e.DeviationPercent:F1}%");
            Console.WriteLine($"Vreme: {e.Timestamp:HH:mm:ss}");
            Console.ResetColor();
            Console.WriteLine();
        }

        public void Unsubscribe()
        {
            if (sensorService != null)
            {
                sensorService.OnTransferStarted -= OnTransferStarted;
                sensorService.OnSampleReceived -= OnSampleReceived;
                sensorService.OnTransferCompleted -= OnTransferCompleted;
                sensorService.OnWarningRaised -= OnWarningRaised;
                sensorService.OnLightLevelSpike -= OnLightLevelSpike;
                sensorService.OnRelativeHumiditySpike -= OnRelativeHumiditySpike;
                sensorService.OnAirQualitySpike -= OnAirQualitySpike;
                sensorService.OnOutOfBandWarning -= OnOutOfBandWarning;
            }
        }
    }
}