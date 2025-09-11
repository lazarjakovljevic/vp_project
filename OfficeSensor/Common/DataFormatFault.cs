using System.Runtime.Serialization;

namespace Common
{
    [DataContract]
    public class DataFormatFault
    {
        [DataMember]
        public string Message { get; set; }

        [DataMember]
        public string Details { get; set; }

        [DataMember]
        public string FieldName { get; set; }
    }
}
