namespace Contracts
{
    public class Condition
    {
        public Guid Id { get; set; }

        public Guid ProjectId { get; set; }

        public List<Quantity> ProjectSummary { get; set; } = new();

        public List<Document> Documents { get; set; } = new();
    }
}
