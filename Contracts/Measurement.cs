namespace Contracts
{
    public class Measurement
    {
        public string MeasurementType { get; set; }
        
        public string ConditionType { get; set; }
        
        public string Uom { get; set; }
        
        public Guid ConditionId { get; set; }
        
        public double Value { get; set; }
    }
}