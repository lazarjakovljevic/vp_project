using Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

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
            throw new NotImplementedException();
        }

        public ServiceResponse EndSession()
        {
            try
            {
                sessionWriter?.Close();
                rejectsWriter?.Close();
                sessionActive = false;

                // ZADATAK 7: Status "zavrsen prenos"
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
