using System;
using System.Configuration;
using System.Globalization;

namespace Server
{
    public static class ConfigurationHelper
    {
        public static double LightThreshold
        {
            get
            {
                string value = ConfigurationManager.AppSettings["L_threshold"];
                return double.Parse(value ?? "/", CultureInfo.InvariantCulture);
            }
        }

        public static double RelativeHumidityThreshold
        {
            get
            {
                string value = ConfigurationManager.AppSettings["RH_threshold"];
                return double.Parse(value ?? "/", CultureInfo.InvariantCulture);
            }
        }

        public static double AirQualityThreshold
        {
            get
            {
                string value = ConfigurationManager.AppSettings["AQ_threshold"];
                return double.Parse(value ?? "/", CultureInfo.InvariantCulture);
            }
        }

        public static double DeviationThreshold
        {
            get
            {
                string value = ConfigurationManager.AppSettings["DeviationThreshold"];
                return double.Parse(value ?? "/", CultureInfo.InvariantCulture);
            }
        }

        public static void LogConfiguration()
        {
            Console.WriteLine("=== KONFIGURACIJA THRESHOLD VREDNOSTI ===");
            Console.WriteLine($"Light Level threshold: {LightThreshold:F0} Ohms");
            Console.WriteLine($"Relative Humidity threshold: {RelativeHumidityThreshold:F1}%");
            Console.WriteLine($"Air Quality threshold: {AirQualityThreshold:F0} Ohms");
            Console.WriteLine($"Deviation threshold: +-{DeviationThreshold:F0}%");
            Console.WriteLine();
        }
    }
}