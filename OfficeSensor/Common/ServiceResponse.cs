using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace Common
{
    [DataContract]
    public class ServiceResponse
    {
        [DataMember]
        public ResponseType Type { get; set; }

        [DataMember]
        public ResponseStatus Status { get; set; }

        [DataMember]
        public string Message { get; set; }
    }

    [DataContract]
    public enum ResponseType
    {
        [EnumMember]
        ACK,
        [EnumMember]
        NACK
    }

    [DataContract]
    public enum ResponseStatus
    {
        [EnumMember]
        IN_PROGRESS,
        [EnumMember]
        COMPLETED
    }
}
