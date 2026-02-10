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

        /// <summary>
        /// Always full list of possible measurements for this condition
        /// </summary>
        public List<MeasurementMetadata> Metadata { get; set; }

        /// <summary>
        /// In case of callback - only changed measurements
        /// In case of snapshot - all possible measurements
        /// </summary>
        public List<MeasurementValue> MeasurementValues { get; set; }
    }
}
