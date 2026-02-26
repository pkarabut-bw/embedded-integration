namespace Contracts
{
    public class ProjectConditionQuantities
    {
        public Guid ConditionId { get; set; }

        public Guid ProjectId { get; set; }

        public List<Quantity> Quantities { get; set; } = new();

        public List<DocumentConditionQuantities> DocumentConditionQuantities { get; set; } = new();
    }
}
