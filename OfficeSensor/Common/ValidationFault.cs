using System.Runtime.Serialization;

namespace Common
{
    [DataContract]
    public class ValidationFault
    {
        [DataMember]
        public string Message { get; set; }

        [DataMember]
        public string FieldName { get; set; }

        [DataMember]
        public object InvalidValue { get; set; }

        [DataMember]
        public string ExpectedRange { get; set; }
    }
}
