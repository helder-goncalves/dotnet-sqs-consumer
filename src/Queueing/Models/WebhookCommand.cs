namespace Queueing.Queueing.Models
{
    public class WebhookCommand : ISQSCommand
    {
        public string CommandId { get; set; }
        public string Url { get; set; }
        public string ReceiptHandle { get; set; }
    }
}