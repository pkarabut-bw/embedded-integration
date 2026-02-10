namespace Takeoff.Api.Options
{
    public class PeerServicesOptions
    {
        public string EstimatorBaseUrl { get; set; }

        public int HttpTimeoutSeconds { get; set; } = 10;
    }
}