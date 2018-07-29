namespace Queueing.Queueing.Models
{
    public class EmailCommand : ISQSCommand
    {
        public string CommandId { get; set; }
        public string ReceiptHandle { get; set; }
        public string Email { get; set; }
    }
}