namespace Contracts
{
    public class Document
    {
        public Guid Id { get; set; }
        public List<Quantity> DocumentSummary { get; set; } = new();
        public List<Page> Pages { get; set; } = new();
    }
}
