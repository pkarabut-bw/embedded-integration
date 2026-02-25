namespace Contracts
{
    public class TakeoffZone
    {
        public Guid Id { get; set; }
        
        public List<Quantity> ZoneSummary { get; set; } = new();
    }
}
