using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using Common;

namespace Client
{
    public class CsvReader : IDisposable
    {
        private StreamReader reader;
        private string filePath;
        private bool disposed = false;

        public CsvReader(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"CSV fajl nije pronađen: {filePath}");

            this.filePath = filePath;
            this.reader = new StreamReader(filePath);
        }

        public List<SensorSample> ReadSamples(int maxRows = 100)
        {
            var samples = new List<SensorSample>();

            try
            {
                string headerLine = reader.ReadLine();
                Console.WriteLine($"CSV Header: {headerLine}");

                int rowCount = 0;
                string line;

                // ZADATAK 5: Ucitavanje prvih 100 redova
                while ((line = reader.ReadLine()) != null && rowCount < maxRows)
                {
                    try
                    {
                        var sample = ParseCsvLine(line);
                        if (sample != null)
                        {
                            samples.Add(sample);
                            rowCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        // ZADATAK 5: Nevalidne redove prijaviti u izdvojeni log
                        Console.WriteLine($"Greska pri parsiranju reda {rowCount + 1}: {ex.Message}");
                        Console.WriteLine($"Problematican red: {line}");
                    }
                }

                Console.WriteLine($"Ucitano {samples.Count} validnih uzoraka od {rowCount} redova.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Greska pri citanju CSV fajla: {ex.Message}");
            }

            return samples;
        }

        private SensorSample ParseCsvLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return null;

            // ZADATAK 5: Parsiranje CSV
            var parts = line.Split(',');
            if (parts.Length < 10)
                throw new FormatException("Nedovoljan broj kolona u redu");

            return new SensorSample
            {
                DateTime = DateTime.Parse(parts[0], CultureInfo.InvariantCulture),
                Volume = double.Parse(parts[1], CultureInfo.InvariantCulture),             // Volume [mV]
                LightLevel = double.Parse(parts[2], CultureInfo.InvariantCulture),        // Light_Level [Ohms] 
                RelativeHumidity = double.Parse(parts[6], CultureInfo.InvariantCulture), // Relative_Humidity [%]
                AirQuality = double.Parse(parts[7], CultureInfo.InvariantCulture)       // Air_Quality [Ohms]
            };
        }

        // ZADATAK 4: Dispose pattern
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    reader?.Close();
                    reader?.Dispose();
                }
                disposed = true;
            }
        }

        ~CsvReader()
        {
            Dispose(false);
        }
    }
}