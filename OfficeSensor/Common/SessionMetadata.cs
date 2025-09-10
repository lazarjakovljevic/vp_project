using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    [DataContract]
    public class SessionMetadata
    {
        [DataMember]
        public double Volume { get; set; }

        [DataMember]
        public double RelativeHumidity { get; set; }

        [DataMember]
        public double AirQuality { get; set; }

        [DataMember]
        public double LightLevel { get; set; }

        [DataMember]
        public DateTime DateTime { get; set; }
    }
}
