namespace Contracts
{
    public class TakeoffZoneConditionQuantities
    {
        public Guid TakeoffZoneId { get; set; }
        
        public List<Quantity> Quantities { get; set; } = new();
    }
}
