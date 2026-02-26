using System.Text.Json.Serialization;

namespace Contracts
{
    public class PageConditionQuantities
    {
        public Guid PageId { get; set; }
        
        public int PageNumber { get; set; }
        
        public List<Quantity> Quantities { get; set; } = new();
        
        public List<TakeoffZoneConditionQuantities> TakeoffZoneConditionQuantities { get; set; } = new();
    }
}
