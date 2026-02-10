namespace Contracts
{
    /// <summary>
    /// In case of callbacks - single Condition instance will be sent
    /// In case of snapshot - full list of Conditions will be requested
    /// </summary>
    public class Condition
    {
        public Guid Id { get; set; }

        public Guid ProjectId { get; set; }

        public List<Measurement> Measurements { get; set; }
    }
}
