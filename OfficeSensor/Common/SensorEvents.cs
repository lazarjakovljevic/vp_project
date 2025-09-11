using System;
using System.Runtime.Serialization;

namespace Common
{
    // ZADATAK 8: EventArgs klase za dogadjaje
    [DataContract]
    public class TransferEventArgs : EventArgs
    {
        [DataMember]
        public string Message { get; set; }

        [DataMember]
        public DateTime Timestamp { get; set; }

        [DataMember]
        public string SessionId { get; set; }

        public TransferEventArgs(string message, string sessionId = null)
        {
            Message = message;
            SessionId = sessionId;
            Timestamp = DateTime.Now;
        }
    }

    [DataContract]
    public class SampleReceivedEventArgs : EventArgs
    {
        [DataMember]
        public SensorSample Sample { get; set; }

        [DataMember]
        public int SampleCount { get; set; }

        [DataMember]
        public DateTime Timestamp { get; set; }

        public SampleReceivedEventArgs(SensorSample sample, int sampleCount)
        {
            Sample = sample;
            SampleCount = sampleCount;
            Timestamp = DateTime.Now;
        }
    }

    [DataContract]
    public class WarningEventArgs : EventArgs
    {
        [DataMember]
        public string WarningType { get; set; }

        [DataMember]
        public string Message { get; set; }

        [DataMember]
        public double CurrentValue { get; set; }

        [DataMember]
        public double ThresholdValue { get; set; }

        [DataMember]
        public string FieldName { get; set; }

        [DataMember]
        public DateTime Timestamp { get; set; }

        public WarningEventArgs(string warningType, string message, double currentValue, double thresholdValue, string fieldName)
        {
            WarningType = warningType;
            Message = message;
            CurrentValue = currentValue;
            ThresholdValue = thresholdValue;
            FieldName = fieldName;
            Timestamp = DateTime.Now;
        }
    }

    [DataContract]
    public class SpikeEventArgs : EventArgs
    {
        [DataMember]
        public string SpikeType { get; set; }

        [DataMember]
        public string Direction { get; set; }

        [DataMember]
        public double Delta { get; set; }

        [DataMember]
        public double PreviousValue { get; set; }

        [DataMember]
        public double CurrentValue { get; set; }

        [DataMember]
        public double Threshold { get; set; }

        [DataMember]
        public DateTime Timestamp { get; set; }

        public SpikeEventArgs(string spikeType, string direction, double delta, double previousValue, double currentValue, double threshold)
        {
            SpikeType = spikeType;
            Direction = direction;
            Delta = delta;
            PreviousValue = previousValue;
            CurrentValue = currentValue;
            Threshold = threshold;
            Timestamp = DateTime.Now;
        }
    }

    [DataContract]
    public class OutOfBandEventArgs : EventArgs
    {
        [DataMember]
        public string FieldName { get; set; }

        [DataMember]
        public string Direction { get; set; }

        [DataMember]
        public double CurrentValue { get; set; }

        [DataMember]
        public double RunningMean { get; set; }

        [DataMember]
        public double DeviationPercent { get; set; }

        [DataMember]
        public DateTime Timestamp { get; set; }

        public OutOfBandEventArgs(string fieldName, string direction, double currentValue, double runningMean, double deviationPercent)
        {
            FieldName = fieldName;
            Direction = direction;
            CurrentValue = currentValue;
            RunningMean = runningMean;
            DeviationPercent = deviationPercent;
            Timestamp = DateTime.Now;
        }
    }
}