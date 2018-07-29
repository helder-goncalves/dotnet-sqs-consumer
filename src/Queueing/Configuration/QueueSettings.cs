namespace Queueing.Configuration
{
    public class QueueSettings
    {
        public string Endpoint { get; set; }
        public string QueueUrl { get; set; }
        public int VisibilityTimeout { get; set; }
        public int Tasks { get; set; }
    }
}