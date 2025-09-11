using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    [ServiceContract]
    public interface ISensorService
    {
        [OperationContract]
        [FaultContract(typeof(ValidationFault))]
        [FaultContract(typeof(DataFormatFault))]
        ServiceResponse StartSession(SessionMetadata metadata);

        [OperationContract]
        [FaultContract(typeof(ValidationFault))]
        [FaultContract(typeof(DataFormatFault))]
        ServiceResponse PushSample(SensorSample sample);

        [OperationContract]
        ServiceResponse EndSession();
    }
}
