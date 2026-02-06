namespace Contracts
{
    public class Quantity
    {
        public Guid Id { get; set; }
        
        public string Name { get; set; }
        
        public string Uom { get; set; }
        
        public Guid ConditionId { get; set; }
        
        public double Value { get; set; }
    }
}