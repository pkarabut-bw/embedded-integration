namespace Contracts
{
    public class Page
    {
        public Guid Id { get; set; }
        public int PageNumber { get; set; }
        public List<Quantity> PageSummary { get; set; } = new();
        public List<TakeoffZone> TakeoffZones { get; set; } = new();
    }
}
