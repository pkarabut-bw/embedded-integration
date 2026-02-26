using System.Text.Json.Serialization;

namespace Contracts
{
    public class DocumentConditionQuantities
    {
        public Guid DocumentId { get; set; }
        
        public List<Quantity> Quantities { get; set; } = new();
        
        public List<PageConditionQuantities> PageConditionQuantities { get; set; } = new();
    }
}
