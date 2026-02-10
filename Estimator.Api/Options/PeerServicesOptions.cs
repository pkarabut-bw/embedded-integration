namespace Estimator.Api.Options
{
    public class PeerServicesOptions
    {
        public string TakeoffBaseUrl { get; set; }

        public int HttpTimeoutSeconds { get; set; } = 10;
    }
}