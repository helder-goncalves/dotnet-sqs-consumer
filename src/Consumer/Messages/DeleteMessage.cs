namespace Consumer.Messages
{
    public class DeleteMessage
    {
        public string MessageId { get; set; }
        public string ReceiptHandle { get; set; }
    }
}